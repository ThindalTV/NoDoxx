using System;
using System.Collections.Generic;
using NoDoxx.Interfaces;

namespace NoDoxx.ValueLocators
{
    internal class EnvValueLocator : IValueLocator
    {
        public IEnumerable<ConfigPosition> FindConfigValues(string fileContent)
        {
            if (fileContent == null) throw new ArgumentNullException(nameof(fileContent));

            var results = new List<ConfigPosition>();
            var lines = fileContent.Split('\n');
            int offset = 0;

            foreach (var rawLine in lines)
            {
                // rawLine may contain a trailing '\r' on Windows line endings
                var line = rawLine.TrimEnd('\r');
                var trimmed = line.TrimStart();
                var leadingSpaces = line.Length - trimmed.Length;

                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    if (trimmed[0] == '#')
                    {
                        // Full-line comment: emit the text after the '#'
                        int commentStart = offset + leadingSpaces + 1;
                        int commentEnd = offset + line.Length;
                        if (commentEnd > commentStart)
                            results.Add(new ConfigPosition(commentStart, commentEnd, ConfigType.Comment, ContentsType.String, trimmed.Substring(1)));
                    }
                    else
                    {
                        int eqIndex = line.IndexOf('=');
                        if (eqIndex > 0)
                        {
                            // Key name (left of first '=')
                            string keyName = line.Substring(leadingSpaces, eqIndex - leadingSpaces).TrimEnd();
                            if (!string.IsNullOrEmpty(keyName))
                            {
                                int keyStart = offset + leadingSpaces;
                                results.Add(new ConfigPosition(keyStart, keyStart + keyName.Length, ConfigType.Name, ContentsType.String, keyName));
                            }

                            // Value (right of first '=')
                            string rawValue = line.Substring(eqIndex + 1);
                            int valueAbsoluteStart = offset + eqIndex + 1;

                            if (!string.IsNullOrEmpty(rawValue))
                            {
                                char first = rawValue[0];
                                char last = rawValue[rawValue.Length - 1];

                                if (rawValue.Length >= 2
                                    && ((first == '"' && last == '"') || (first == '\'' && last == '\'')))
                                {
                                    // Quoted value: hide the content inside the quotes
                                    string innerValue = rawValue.Substring(1, rawValue.Length - 2);
                                    if (!string.IsNullOrEmpty(innerValue))
                                        results.Add(new ConfigPosition(
                                            valueAbsoluteStart + 1,
                                            valueAbsoluteStart + rawValue.Length - 1,
                                            ConfigType.Value, ContentsType.String, innerValue));
                                }
                                else
                                {
                                    // Unquoted value: hide to end of line (trailing whitespace excluded)
                                    string trimmedValue = rawValue.TrimEnd();
                                    if (!string.IsNullOrEmpty(trimmedValue))
                                        results.Add(new ConfigPosition(
                                            valueAbsoluteStart,
                                            valueAbsoluteStart + trimmedValue.Length,
                                            ConfigType.Value, ContentsType.String, trimmedValue));
                                }
                            }
                        }
                    }
                }

                // +1 to account for the '\n' that Split removed
                offset += rawLine.Length + 1;
            }

            return results;
        }
    }
}
