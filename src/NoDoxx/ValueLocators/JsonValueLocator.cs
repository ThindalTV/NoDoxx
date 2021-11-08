using NoDoxx.Interfaces;
using System;
using System.Collections.Generic;

namespace NoDoxx.ValueLocators
{
    internal class JsonValueLocator : IValueLocator
    {
        public IEnumerable<ConfigPosition> FindConfigValues(string fileContent)
        {

            return HideJson(fileContent);
        }

        private IEnumerable<ConfigPosition> HideJson(string fullJson, string json = null, int jsonStartIndex = 0)
        {
            var ret = new List<ConfigPosition>();
            if (json == null) json = fullJson;

            System.Text.Json.JsonDocument jsonObject;
            try
            {
                jsonObject = System.Text.Json.JsonDocument.Parse(json);
            }
            catch (Exception)
            {
                var start = fullJson.IndexOf(json);
                var end = start + json.Length;
                ret.Add(new ConfigPosition(start, end));
                return ret;
            }

            var obj = jsonObject.RootElement.EnumerateObject();
            foreach (var o in obj)
            {
                if (o.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    var objString = o.Value.ToString();
                    ret.AddRange(HideJson(fullJson, objString, fullJson.IndexOf(objString)));
                    continue;
                }

                if (o.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var objString = o.Value.ToString();
                    foreach (var arrayItem in o.Value.EnumerateArray())
                    {
                        ret.AddRange(HideJson(fullJson, arrayItem.ToString(), fullJson.IndexOf(objString)));
                    }
                    continue;
                }

                // Get value part of property
                int index = jsonStartIndex;
                var propertyText = o.ToString();
                while ((index = fullJson.IndexOf(propertyText, index + 1)) > 0)
                {
                    var caseInsensitive = o.Value.ValueKind == System.Text.Json.JsonValueKind.True || o.Value.ValueKind == System.Text.Json.JsonValueKind.False;
                    var valuePartIndex = fullJson.IndexOf(o.Value.ToString(), index + 2, caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                    var endIndex = index + propertyText.Length;

                    // For strings it doesn't catch the trailing " because it only works with the value.
                    int stringPad = o.Value.ValueKind == System.Text.Json.JsonValueKind.String ? -1 : 0;

                    ret.Add(new ConfigPosition(valuePartIndex, endIndex + stringPad));
                }
            }
            return ret;
        }
    }
}
