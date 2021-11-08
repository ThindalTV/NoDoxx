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
            return(HideXml(fileContent));
        }

        private IEnumerable<ConfigPosition> HideXml(string fullContents, string contents = null, int contentsStartIndex = 0)
        {
            var ret = new List<ConfigPosition>();

            if (contents == null) contents = fullContents;

            if (String.IsNullOrWhiteSpace(contents)) return ret;

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(contents);

            if (xmlDocument[xmlDocument.DocumentElement.Name].Attributes != null &&
                xmlDocument[xmlDocument.DocumentElement.Name].Attributes.Count > 0)
            {
                foreach (XmlNode attr in xmlDocument[xmlDocument.DocumentElement.Name].Attributes)
                {
                    var tagStartIndex = -1;
                    while ((tagStartIndex = fullContents.IndexOf(attr.OuterXml, tagStartIndex+1)) != -1)
                    {
                        var valueStartIndex = fullContents.IndexOf(attr.InnerText, tagStartIndex);
                        var valueStopIndex = valueStartIndex + attr.InnerText.Length;
                        ret.Add(new ConfigPosition(valueStartIndex, valueStopIndex));
                    }
                }
            }

            foreach (var tag in xmlDocument.ChildNodes)
            {
                var xTag = tag as System.Xml.XmlElement; // Or if child element is content(pure text), hide it right away
                if (xTag != null)
                {
                    ret.AddRange(HideXml(fullContents, xTag.InnerXml, 0));
                }
            }
            return ret;
        }
    }
}
