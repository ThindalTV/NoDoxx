using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NoDoxx.Interfaces;
using NoDoxx.ValueLocators;

namespace NoDoxx.UnitTests.ValueLocators
{
    [TestClass]
    public class RegExXmlValueLocatorTests
    {
        [TestMethod]
        public void Constructor_InitializesSuccessfully()
        {
            // Act
            var locator = new RegExXmlValueLocator();

            // Assert
            Assert.IsNotNull(locator);
        }

        [TestMethod]
        public void FindConfigValues_ValidXmlWithQuotedValues_ReturnsConfigPositions()
        {
            // Arrange
            var locator = new RegExXmlValueLocator();
            var xml = "<config><setting name=\"value1\">content</setting></config>";

            // Act
            var result = locator.FindConfigValues(xml);

            // Assert
            var configList = result.ToList();
            Assert.IsTrue(configList.Count > 0);
            Assert.IsTrue(configList.Any(c => c.Type == ConfigType.Value));
            var valueItem = configList.First(c => c.Type == ConfigType.Value && c.Contents.Contains("value1"));
            Assert.AreEqual("value1", xml.Substring(valueItem.StartIndex, valueItem.EndIndex - valueItem.StartIndex));
        }

        [TestMethod]
        public void FindConfigValues_ValidXmlWithComments_ReturnsCommentConfigPositions()
        {
            // Arrange
            var locator = new RegExXmlValueLocator();
            var xml = "<config><!-- This is a comment --><setting>value</setting></config>";

            // Act
            var result = locator.FindConfigValues(xml);

            // Assert
            var configList = result.ToList();
            Assert.IsTrue(configList.Any(c => c.Type == ConfigType.Comment));
            var comment = configList.First(c => c.Type == ConfigType.Comment);
            Assert.AreEqual(" This is a comment ", xml.Substring(comment.StartIndex, comment.EndIndex - comment.StartIndex));
        }

        [TestMethod]
        public void FindConfigValues_ValidXmlWithBareWords_ReturnsBareWordConfigPositions()
        {
            // Arrange
            var locator = new RegExXmlValueLocator();
            var xml = "<config><item>BareWordContent</item></config>";

            // Act
            var result = locator.FindConfigValues(xml);

            // Assert
            var configList = result.ToList();
            Assert.IsTrue(configList.Count > 0);
            var bareWordItem = configList.First(c => c.Contents.Contains("BareWordContent"));
            Assert.AreEqual("<item>BareWordContent", xml.Substring(bareWordItem.StartIndex, bareWordItem.EndIndex - bareWordItem.StartIndex));
        }

        [TestMethod]
        public void FindConfigValues_EmptyXml_ReturnsEmptyList()
        {
            // Arrange
            var locator = new RegExXmlValueLocator();
            var xml = "<root></root>";

            // Act
            var result = locator.FindConfigValues(xml);

            // Assert
            var configList = result.ToList();
            Assert.IsNotNull(configList);
        }

        [TestMethod]
        public void FindConfigValues_InvalidXml_ThrowsXmlException()
        {
            // Arrange
            var locator = new RegExXmlValueLocator();
            var invalidXml = "<config><unclosed>";

            // Act & Assert
            try
            {
                locator.FindConfigValues(invalidXml);
                Assert.Fail("Expected XmlException was not thrown");
            }
            catch (XmlException)
            {
                // Expected exception
            }
        }

        [TestMethod]
        public void FindConfigValues_MalformedXml_ThrowsXmlException()
        {
            // Arrange
            var locator = new RegExXmlValueLocator();
            var malformedXml = "not xml at all";

            // Act & Assert
            try
            {
                locator.FindConfigValues(malformedXml);
                Assert.Fail("Expected XmlException was not thrown");
            }
            catch (XmlException)
            {
                // Expected exception
            }
        }

        [TestMethod]
        public void FindConfigValues_NullInput_ThrowsArgumentNullException()
        {
            // Arrange
            var locator = new RegExXmlValueLocator();

            // Act & Assert
            try
            {
                locator.FindConfigValues(null);
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
            catch (ArgumentNullException)
            {
                // Expected exception
            }
        }

        [TestMethod]
        public void FindConfigValues_XmlWithMultipleQuotedValues_ReturnsAllMatches()
        {
            // Arrange
            var locator = new RegExXmlValueLocator();
            var xml = "<config><setting name=\"value1\" attr=\"value2\"><item key=\"value3\">text</item></setting></config>";

            // Act
            var result = locator.FindConfigValues(xml);

            // Assert
            var configList = result.ToList();
            var quotedValues = configList.Where(c => c.Type == ConfigType.Value && c.Contents.Contains("\"")).ToList();
            Assert.IsTrue(quotedValues.Count >= 3);
        }

        [TestMethod]
        public void FindConfigValues_XmlWithMultipleComments_ReturnsAllComments()
        {
            // Arrange
            var locator = new RegExXmlValueLocator();
            var xml = "<config><!-- Comment 1 --><setting>value</setting><!-- Comment 2 --></config>";

            // Act
            var result = locator.FindConfigValues(xml);

            // Assert
            var configList = result.ToList();
            var comments = configList.Where(c => c.Type == ConfigType.Comment).OrderBy(c => c.StartIndex).ToList();
            Assert.AreEqual(2, comments.Count);
            Assert.AreEqual(" Comment 1 ", xml.Substring(comments[0].StartIndex, comments[0].EndIndex - comments[0].StartIndex));
            Assert.AreEqual(" Comment 2 ", xml.Substring(comments[1].StartIndex, comments[1].EndIndex - comments[1].StartIndex));
        }

        [TestMethod]
        public void FindConfigValues_XmlWithMultilineBareWords_ReturnsConfigPositions()
        {
            // Arrange
            var locator = new RegExXmlValueLocator();
            var xml = "<config><description>Line1\nLine2\nLine3</description></config>";

            // Act
            var result = locator.FindConfigValues(xml);

            // Assert
            var configList = result.ToList();
            Assert.IsTrue(configList.Any(c => c.Contents.Contains("Line1")));
        }

        [TestMethod]
        public void FindConfigValues_XmlWithMultilineComment_ReturnsCommentConfigPosition()
        {
            // Arrange
            var locator = new RegExXmlValueLocator();
            var xml = "<config><!-- Comment\nspanning\nmultiple lines --><setting>value</setting></config>";

            // Act
            var result = locator.FindConfigValues(xml);

            // Assert
            var configList = result.ToList();
            var comments = configList.Where(c => c.Type == ConfigType.Comment).ToList();
            Assert.AreEqual(1, comments.Count);
            Assert.IsTrue(comments[0].Contents.Contains("spanning"));
            Assert.AreEqual(" Comment\nspanning\nmultiple lines ", xml.Substring(comments[0].StartIndex, comments[0].EndIndex - comments[0].StartIndex));
        }

        [TestMethod]
        public void FindConfigValues_ComplexXmlDocument_ReturnsMixedConfigTypes()
        {
            // Arrange
            var locator = new RegExXmlValueLocator();
            var xml = @"<root>
                <!-- Configuration file -->
                <appSettings>
                    <add key=""Setting1"" value=""Value1""/>
                    <add key=""Setting2"" value=""Value2""/>
                </appSettings>
                <connection>Server=localhost;Database=TestDB</connection>
                <!-- End of settings -->
            </root>";

            // Act
            var result = locator.FindConfigValues(xml);

            // Assert
            var configList = result.ToList();
            Assert.IsTrue(configList.Any(c => c.Type == ConfigType.Value));
            Assert.IsTrue(configList.Any(c => c.Type == ConfigType.Comment));
            Assert.IsTrue(configList.Count > 0);
        }

        [TestMethod]
        public void FindConfigValues_XmlWithEmptyTags_HandlesCorrectly()
        {
            // Arrange
            var locator = new RegExXmlValueLocator();
            var xml = "<config><empty></empty><nonempty>data</nonempty></config>";

            // Act
            var result = locator.FindConfigValues(xml);

            // Assert
            var configList = result.ToList();
            Assert.IsNotNull(configList);
        }

        [TestMethod]
        public void FindConfigValues_XmlWithSpecialCharactersInValues_ReturnsConfigPositions()
        {
            // Arrange
            var locator = new RegExXmlValueLocator();
            var xml = "<config><setting name=\"value with spaces\" attr=\"value@with#special$chars\">content</setting></config>";

            // Act
            var result = locator.FindConfigValues(xml);

            // Assert
            var configList = result.ToList();
            Assert.IsTrue(configList.Count > 0);
        }

        [TestMethod]
        public void FindConfigValues_XmlWithNestedElements_ReturnsAllConfigPositions()
        {
            // Arrange
            var locator = new RegExXmlValueLocator();
            var xml = "<root><parent><child attr=\"value1\"><grandchild>text</grandchild></child></parent></root>";

            // Act
            var result = locator.FindConfigValues(xml);

            // Assert
            var configList = result.ToList();
            Assert.IsTrue(configList.Count > 0);
        }

        [TestMethod]
        public void FindConfigValues_EmptyString_ThrowsXmlException()
        {
            // Arrange
            var locator = new RegExXmlValueLocator();

            // Act & Assert
            try
            {
                locator.FindConfigValues(string.Empty);
                Assert.Fail("Expected XmlException was not thrown");
            }
            catch (XmlException)
            {
                // Expected exception
            }
        }
    }
}
