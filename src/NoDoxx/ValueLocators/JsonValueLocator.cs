using NoDoxx.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace NoDoxx.ValueLocators
{

    internal class JsonValueLocator : IValueLocator
    {
        public IEnumerable<ConfigPosition> FindConfigValues(string fileContent)
        {
            try
            {
                using var _ = JsonDocument.Parse(fileContent, new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });
            }
            catch (Exception ex)
            {
                return new List<ConfigPosition>()
                {
                    new ConfigPosition(0, fileContent.Length, ConfigType.Value, ContentsType.Null, ex.ToString())
                };
            }

            // Find all the values in the JSON file
            var hits = LocalJsonValues(fileContent).Distinct().ToList();
            // Remove overlapping values
            RemoveIfIsInside(hits, hits);

            // Find all property names in the json file
            var names = LocateNames(fileContent).Distinct().ToList();



            // Find all the comments in the JSON file
            var comments = LocateComments(fileContent).Distinct().ToList();

            // Remove comments if the start marker is INSIDE of a value or name field
            var hitsWithNames = new List<ConfigPosition>();
            hitsWithNames.AddRange(hits);
            hitsWithNames.AddRange(names);
            RemoveIfStartsInside(comments, hitsWithNames);

            // Add the comments to the hits
            hits.AddRange(comments);
            return hits;
        }

        private void RemoveIfStartsInside(List<ConfigPosition> values, List<ConfigPosition> checks)
        {
            foreach (var check in checks)
            {
                values.RemoveAll(x => x.StartIndex >= check.StartIndex && x.StartIndex < check.EndIndex);
            }
        }

        private void RemoveIfIsInside(List<ConfigPosition> values, List<ConfigPosition> duplicates)
        {
            var toRemove = new List<ConfigPosition>();
            foreach (var val in values.Where(v => v.Type == ConfigType.Value))
            {
                foreach (var dupl in duplicates)
                {
                    // If dupl starts & ends inside of val, remove it
                    if (val.IsInside(dupl))
                    {
                        toRemove.Add(val);
                    }
                }
            }

            values.RemoveAll(toRemove.Contains);
        }

        private IEnumerable<ConfigPosition> LocateNames(string fileContent)
        {
            // Look for ':' that are outside of string values to locate property names.
            // Tracking inString prevents colons embedded in values (e.g. "https://") from
            // producing false Name regions that would incorrectly filter out nearby comments.
            var ret = new List<ConfigPosition>();
            bool inString = false;

            for (int i = 0; i < fileContent.Length; i++)
            {
                char c = fileContent[i];

                if (c == '"' && (i == 0 || fileContent[i - 1] != '\\'))
                {
                    inString = !inString;
                    continue;
                }

                if (inString || c != ':')
                    continue;

                // Found a ':' outside a string — look backwards for the enclosing property name.
                var quoteIndex = fileContent.LastIndexOf('"', i - 1);
                if (quoteIndex == -1) continue;
                var nameStartIndex = fileContent.LastIndexOf('"', quoteIndex - 1);
                if (nameStartIndex == -1) continue;
                var name = fileContent.Substring(nameStartIndex + 1, quoteIndex - nameStartIndex - 1);
                ret.Add(new ConfigPosition(nameStartIndex, quoteIndex + 1, ConfigType.Name, ContentsType.String, name));
            }

            return ret;
        }

        private IEnumerable<ConfigPosition> LocalJsonValues(string fullJson)
        {
            var returnValue = new List<ConfigPosition>();
            var bytes = Encoding.UTF8.GetBytes(fullJson);

            var reader = new Utf8JsonReader(bytes, new JsonReaderOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            try
            {
                while (reader.Read())
                {
                    if (reader.TokenType is not (JsonTokenType.String
                                                or JsonTokenType.Number
                                                or JsonTokenType.True
                                                or JsonTokenType.False
                                                or JsonTokenType.Null))
                        continue;

                    int startByte = (int)reader.TokenStartIndex;

                    // ValueSpan holds the raw source bytes of the token value:
                    // for strings, the content between the quotes (escapes unresolved);
                    // for primitives, the literal text (true/false/null/number).
                    // Using ValueSpan for lengths gives correct source positions even
                    // when escape sequences are present (unlike GetString()).
                    var rawValueBytes = reader.ValueSpan.ToArray();

                    int charStart = Encoding.UTF8.GetCharCount(bytes, 0, startByte);
                    int rawValueCharLen = Encoding.UTF8.GetCharCount(rawValueBytes);

                    int contentStart, contentEnd;
                    string contents;

                    if (reader.TokenType == JsonTokenType.String)
                    {
                        contentStart = charStart + 1; // skip opening "
                        contentEnd   = contentStart + rawValueCharLen;
                        contents     = reader.GetString();
                    }
                    else
                    {
                        contentStart = charStart;
                        contentEnd   = charStart + rawValueCharLen;
                        contents     = Encoding.UTF8.GetString(rawValueBytes);
                    }

                    returnValue.Add(new ConfigPosition(
                        contentStart, contentEnd,
                        ConfigType.Value,
                        reader.TokenType == JsonTokenType.String ? ContentsType.String : ContentsType.Null,
                        contents));
                }
            }
            catch (JsonException ex)
            {
                return new List<ConfigPosition>
                {
                    new ConfigPosition(0, fullJson.Length, ConfigType.Value, ContentsType.Null, ex.ToString())
                };
            }

            return returnValue;
        }

        internal IEnumerable<ConfigPosition> LocateComments(string contents)
        {
            var returnValue = new List<ConfigPosition>();

            // Locate line comments
            returnValue.AddRange(LocateInlineComments(contents));

            // Locate multiline comments
            returnValue.AddRange(LocateMultiLineComments(contents));

            return returnValue;
        }

        private IEnumerable<ConfigPosition> LocateInlineComments(string contents)
        {
            var returnValue = new List<ConfigPosition>();
            int position = 0;
            while ((position = contents.IndexOf("//", position)) != -1)
            {
                var end = contents.IndexOf('\n', position + "//".Length);
                if (end == -1)
                {
                    end = contents.Length;
                }
                else
                {
                    end = end + 1; // include the '\n' in both the range and the stored content
                }
                // Start after '//' so the marker remains visible; only the content is hidden.
                var contentStart = position + "//".Length;
                returnValue.Add(new ConfigPosition(contentStart, end, ConfigType.Comment, ContentsType.String, contents.Substring(contentStart, end - contentStart)));
                position = position + 1;
            }
            return returnValue;
        }

        private IEnumerable<ConfigPosition> LocateMultiLineComments(string contents)
        {
            var returnValue = new List<ConfigPosition>();

            int position = 0;
            while (position < contents.Length && (position = contents.IndexOf("/*", position)) != -1)
            {
                // Start after '/*' so the opening marker remains visible; only the content is hidden.
                var contentStart = position + "/*".Length;
                var end = contents.IndexOf("*/", position + "/*".Length);
                int endIndex;
                if (end == -1)
                {
                    // Unclosed comment — span to the end of the file
                    endIndex = contents.Length;
                }
                else
                {
                    // Stop before '*/' so the closing marker remains visible
                    endIndex = end;
                }

                returnValue.Add(new ConfigPosition(contentStart, endIndex, ConfigType.Comment, ContentsType.String, contents.Substring(contentStart, endIndex - contentStart)));

                position = position + 1;
            }

            return returnValue;
        }
    }
}
