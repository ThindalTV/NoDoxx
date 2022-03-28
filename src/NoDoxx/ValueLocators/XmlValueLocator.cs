using NoDoxx.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace NoDoxx.ValueLocators
{
    internal class XmlValueLocator : IValueLocator
    {
        public IEnumerable<ConfigPosition> FindConfigValues(string fileContent)
        {
            var values = HideXml(fileContent).ToList();
            values.AddRange(LocateComments(fileContent));
            return values;
        }

        private IEnumerable<ConfigPosition> HideXml(string fullContents, string contents = null, int contentsStartIndex = 0)
        {
            var ret = new List<ConfigPosition>();

            if (contents == null) contents = fullContents;

            // Break out of comments
            if (contents.StartsWith("<!--") && contents.EndsWith("-->")) return ret;

            // Bare text
            if( !contents.StartsWith("<"))
            {
                var valueStopIndex = contents.IndexOf("<");
                var bareWords = contents.Substring(0, valueStopIndex != -1 ? valueStopIndex : contents.Length);
                var valueStartIndex = fullContents.IndexOf($">{bareWords}<") + 1;
                if( valueStopIndex == -1)
                {
                    ret.Add(new ConfigPosition(valueStartIndex, valueStartIndex + contents.Length, ConfigType.Value));
                    return ret;
                }
                ret.Add(new ConfigPosition(valueStartIndex, valueStopIndex + valueStartIndex, ConfigType.Value));
                contents = contents.Substring(valueStopIndex);
            }

            if (String.IsNullOrWhiteSpace(contents)) return ret;

            // Xml
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
                            ret.Add(new ConfigPosition(valueStartIndex, valueStopIndex, ConfigType.Value));
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
                                var valueStartIndex = 0;
                                while( (valueStartIndex = fullContents.IndexOf($">{c.Value}<", valueStartIndex)) > 0) {
                                    valueStartIndex++;
                                    var valueStopIndex = valueStartIndex + c.Value.Length;
                                    ret.Add(new ConfigPosition(valueStartIndex, valueStopIndex, ConfigType.Value));
                                }
                                continue;
                            }
                            ret.AddRange(HideXml(fullContents, c.OuterXml, 0));
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
            catch(Exception)
            {
                // Not valid xml, probably means it's bare text
                var startIndex = fullContents.IndexOf(contents);
                ret.Add(new ConfigPosition(startIndex, startIndex + contents.Length, ConfigType.Value));
            }
            return ret;
        }

        internal IEnumerable<ConfigPosition> LocateComments(string contents)
        {
            var ret = new List<ConfigPosition>();

            int position = 0;
            while((position = contents.IndexOf("<!--", position)) != -1)
            {
                var start = position;
                var end = contents.IndexOf("-->", position + "<!--".Length);
                if( end == -1)
                {
                    end = contents.Length;
                }

                ret.Add(new ConfigPosition(start, end + "-->".Length, ConfigType.Comment));
                
                position = end;
            }

            return ret;
        }
    }
}
