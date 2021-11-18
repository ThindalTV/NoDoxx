using NoDoxx.Interfaces;
using System;
using System.Collections.Generic;
using System.Xml;

namespace NoDoxx.ValueLocators
{
    internal class XmlValueLocator : IValueLocator
    {
        public IEnumerable<ConfigPosition> FindConfigValues(string fileContent)
        {
            return (HideXml(fileContent));
        }

        private IEnumerable<ConfigPosition> HideXml(string fullContents, string contents = null, int contentsStartIndex = 0)
        {
            var ret = new List<ConfigPosition>();

            if (contents == null) contents = fullContents;

            if( !contents.StartsWith("<"))
            {
                var valueStopIndex = contents.IndexOf("<");
                var bareWords = contents.Substring(0, valueStopIndex != -1 ? valueStopIndex : contents.Length);
                var valueStartIndex = fullContents.IndexOf($">{bareWords}<") + 1;
                if( valueStopIndex == -1)
                {
                    ret.Add(new ConfigPosition(valueStartIndex, valueStartIndex + contents.Length));
                    return ret;
                }
                ret.Add(new ConfigPosition(valueStartIndex, valueStopIndex + valueStartIndex));
                contents = contents.Substring(valueStopIndex);
            }

            if (String.IsNullOrWhiteSpace(contents)) return ret;

            try
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(contents);

                if (xmlDocument[xmlDocument.DocumentElement.Name].Attributes != null &&
                    xmlDocument[xmlDocument.DocumentElement.Name].Attributes.Count > 0)
                {
                    foreach (XmlNode attr in xmlDocument[xmlDocument.DocumentElement.Name].Attributes)
                    {
                        var tagStartIndex = -1;
                        while ((tagStartIndex = fullContents.IndexOf(attr.OuterXml, tagStartIndex + 1)) != -1)
                        {
                            var valueStartIndex = fullContents.IndexOf(attr.InnerText, tagStartIndex);
                            var valueStopIndex = valueStartIndex + attr.InnerText.Length;
                            ret.Add(new ConfigPosition(valueStartIndex, valueStopIndex));
                        }
                    }
                }

                foreach (var tag in xmlDocument)
                {
                    var xTag = tag as XmlElement; // Or if child element is content(pure text), hide it right away
                    if (xTag != null)
                    {
                        foreach (XmlNode c in xTag.ChildNodes)
                        {
                            if( c.NodeType == XmlNodeType.Text)
                            {
                                var valueStartIndex = fullContents.IndexOf(">" + c.Value) + 1;
                                var valueStopIndex = valueStartIndex + c.Value.Length;
                                ret.Add(new ConfigPosition(valueStartIndex, valueStopIndex));
                                continue;
                            }
                            ret.AddRange(HideXml(fullContents, c.InnerXml, 0));
                        }
                    }
                }
            } catch(XmlException xex)
            {
                if( xex.Message.StartsWith("Data at the root level is invalid."))
                {
                    return ret;
                } else
                {
                    throw;
                }
            }
            catch(Exception ex)
            {
                // Not valid xml, probably means it's bare text
                var startIndex = fullContents.IndexOf(contents);
                ret.Add(new ConfigPosition(startIndex, startIndex + contents.Length));
            }
            return ret;
        }
    }
}
