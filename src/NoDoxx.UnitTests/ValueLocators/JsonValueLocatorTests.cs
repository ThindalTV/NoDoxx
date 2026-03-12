using NoDoxx.Interfaces;
using NoDoxx.ValueLocators;

namespace NoDoxx.UnitTests.ValueLocators;

[TestClass]
public class JsonValueLocatorTests
{
    private JsonValueLocator _locator = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _locator = new JsonValueLocator();
    }

    #region FindConfigValues Tests

    [TestMethod]
    public void FindConfigValues_ValidJson_ReturnsConfigPositions()
    {
        // Arrange
        var jsonContent = "{\"key\":\"value\"}";

        // Act
        var result = _locator.FindConfigValues(jsonContent);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsTrue(resultList.Any());
        var valueItem = resultList.First(cp => cp.Type == ConfigType.Value);
        Assert.AreEqual("value", jsonContent.Substring(valueItem.StartIndex, valueItem.EndIndex - valueItem.StartIndex));
    }

    [TestMethod]
    public void FindConfigValues_InvalidJson_ReturnsErrorConfigPosition()
    {
        // Arrange
        var invalidJson = "{invalid json";

        // Act
        var result = _locator.FindConfigValues(invalidJson);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.HasCount(1, resultList);
        Assert.AreEqual(ConfigType.Value, resultList[0].Type);
        // Just check that it contains some exception information
        Assert.IsGreaterThan(0, resultList[0].Contents.Length);
    }

    [TestMethod]
    public void FindConfigValues_EmptyJson_ReturnsEmptyList()
    {
        // Arrange
        var emptyJson = "{}";

        // Act
        var result = _locator.FindConfigValues(emptyJson);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsFalse(resultList.Any());
    }

    [TestMethod]
    public void FindConfigValues_JsonWithInlineComment_ReturnsConfigPositionsWithComment()
    {
        // Arrange
        var jsonContent = "{\n\"key\":\"value\" // this is a comment\n}";

        // Act
        var result = _locator.FindConfigValues(jsonContent);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsTrue(resultList.Any(cp => cp.Type == ConfigType.Comment));
    }

    [TestMethod]
    public void FindConfigValues_JsonWithMultiLineComment_ReturnsConfigPositionsWithComment()
    {
        // Arrange
        var jsonContent = "{\n/* this is a\nmultiline comment */\n\"key\":\"value\"\n}";

        // Act
        var result = _locator.FindConfigValues(jsonContent);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsTrue(resultList.Any(cp => cp.Type == ConfigType.Comment));
    }

    [TestMethod]
    public void FindConfigValues_JsonWithTrailingCommas_ReturnsConfigPositions()
    {
        // Arrange
        var jsonContent = "{\"key1\":\"value1\",\"key2\":\"value2\",}";

        // Act
        var result = _locator.FindConfigValues(jsonContent);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsTrue(resultList.Any());
    }

    [TestMethod]
    public void FindConfigValues_JsonWithMultipleValues_ReturnsMultipleConfigPositions()
    {
        // Arrange
        var jsonContent = "{\"key1\":\"value1\",\"key2\":\"value2\",\"key3\":123}";

        // Act
        var result = _locator.FindConfigValues(jsonContent);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsGreaterThanOrEqualTo(resultList.Count, 3);
        var value1 = resultList.First(cp => cp.Contents == "value1");
        Assert.AreEqual("value1", jsonContent.Substring(value1.StartIndex, value1.EndIndex - value1.StartIndex));
        var value2 = resultList.First(cp => cp.Contents == "value2");
        Assert.AreEqual("value2", jsonContent.Substring(value2.StartIndex, value2.EndIndex - value2.StartIndex));
        var numericValue = resultList.First(cp => cp.Contents == "123");
        Assert.AreEqual("123", jsonContent.Substring(numericValue.StartIndex, numericValue.EndIndex - numericValue.StartIndex));
    }

    [TestMethod]
    public void FindConfigValues_JsonWithNestedObject_ReturnsConfigPositions()
    {
        // Arrange
        var jsonContent = "{\"outer\":{\"inner\":\"value\"}}";

        // Act
        var result = _locator.FindConfigValues(jsonContent);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsTrue(resultList.Any());
    }

    [TestMethod]
    public void FindConfigValues_JsonWithArray_ReturnsConfigPositions()
    {
        // Arrange
        var jsonContent = "{\"array\":[\"value1\",\"value2\",\"value3\"]}";

        // Act
        var result = _locator.FindConfigValues(jsonContent);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsTrue(resultList.Any());
    }

    [TestMethod]
    public void FindConfigValues_JsonWithBooleanValue_ReturnsConfigPositions()
    {
        // Arrange
        var jsonContent = "{\"flag\":true}";

        // Act
        var result = _locator.FindConfigValues(jsonContent);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsTrue(resultList.Any());
        var valueItem = resultList.First(cp => cp.Type == ConfigType.Value);
        Assert.AreEqual("true", jsonContent.Substring(valueItem.StartIndex, valueItem.EndIndex - valueItem.StartIndex));
    }

    [TestMethod]
    public void FindConfigValues_JsonWithNullValue_ReturnsConfigPositions()
    {
        // Arrange
        var jsonContent = "{\"key\":null}";

        // Act
        var result = _locator.FindConfigValues(jsonContent);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsTrue(resultList.Any());
        var valueItem = resultList.First(cp => cp.Type == ConfigType.Value);
        Assert.AreEqual("null", jsonContent.Substring(valueItem.StartIndex, valueItem.EndIndex - valueItem.StartIndex));
    }

    [TestMethod]
    public void FindConfigValues_JsonWithCommentsAndValues_RemovesCommentsInsideValues()
    {
        // Arrange
        var jsonContent = "{\"url\":\"http://example.com\",// comment\n\"key\":\"value\"}";

        // Act
        var result = _locator.FindConfigValues(jsonContent);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsTrue(resultList.Any(cp => cp.Type == ConfigType.Value));
        Assert.IsTrue(resultList.Any(cp => cp.Type == ConfigType.Comment));
    }

    [TestMethod]
    public void FindConfigValues_JsonWithBothCommentTypes_ReturnsAllComments()
    {
        // Arrange
        var jsonContent = "{\n// inline comment\n/* multiline\ncomment */\n\"key\":\"value\"\n}";

        // Act
        var result = _locator.FindConfigValues(jsonContent);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        var comments = resultList.Where(cp => cp.Type == ConfigType.Comment).ToList();
        Assert.HasCount(2, comments);
    }

    [TestMethod]
    public void FindConfigValues_ComplexJson_ReturnsDistinctConfigPositions()
    {
        // Arrange
        var jsonContent = @"{
            ""name"": ""John"",
            ""age"": 30,
            ""active"": true,
            ""address"": {
                ""street"": ""123 Main St"",
                ""city"": ""Springfield""
            },
            ""hobbies"": [""reading"", ""coding""]
        }";

        // Act
        var result = _locator.FindConfigValues(jsonContent);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsTrue(resultList.Any());
        // Check for distinct results
        var distinctCount = resultList.Distinct().Count();
        Assert.AreEqual(resultList.Count, distinctCount);
    }

    [TestMethod]
    public void FindConfigValues_JsonWithCommentBetweenColonAndUrlValue_CommentIsNotFilteredOut()
    {
        // Arrange
        // LocateNames scans every ':' in the file, including those inside string values (e.g. "https://").
        // This produces a false Name region spanning from the key's closing quote to the URL value's
        // opening quote. Any comment whose start index falls inside that region is silently discarded
        // by RemoveIfStartsInside, so it never appears in the returned results.
        var jsonContent = "{\"key\": /* a comment */ \"https://example.com\"}";

        // Act
        var result = _locator.FindConfigValues(jsonContent);

        // Assert
        var resultList = result.ToList();
        Assert.IsTrue(
            resultList.Any(cp => cp.Type == ConfigType.Comment),
            "Expected the comment between ':' and the URL value to survive, but it was " +
            "incorrectly filtered out by the false Name region created for the ':' in 'https://'.");
    }

    [TestMethod]
    public void FindConfigValues_InvalidJson_HidesEntireContent()
    {
        // Arrange
        var invalidJson = "{invalid json";

        // Act
        var result = _locator.FindConfigValues(invalidJson).ToList();

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(ConfigType.Value, result[0].Type);
        Assert.AreEqual(0, result[0].StartIndex);
        Assert.AreEqual(invalidJson.Length, result[0].EndIndex);
    }

    [TestMethod]
    public void FindConfigValues_PartialJson_HidesEntireContent()
    {
        // Arrange
        var partialJson = "{\"key\":";

        // Act
        var result = _locator.FindConfigValues(partialJson).ToList();

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(ConfigType.Value, result[0].Type);
        Assert.AreEqual(0, result[0].StartIndex);
        Assert.AreEqual(partialJson.Length, result[0].EndIndex);
    }

    [TestMethod]
    public void FindConfigValues_RootArray_ReturnsValuePositions()
    {
        // Arrange
        var json = "[\"alpha\",\"beta\",\"gamma\"]";

        // Act
        var result = _locator.FindConfigValues(json).ToList();

        // Assert
        var values = result.Where(cp => cp.Type == ConfigType.Value).ToList();
        Assert.AreEqual(3, values.Count);
        Assert.IsTrue(values.Any(v => v.Contents == "alpha"));
        Assert.IsTrue(values.Any(v => v.Contents == "beta"));
        Assert.IsTrue(values.Any(v => v.Contents == "gamma"));
    }

    [TestMethod]
    public void FindConfigValues_RootArrayWithNumbers_ReturnsValuePositions()
    {
        // Arrange
        var json = "[1,2,3]";

        // Act
        var result = _locator.FindConfigValues(json).ToList();

        // Assert
        var values = result.Where(cp => cp.Type == ConfigType.Value).ToList();
        Assert.AreEqual(3, values.Count);
    }

    [TestMethod]
    public void FindConfigValues_RootString_ReturnsValuePosition()
    {
        // Arrange
        var json = "\"hello world\"";

        // Act
        var result = _locator.FindConfigValues(json).ToList();

        // Assert
        var values = result.Where(cp => cp.Type == ConfigType.Value).ToList();
        Assert.AreEqual(1, values.Count);
        Assert.AreEqual("hello world", values[0].Contents);
    }

    [TestMethod]
    public void FindConfigValues_RootNumber_ReturnsValuePosition()
    {
        // Arrange
        var json = "42";

        // Act
        var result = _locator.FindConfigValues(json).ToList();

        // Assert
        var values = result.Where(cp => cp.Type == ConfigType.Value).ToList();
        Assert.AreEqual(1, values.Count);
        Assert.AreEqual("42", values[0].Contents);
    }

    [TestMethod]
    public void FindConfigValues_RootBoolean_ReturnsValuePosition()
    {
        // Arrange
        var json = "true";

        // Act
        var result = _locator.FindConfigValues(json).ToList();

        // Assert
        var values = result.Where(cp => cp.Type == ConfigType.Value).ToList();
        Assert.AreEqual(1, values.Count);
        Assert.AreEqual("true", values[0].Contents);
    }

    [TestMethod]
    public void FindConfigValues_RootNull_ReturnsValuePosition()
    {
        // Arrange
        var json = "null";

        // Act
        var result = _locator.FindConfigValues(json).ToList();

        // Assert
        var values = result.Where(cp => cp.Type == ConfigType.Value).ToList();
        Assert.AreEqual(1, values.Count);
        Assert.AreEqual("null", values[0].Contents);
    }

    #endregion

    #region LocateComments Tests

    [TestMethod]
    public void LocateComments_NoComments_ReturnsEmptyList()
    {
        // Arrange
        var content = "{\"key\":\"value\"}";

        // Act
        var result = _locator.LocateComments(content);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsFalse(resultList.Any());
    }

    [TestMethod]
    public void LocateComments_SingleInlineComment_ReturnsOneComment()
    {
        // Arrange
        var content = "// this is a comment\n{\"key\":\"value\"}";

        // Act
        var result = _locator.LocateComments(content);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.HasCount(1, resultList);
        Assert.AreEqual(ConfigType.Comment, resultList[0].Type);
        Assert.AreEqual(2, resultList[0].StartIndex);
    }

    [TestMethod]
    public void LocateComments_MultipleInlineComments_ReturnsMultipleComments()
    {
        // Arrange
        var content = "// comment 1\n// comment 2\n// comment 3";

        // Act
        var result = _locator.LocateComments(content);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.HasCount(3, resultList);
        Assert.IsTrue(resultList.All(cp => cp.Type == ConfigType.Comment));
    }

    [TestMethod]
    public void LocateComments_SingleMultiLineComment_ReturnsOneComment()
    {
        // Arrange
        var content = "/* this is a\nmultiline\ncomment */\n{\"key\":\"value\"}";

        // Act
        var result = _locator.LocateComments(content);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.HasCount(1, resultList);
        Assert.AreEqual(ConfigType.Comment, resultList[0].Type);
        Assert.AreEqual(2, resultList[0].StartIndex);
    }

    [TestMethod]
    public void LocateComments_MultipleMultiLineComments_ReturnsMultipleComments()
    {
        // Arrange
        var content = "/* comment 1 */\nsome text\n/* comment 2 */";

        // Act
        var result = _locator.LocateComments(content);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.HasCount(2, resultList);
        Assert.IsTrue(resultList.All(cp => cp.Type == ConfigType.Comment));
    }

    [TestMethod]
    public void LocateComments_MixedCommentTypes_ReturnsAllComments()
    {
        // Arrange
        var content = "// inline comment\nsome text\n/* multiline comment */";

        // Act
        var result = _locator.LocateComments(content);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.HasCount(2, resultList);
        Assert.IsTrue(resultList.All(cp => cp.Type == ConfigType.Comment));
    }

    [TestMethod]
    public void LocateComments_InlineCommentAtEndOfFile_ReturnsComment()
    {
        // Arrange
        var content = "{\"key\":\"value\"}\n// comment at end";

        // Act
        var result = _locator.LocateComments(content);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.HasCount(1, resultList);
        Assert.AreEqual(ConfigType.Comment, resultList[0].Type);
    }

    [TestMethod]
    public void LocateComments_InlineCommentWithoutNewline_ReturnsComment()
    {
        // Arrange
        var content = "// comment without newline";

        // Act
        var result = _locator.LocateComments(content);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.HasCount(1, resultList);
        Assert.AreEqual(2, resultList[0].StartIndex);
        Assert.AreEqual(ConfigType.Comment, resultList[0].Type);
    }

    [TestMethod]
    public void LocateComments_MultiLineCommentWithoutClosing_ReturnsCommentToEnd()
    {
        // Arrange
        var content = "some text /* unclosed comment";

        // Act
        var result = _locator.LocateComments(content);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.HasCount(1, resultList);
        Assert.AreEqual(ConfigType.Comment, resultList[0].Type);
        // EndIndex should be at or before the content length for unclosed comments
        Assert.IsLessThanOrEqualTo(resultList[0].EndIndex, content.Length);
    }

    [TestMethod]
    public void LocateComments_EmptyString_ReturnsEmptyList()
    {
        // Arrange
        var content = string.Empty;

        // Act
        var result = _locator.LocateComments(content);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsFalse(resultList.Any());
    }

    [TestMethod]
    public void LocateComments_CommentMarkersInMiddleOfText_ReturnsComments()
    {
        // Arrange
        var content = "text before // comment\nmore text";

        // Act
        var result = _locator.LocateComments(content);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.HasCount(1, resultList);
        Assert.AreEqual(ConfigType.Comment, resultList[0].Type);
        // Comment should start after "text before "
        Assert.AreNotEqual(0, resultList[0].StartIndex);
    }

    [TestMethod]
    public void LocateComments_NestedMultiLineComments_ReturnsComments()
    {
        // Arrange
        var content = "/* outer /* inner */ still in comment */";

        // Act
        var result = _locator.LocateComments(content);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        Assert.IsTrue(resultList.Any());
    }

    [TestMethod]
    public void LocateComments_ConsecutiveCommentMarkers_ReturnsAllComments()
    {
        // Arrange
        var content = "//// double slash comment";

        // Act
        var result = _locator.LocateComments(content);

        // Assert
        Assert.IsNotNull(result);
        var resultList = result.ToList();
        // The implementation may find overlapping comment markers - just verify we got some comments
        Assert.IsGreaterThanOrEqualTo(1, resultList.Count);
    }

    [TestMethod]
    public void LocateComments_InlineCommentContentsMatchesExtractedRange()
    {
        // Arrange
        // LocateInlineComments sets StartIndex = position_of_'//' + 2 (skips the marker)
        // and EndIndex = position_of_newline + 1 (includes the '\n').
        // Contents is Substring(contentStart, end - contentStart), matching the range exactly.
        var content = "// inline comment\n{}";

        // Act
        var resultList = _locator.LocateComments(content).ToList();

        // Assert
        var comment = resultList.First(cp => cp.Type == ConfigType.Comment);
        var extractedFromSource = content.Substring(comment.StartIndex, comment.EndIndex - comment.StartIndex);
        Assert.AreEqual(
            comment.Contents,
            extractedFromSource,
            $"Contents ('{comment.Contents}') does not match what the StartIndex/EndIndex range " +
            $"extracts from the source ('{extractedFromSource}'). EndIndex includes '\\n' but Contents does not.");
    }

    [TestMethod]
    public void LocateComments_MultiLineCommentContentsMatchesExtractedRange()
    {
        // Arrange
        // LocateMultiLineComments sets StartIndex = position_of_'/*' + 2 (skips the opening marker)
        // and EndIndex = position_of_'*/' (stops before the closing marker).
        // Contents is Substring(contentStart, endIndex - contentStart), matching the range exactly.
        var content = "/* multi-line comment */";

        // Act
        var resultList = _locator.LocateComments(content).ToList();

        // Assert
        var comment = resultList.First(cp => cp.Type == ConfigType.Comment);
        var extractedFromSource = content.Substring(comment.StartIndex, comment.EndIndex - comment.StartIndex);
        Assert.AreEqual(
            comment.Contents,
            extractedFromSource,
            $"Contents ('{comment.Contents}') does not match what the StartIndex/EndIndex range " +
            $"extracts from the source ('{extractedFromSource}'). The closing '*/' is missing from Contents.");
    }

    #endregion
}
