using NoDoxx.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace NoDoxx.ValueLocators
{

    internal class JsonValueLocator : IValueLocator
    {
        public IEnumerable<ConfigPosition> FindConfigValues(string fileContent)
        {
            try
            {
                _ = JsonSerializer.Deserialize<Dictionary<string, object>>(fileContent, new JsonSerializerOptions()
                {
                    ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true
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

            var hitsWithNames = new List<ConfigPosition>();
            hitsWithNames.AddRange(hits);
            hitsWithNames.AddRange(names);

            // Find all the comments in the JSON file
            var comments = LocateComments(fileContent).Distinct().ToList();
            // Remove comments if the start marker is INSIDE of a value or name field
            RemoveIfStartsInside(comments, hitsWithNames);

            
            // Remove partial hits from "outside" hits

            //RemoveIfIsInside(hits, names);
            //RemoveIfStartsInside(hits, comments);


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
            // Look for : and locate the property name
            var ret = new List<ConfigPosition>();
            var startIndex = 0;
            while (startIndex < fileContent.Length)
            {
                var colonIndex = fileContent.IndexOf(':', startIndex);
                if (colonIndex == -1) break;
                var quoteIndex = fileContent.LastIndexOf('"', colonIndex);
                if (quoteIndex == -1) break;
                var nameStartIndex = fileContent.LastIndexOf('"', quoteIndex - 1);
                if (nameStartIndex == -1) break;
                var nameStopIndex = fileContent.IndexOf('"', nameStartIndex + 1);
                if (nameStopIndex == -1) break;
                var name = fileContent.Substring(nameStartIndex + 1, nameStopIndex - nameStartIndex - 1);
                ret.Add(new ConfigPosition(nameStartIndex, nameStopIndex + 1, ConfigType.Name, ContentsType.String, name));
                startIndex = colonIndex + 1;
            }
            return ret;
        }

        private IEnumerable<ConfigPosition> LocalJsonValues(string fullJson, string json = null)
        {
            var returnValue = new List<ConfigPosition>();
            if (json == null) json = fullJson;

            JsonDocument jsonObject;
            // Check for valid json
            try
            {
                jsonObject = JsonDocument.Parse(json, new JsonDocumentOptions() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            }
            catch (Exception)
            {
                var start = fullJson.IndexOf(json);
                var end = start + json.Length;
                returnValue.Add(new ConfigPosition(start, end, ConfigType.Value, ContentsType.Null, "ERROR, cannot parse"));
                return returnValue;
            }

            returnValue.AddRange(DetermineConfigPositions(fullJson, jsonObject, json));
            return returnValue;
        }

        private IEnumerable<ConfigPosition> DetermineConfigPositions(string fullJsonString, JsonDocument jsonDocument, string json = null)
        {
            var returnValue = new List<ConfigPosition>();
            foreach (var jsonObject in jsonDocument.RootElement.EnumerateObject())
            {
                returnValue.AddRange(GetConfigPosition(jsonObject, fullJsonString, json));
            }
            return returnValue;
        }

        private IEnumerable<ConfigPosition> GetConfigPosition(JsonProperty jsonObject, string fullJsonString, string json = null)
        {
            var returnValue = new List<ConfigPosition>();
            var fullObjectString = jsonObject.ToString();
            var objString = jsonObject.Value.ToString();
            int jsonIndex = fullJsonString.IndexOf(fullObjectString);

            while (jsonIndex != -1)
            {
                if (jsonObject.Value.ValueKind == JsonValueKind.Object)
                {
                    var locatedConfigPositions = new List<ConfigPosition>();

                    // Locate json values inside of the object
                    locatedConfigPositions.AddRange(LocalJsonValues(fullJsonString, objString));

                    return locatedConfigPositions;
                }

                if (jsonObject.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var arrayItem in jsonObject.Value.EnumerateArray())
                        if (arrayItem.ValueKind == JsonValueKind.Object)
                        {
                            returnValue.AddRange(LocalJsonValues(fullJsonString, arrayItem.ToString()));
                        }
                        else
                        {
                            var itemString = arrayItem.ToString();
                            int itemIndex = fullJsonString.IndexOf(itemString);
                            while (itemIndex != -1)
                            {
                                returnValue.Add(new ConfigPosition(itemIndex, itemIndex + itemString.Length, ConfigType.Value, ContentsType.String, itemString));
                                itemIndex = fullJsonString.IndexOf(itemString, itemIndex + 1);
                            }
                        }

                    return returnValue;
                }

                try
                {
                    // Get value part of property
                    int jsonStartIndex = GetPropertTextAndIndex(jsonObject, fullJsonString, json, out string propertyText, out int index);

                    int propLength;
                    if (jsonObject.Value.ValueKind == JsonValueKind.String)
                    {
                        propertyText = propertyText.Trim();
                        index = json.IndexOf("\"", index) + 1;
                        propLength = propertyText.Length - 2;
                    }
                    else
                    {
                        propLength = propertyText.Length;
                    }

                    returnValue.Add(new ConfigPosition(index + jsonStartIndex,
                                        index + jsonStartIndex + propLength,
                                        ConfigType.Value,
                                        jsonObject.Value.ValueKind == JsonValueKind.String ? ContentsType.String : ContentsType.Null,
                                        jsonObject.Value.ToString()));
                }
                catch (Exception) { }

                // Index of the value part of the object
                jsonIndex = fullJsonString.IndexOf(fullObjectString, jsonIndex + 1);
            }
            return returnValue;
        }

        private static int GetPropertTextAndIndex(JsonProperty jsonObject, string fullJsonString, string json, out string propertyText, out int index)
        {
            var jsonString = jsonObject.ToString();
            int jsonStartIndex = Math.Max(fullJsonString.IndexOf(json, 0), 0);
            index = json.IndexOf(jsonString);
            var afterColonOffset = jsonString.IndexOf(':') + 1;
            propertyText = jsonString.Substring(afterColonOffset);
            index += afterColonOffset;
            return jsonStartIndex;
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
                var start = position;
                var end = contents.IndexOf('\n', position + "//".Length);
                if (end == -1)
                {
                    end = contents.Length;
                }
                returnValue.Add(new ConfigPosition(start, end + 1, ConfigType.Comment, ContentsType.String, contents.Substring(start, end - start)));
                position = position+1;
            }
            return returnValue;
        }

        private IEnumerable<ConfigPosition> LocateMultiLineComments(string contents)
        {
            var returnValue = new List<ConfigPosition>();

            int position = 0;
            while (position < contents.Length && (position = contents.IndexOf("/*", position)) != -1)
            {
                var start = position;
                var end = contents.IndexOf("*/", position + "/*".Length);
                if (end == -1)
                {
                    end = contents.Length;
                }

                returnValue.Add(new ConfigPosition(start, end + "*/".Length, ConfigType.Comment, ContentsType.String, contents.Substring(start, end - start)));

                position = position+1;
            }

            return returnValue;
        }
    }
}
