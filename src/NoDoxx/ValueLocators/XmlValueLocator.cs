using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using NoDoxx.Interfaces;

namespace NoDoxx.ValueLocators
{
    internal class XmlValueLocator : IValueLocator
    {
        public IEnumerable<ConfigPosition> FindConfigValues(string fileContent)
        {
            if (fileContent == null) throw new ArgumentNullException(nameof(fileContent));

            // Validate: if the content is not well-formed XML, hide everything rather than throwing
            var xmlDocument = new XmlDocument();
            try
            {
                xmlDocument.LoadXml(fileContent);
            }
            catch (XmlException ex)
            {
                return new List<ConfigPosition>
                {
                    new ConfigPosition(0, fileContent.Length, ConfigType.Value, ContentsType.Null, ex.ToString())
                };
            }

            var results = new List<ConfigPosition>();
            int[] lineOffsets = BuildLineOffsets(fileContent);

            using (var xmlTextReader = new XmlTextReader(new StringReader(fileContent)))
            {
                xmlTextReader.DtdProcessing = DtdProcessing.Ignore;
                var lineInfo = (IXmlLineInfo)xmlTextReader;

                while (xmlTextReader.Read())
                {
                    switch (xmlTextReader.NodeType)
                    {
                        case XmlNodeType.Text:
                        {
                            if (string.IsNullOrWhiteSpace(xmlTextReader.Value)) break;
                            int textStart = ToAbsolute(lineOffsets, lineInfo.LineNumber, lineInfo.LinePosition);
                            // Find the closing '<' in raw source to correctly handle entity refs
                            int textEnd = fileContent.IndexOf('<', textStart);
                            if (textEnd == -1) textEnd = fileContent.Length;
                            results.Add(new ConfigPosition(textStart, textEnd, ConfigType.Value, ContentsType.String, xmlTextReader.Value));
                            break;
                        }
                        case XmlNodeType.CDATA:
                        {
                            if (string.IsNullOrWhiteSpace(xmlTextReader.Value)) break;
                            int cdataStart = ToAbsolute(lineOffsets, lineInfo.LineNumber, lineInfo.LinePosition);
                            int cdataEnd = fileContent.IndexOf("]]>", cdataStart, StringComparison.Ordinal);
                            if (cdataEnd == -1) cdataEnd = fileContent.Length;
                            results.Add(new ConfigPosition(cdataStart, cdataEnd, ConfigType.Value, ContentsType.String, xmlTextReader.Value));
                            break;
                        }
                        case XmlNodeType.Comment:
                        {
                            // IXmlLineInfo for Comment nodes points to the first content char (after <!--)
                            int contentStart = ToAbsolute(lineOffsets, lineInfo.LineNumber, lineInfo.LinePosition);
                            int contentEnd = fileContent.IndexOf("-->", contentStart, StringComparison.Ordinal);
                            if (contentEnd == -1) contentEnd = fileContent.Length;
                            string commentContent = fileContent.Substring(contentStart, contentEnd - contentStart);
                            results.Add(new ConfigPosition(contentStart, contentEnd, ConfigType.Comment, ContentsType.String, commentContent));
                            break;
                        }
                        case XmlNodeType.Element:
                        {
                            while (xmlTextReader.MoveToNextAttribute())
                            {
                                // IXmlLineInfo points to first char of the attribute name
                                int attrNamePos = ToAbsolute(lineOffsets, lineInfo.LineNumber, lineInfo.LinePosition);
                                int pos = attrNamePos + xmlTextReader.Name.Length;

                                // Scan to '='
                                while (pos < fileContent.Length && fileContent[pos] != '=') pos++;
                                pos++; // skip '='

                                // Scan past optional whitespace
                                while (pos < fileContent.Length && (fileContent[pos] == ' ' || fileContent[pos] == '\t')) pos++;
                                if (pos >= fileContent.Length) continue;

                                char quote = fileContent[pos];
                                int valueStart = pos + 1;
                                // Search raw source for closing quote to correctly handle entity refs
                                int valueEnd = fileContent.IndexOf(quote, valueStart);
                                if (valueEnd == -1) valueEnd = fileContent.Length;

                                if (valueEnd > valueStart)
                                    results.Add(new ConfigPosition(valueStart, valueEnd, ConfigType.Value, ContentsType.String, xmlTextReader.Value));
                            }
                            break;
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Builds a table mapping 1-based line numbers to their absolute character offsets.
        /// lineOffsets[0] is always 0 (line 1 starts at char 0).
        /// </summary>
        private static int[] BuildLineOffsets(string text)
        {
            var offsets = new List<int> { 0 };
            for (int i = 0; i < text.Length; i++)
                if (text[i] == '\n') offsets.Add(i + 1);
            return offsets.ToArray();
        }

        /// <summary>
        /// Converts a 1-based IXmlLineInfo (line, column) pair to a 0-based absolute char offset.
        /// </summary>
        private static int ToAbsolute(int[] lineOffsets, int line, int col)
            => lineOffsets[line - 1] + col - 1;
    }
}
