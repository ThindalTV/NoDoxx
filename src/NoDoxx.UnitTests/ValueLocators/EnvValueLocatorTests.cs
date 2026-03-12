using NoDoxx.Interfaces;
using NoDoxx.ValueLocators;

namespace NoDoxx.UnitTests.ValueLocators;

[TestClass]
public class EnvValueLocatorTests
{
    private EnvValueLocator _locator = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _locator = new EnvValueLocator();
    }

    [TestMethod]
    public void FindConfigValues_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        bool threw = false;

        // Act
        try { _locator.FindConfigValues(null!).ToList(); }
        catch (ArgumentNullException) { threw = true; }

        // Assert
        Assert.IsTrue(threw);
    }

    [TestMethod]
    public void FindConfigValues_EmptyFile_ReturnsEmptyList()
    {
        // Act
        var result = _locator.FindConfigValues(string.Empty).ToList();

        // Assert
        Assert.IsFalse(result.Any());
    }

    [TestMethod]
    public void FindConfigValues_WhitespaceOnlyFile_ReturnsEmptyList()
    {
        // Act
        var result = _locator.FindConfigValues("   \n\n  \n").ToList();

        // Assert
        Assert.IsFalse(result.Any());
    }

    // -------------------------------------------------------------------------
    // Key (Name) positions
    // -------------------------------------------------------------------------

    [TestMethod]
    public void FindConfigValues_SimpleKeyValue_ReturnsNamePosition()
    {
        // Arrange
        var content = "API_KEY=supersecret";

        // Act
        var result = _locator.FindConfigValues(content).ToList();

        // Assert
        var name = result.First(cp => cp.Type == ConfigType.Name);
        Assert.AreEqual("API_KEY", content.Substring(name.StartIndex, name.EndIndex - name.StartIndex));
    }

    [TestMethod]
    public void FindConfigValues_SimpleKeyValue_ReturnsValuePosition()
    {
        // Arrange
        var content = "API_KEY=supersecret";

        // Act
        var result = _locator.FindConfigValues(content).ToList();

        // Assert
        var value = result.First(cp => cp.Type == ConfigType.Value);
        Assert.AreEqual("supersecret", content.Substring(value.StartIndex, value.EndIndex - value.StartIndex));
    }

    // -------------------------------------------------------------------------
    // Quoted values
    // -------------------------------------------------------------------------

    [TestMethod]
    public void FindConfigValues_DoubleQuotedValue_ReturnsContentInsideQuotes()
    {
        // Arrange
        var content = "SECRET=\"hidden value\"";

        // Act
        var result = _locator.FindConfigValues(content).ToList();

        // Assert
        var value = result.First(cp => cp.Type == ConfigType.Value);
        Assert.AreEqual("hidden value", content.Substring(value.StartIndex, value.EndIndex - value.StartIndex));
    }

    [TestMethod]
    public void FindConfigValues_SingleQuotedValue_ReturnsContentInsideQuotes()
    {
        // Arrange
        var content = "SECRET='hidden value'";

        // Act
        var result = _locator.FindConfigValues(content).ToList();

        // Assert
        var value = result.First(cp => cp.Type == ConfigType.Value);
        Assert.AreEqual("hidden value", content.Substring(value.StartIndex, value.EndIndex - value.StartIndex));
    }

    [TestMethod]
    public void FindConfigValues_QuotedEmptyValue_ReturnsNoValuePosition()
    {
        // Arrange
        var content = "SECRET=\"\"";

        // Act
        var result = _locator.FindConfigValues(content).ToList();

        // Assert
        Assert.IsFalse(result.Any(cp => cp.Type == ConfigType.Value));
    }

    // -------------------------------------------------------------------------
    // Empty values
    // -------------------------------------------------------------------------

    [TestMethod]
    public void FindConfigValues_EmptyValue_ReturnsNameButNoValue()
    {
        // Arrange
        var content = "EMPTY_KEY=";

        // Act
        var result = _locator.FindConfigValues(content).ToList();

        // Assert
        Assert.IsTrue(result.Any(cp => cp.Type == ConfigType.Name));
        Assert.IsFalse(result.Any(cp => cp.Type == ConfigType.Value));
    }

    // -------------------------------------------------------------------------
    // Comments
    // -------------------------------------------------------------------------

    [TestMethod]
    public void FindConfigValues_CommentLine_ReturnsCommentPosition()
    {
        // Arrange
        var content = "# This is a comment";

        // Act
        var result = _locator.FindConfigValues(content).ToList();

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(ConfigType.Comment, result[0].Type);
        Assert.AreEqual(" This is a comment", content.Substring(result[0].StartIndex, result[0].EndIndex - result[0].StartIndex));
    }

    [TestMethod]
    public void FindConfigValues_CommentLineWithLeadingWhitespace_ReturnsCommentPosition()
    {
        // Arrange
        var content = "  # indented comment";

        // Act
        var result = _locator.FindConfigValues(content).ToList();

        // Assert
        var comment = result.First(cp => cp.Type == ConfigType.Comment);
        Assert.AreEqual(" indented comment", content.Substring(comment.StartIndex, comment.EndIndex - comment.StartIndex));
    }

    [TestMethod]
    public void FindConfigValues_InlineHashInValue_TreatsHashAsPartOfValue()
    {
        // Arrange
        // Per .env convention: inline '#' is NOT a comment — it is part of the value
        var content = "URL=http://example.com#anchor";

        // Act
        var result = _locator.FindConfigValues(content).ToList();

        // Assert
        var value = result.First(cp => cp.Type == ConfigType.Value);
        Assert.AreEqual("http://example.com#anchor", content.Substring(value.StartIndex, value.EndIndex - value.StartIndex));
        Assert.IsFalse(result.Any(cp => cp.Type == ConfigType.Comment));
    }

    // -------------------------------------------------------------------------
    // Multiple entries
    // -------------------------------------------------------------------------

    [TestMethod]
    public void FindConfigValues_MultipleKeyValuePairs_ReturnsAllNamesAndValues()
    {
        // Arrange
        var content = "KEY1=value1\nKEY2=value2\nKEY3=value3";

        // Act
        var result = _locator.FindConfigValues(content).ToList();

        // Assert
        var names = result.Where(cp => cp.Type == ConfigType.Name).ToList();
        var values = result.Where(cp => cp.Type == ConfigType.Value).ToList();
        Assert.AreEqual(3, names.Count);
        Assert.AreEqual(3, values.Count);

        Assert.AreEqual("KEY1", content.Substring(names[0].StartIndex, names[0].EndIndex - names[0].StartIndex));
        Assert.AreEqual("value1", content.Substring(values[0].StartIndex, values[0].EndIndex - values[0].StartIndex));
        Assert.AreEqual("KEY2", content.Substring(names[1].StartIndex, names[1].EndIndex - names[1].StartIndex));
        Assert.AreEqual("value2", content.Substring(values[1].StartIndex, values[1].EndIndex - values[1].StartIndex));
        Assert.AreEqual("KEY3", content.Substring(names[2].StartIndex, names[2].EndIndex - names[2].StartIndex));
        Assert.AreEqual("value3", content.Substring(values[2].StartIndex, values[2].EndIndex - values[2].StartIndex));
    }

    [TestMethod]
    public void FindConfigValues_MixedContent_ReturnsAllTypes()
    {
        // Arrange
        var content = "# config file\nAPI_KEY=secret\nDEBUG=true";

        // Act
        var result = _locator.FindConfigValues(content).ToList();

        // Assert
        Assert.IsTrue(result.Any(cp => cp.Type == ConfigType.Comment));
        Assert.IsTrue(result.Any(cp => cp.Type == ConfigType.Name));
        Assert.IsTrue(result.Any(cp => cp.Type == ConfigType.Value));
    }

    [TestMethod]
    public void FindConfigValues_WindowsLineEndings_ReturnsCorrectPositions()
    {
        // Arrange
        var content = "KEY1=value1\r\nKEY2=value2";

        // Act
        var result = _locator.FindConfigValues(content).ToList();

        // Assert
        var values = result.Where(cp => cp.Type == ConfigType.Value).ToList();
        Assert.AreEqual(2, values.Count);
        Assert.AreEqual("value1", content.Substring(values[0].StartIndex, values[0].EndIndex - values[0].StartIndex));
        Assert.AreEqual("value2", content.Substring(values[1].StartIndex, values[1].EndIndex - values[1].StartIndex));
    }

    // -------------------------------------------------------------------------
    // Value with '=' inside it
    // -------------------------------------------------------------------------

    [TestMethod]
    public void FindConfigValues_ValueContainsEqualsSign_SplitsOnFirstEqualsOnly()
    {
        // Arrange
        var content = "CONNECTION=server=localhost;db=mydb";

        // Act
        var result = _locator.FindConfigValues(content).ToList();

        // Assert
        var name = result.First(cp => cp.Type == ConfigType.Name);
        var value = result.First(cp => cp.Type == ConfigType.Value);
        Assert.AreEqual("CONNECTION", content.Substring(name.StartIndex, name.EndIndex - name.StartIndex));
        Assert.AreEqual("server=localhost;db=mydb", content.Substring(value.StartIndex, value.EndIndex - value.StartIndex));
    }

    // -------------------------------------------------------------------------
    // Trailing whitespace
    // -------------------------------------------------------------------------

    [TestMethod]
    public void FindConfigValues_ValueWithTrailingWhitespace_TrimsTrailingWhitespace()
    {
        // Arrange
        var content = "KEY=value   ";

        // Act
        var result = _locator.FindConfigValues(content).ToList();

        // Assert
        var value = result.First(cp => cp.Type == ConfigType.Value);
        Assert.AreEqual("value", content.Substring(value.StartIndex, value.EndIndex - value.StartIndex));
    }

    // -------------------------------------------------------------------------
    // Realistic .env file
    // -------------------------------------------------------------------------

    [TestMethod]
    public void FindConfigValues_RealisticEnvFile_ReturnsExpectedCounts()
    {
        // Arrange
        var content =
            "# Database\n" +
            "DB_HOST=localhost\n" +
            "DB_PORT=5432\n" +
            "DB_PASSWORD=\"s3cr3t!\"\n" +
            "\n" +
            "# API\n" +
            "API_KEY=abc123\n" +
            "DEBUG=false\n";

        // Act
        var result = _locator.FindConfigValues(content).ToList();

        // Assert
        Assert.AreEqual(2, result.Count(cp => cp.Type == ConfigType.Comment));
        Assert.AreEqual(5, result.Count(cp => cp.Type == ConfigType.Name));
        Assert.AreEqual(5, result.Count(cp => cp.Type == ConfigType.Value));
    }
}
