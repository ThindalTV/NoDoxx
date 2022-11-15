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
            var ret = HideJson(fileContent).ToList();
            ret.AddRange(HideComments(fileContent, ret));
            return ret;
        }

        private IEnumerable<ConfigPosition> HideJson(string fullJson, string json = null, int jsonStartIndex = 0)
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
                returnValue.Add(new ConfigPosition(start, end, ConfigType.Value, "ERROR, cannot parse"));
                return returnValue;
            }

            returnValue.AddRange(DetermineConfigPositions(fullJson, jsonObject, json, jsonStartIndex));
            return returnValue;
        }

        private IEnumerable<ConfigPosition> DetermineConfigPositions(string fullJsonString, JsonDocument jsonDocument, string json = null, int jsonStartIndex = 0)
        {
            var returnValue = new List<ConfigPosition>();
            foreach (var jsonObject in jsonDocument.RootElement.EnumerateObject())
            {
                returnValue.AddRange(GetConfigPosition(jsonObject, fullJsonString, json));
                jsonStartIndex = returnValue.Max(r => r.EndIndex);
            }
            return returnValue;
        }

        private IEnumerable<ConfigPosition> GetConfigPosition(JsonProperty jsonObject, string fullJsonString, string json = null)
        {
            if (jsonObject.Value.ValueKind == JsonValueKind.Object)
            {
                var objString = jsonObject.Value.ToString();
                return HideJson(fullJsonString, objString, fullJsonString.IndexOf(objString));
            }

            var returnValue = new List<ConfigPosition>();
            if (jsonObject.Value.ValueKind == JsonValueKind.Array)
            {
                var objString = jsonObject.Value.ToString();
                foreach (var arrayItem in jsonObject.Value.EnumerateArray())
                    returnValue.AddRange(HideJson(fullJsonString, arrayItem.ToString(), fullJsonString.IndexOf(objString)));

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

                returnValue.Add(new ConfigPosition(index + jsonStartIndex, index + jsonStartIndex + propLength, ConfigType.Value, jsonObject.Value.ToString()));
            }
            catch (Exception)
            { }

            return returnValue;
        }

        private static int GetPropertTextAndIndex(JsonProperty jsonObject, string fullJsonString, string json, out string propertyText, out int index)
        {
            propertyText = jsonObject.ToString();
            int jsonStartIndex = Math.Max(fullJsonString.IndexOf(json, 0), 0);
            index = json.IndexOf(propertyText);
            var afterColonOffset = propertyText.IndexOf(':') + 1;
            propertyText = propertyText.Substring(afterColonOffset);
            index += afterColonOffset;
            return jsonStartIndex;
        }

        internal IEnumerable<ConfigPosition> HideComments(string contents, List<ConfigPosition> jsonValues)
        {
            var returnValue = new List<ConfigPosition>();

            // Locate line comments
            returnValue.AddRange(LocateInlineComments(contents, jsonValues));

            // Locate multiline comments
            returnValue.AddRange(LocateMultiLineComments(contents, jsonValues));

            return returnValue;
        }

        private IEnumerable<ConfigPosition> LocateInlineComments(string contents, List<ConfigPosition> jsonValues)
        {
            var returnValue = new List<ConfigPosition>();
            int position = 0;
            while ((position = contents.IndexOf("//", position)) != -1)
            {
                if (jsonValues.Any(v => v.StartIndex <= position && v.EndIndex > position))
                {
                    // We're in a value field
                    position++;
                    continue;
                }
                
                var start = position;
                var end = contents.IndexOf('\n', position + "//".Length);
                if (end == -1)
                {
                    end = contents.Length;
                }

                returnValue.Add(new ConfigPosition(start, end + 1, ConfigType.Comment, contents.Substring(start, end-start)));

                position = end;
            }
            return returnValue;
        }

        private IEnumerable<ConfigPosition> LocateMultiLineComments(string contents, List<ConfigPosition> jsonValues)
        {
            var returnValue = new List<ConfigPosition>();

            int position = 0;
            while (position < contents.Length && (position = contents.IndexOf("/*", position)) != -1)
            {
                if (jsonValues.Any(v => v.StartIndex <= position && v.EndIndex > position))
                {
                    // We're in a value field
                    position++;
                    continue;
                }
                var start = position;
                var end = contents.IndexOf("*/", position + "/*".Length);
                if (end == -1)
                {
                    end = contents.Length;
                }

                returnValue.Add(new ConfigPosition(start, end + "*/".Length, ConfigType.Comment, contents.Substring(start, end-start)));

                position = end;
            }

            return returnValue;
        }
    }
}
