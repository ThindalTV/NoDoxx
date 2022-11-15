using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using NoDoxx.Interfaces;

namespace NoDoxx.ValueLocators
{
    internal class RegExXmlValueLocator : IValueLocator
    {
        private const string VALUE_REGEX = "\"([^\"]+?)\"";
        private const string COMMENT_REGEX = "<!--(\\n|.)*-->";
        private const string BAREWORD_REGEX = ">([\\s\\S])*?<";

        private readonly Regex _valueRegEx;
        private readonly Regex _commentRegEx;
        private readonly Regex _bareWordRegEx;

        public RegExXmlValueLocator()
        {
            _valueRegEx = new Regex(VALUE_REGEX);
            _commentRegEx = new Regex(COMMENT_REGEX);
            _bareWordRegEx = new Regex(BAREWORD_REGEX);
        }

        public IEnumerable<ConfigPosition> FindConfigValues(string fileContent)
        {
            /* Verify that document is proper XML by loading it.
               If it's invalid then XmlDocument will throw and it will be caught in the adorner. */
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(fileContent);

            // Locate matches for the value regex
            List<ConfigPosition> configValues = new List<ConfigPosition>();
            configValues.AddRange(LocateMatch(fileContent, _valueRegEx, ConfigType.Value, "\"", "\""));
            configValues.AddRange(LocateMatch(fileContent, _bareWordRegEx, ConfigType.Value, ">", "<", hideEmpty: false));
            configValues.AddRange(LocateMatch(fileContent, _commentRegEx, ConfigType.Comment, "<!--", "-->"));

            return configValues;
        }

        private List<ConfigPosition> LocateMatch(string fileContent, Regex regex, ConfigType type, string startTag = "", string endTag = "", bool hideEmpty = true)
        {
            var matches = regex.Matches(fileContent);
            var configValues = new List<ConfigPosition>();

            var startLength = startTag.Length;
            var endLength = endTag.Length;

            foreach (Match match in matches)
            {
                if (!hideEmpty && 
                        match.Value
                        .Replace(" ", "")
                        .Replace("\t", "")
                        .Replace(Environment.NewLine, "")
                        .Replace(startTag, "")
                        .Replace(endTag, "")
                        .Length == 0)
                    continue;

                configValues.Add(new ConfigPosition(
                    match.Index + startLength,
                    match.Index + match.Length - endLength,
                    type,
                    match.Value
                    ));
            }

            return configValues;
        }
    }
}
