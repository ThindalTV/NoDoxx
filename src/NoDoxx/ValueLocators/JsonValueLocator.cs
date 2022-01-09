using NoDoxx.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace NoDoxx.ValueLocators
{
    enum ValueType
    {
        Null,
        Boolean,
        String,
        Object,
        Array,
        Number
    }

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

            //var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));

            //try
            //{
            //    while (reader.Read())
            //    {
            //        ret.Add(
            //            new ConfigPosition(
            //                (int)reader.TokenStartIndex,
            //                0
            //                ));
            //    }
            //} catch(Exception ex)
            //{
            //    ;
            //}


            System.Text.Json.JsonDocument jsonObject = null;
            // Check for valid json
            try
            {
                jsonObject = JsonDocument.Parse(json, new JsonDocumentOptions() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            }
            catch (Exception ex)
            {
                var start = fullJson.IndexOf(json);
                var end = start + json.Length;
                ret.Add(new ConfigPosition(start, end));
                return ret;
            }

            var obj = jsonObject.RootElement.EnumerateObject();
            foreach (var o in obj)
            {
                if (o.Value.ValueKind == JsonValueKind.Object)
                {
                    var objString = o.Value.ToString();
                    ret.AddRange(HideJson(fullJson, objString, fullJson.IndexOf(objString)));
                    jsonStartIndex = ret.Max(r => r.EndIndex);
                    continue;
                }

                if (o.Value.ValueKind == JsonValueKind.Array)
                {
                    var objString = o.Value.ToString();
                    foreach (var arrayItem in o.Value.EnumerateArray())
                    {
                        ret.AddRange(HideJson(fullJson, arrayItem.ToString(), fullJson.IndexOf(objString)));
                    }
                    jsonStartIndex = ret.Max(r => r.EndIndex);
                    continue;
                }

                try
                {
                    // Get value part of property
                    var propertyText = o.ToString();
                    jsonStartIndex = Math.Max(fullJson.IndexOf(json, Math.Max(jsonStartIndex, 0)), 0);
                    var index = json.IndexOf(propertyText);
                    var afterColonOffset = propertyText.IndexOf(':') + 1;
                    propertyText = propertyText.Substring(afterColonOffset);
                    index = index + afterColonOffset;

                    int propLength = -1;
                    if (o.Value.ValueKind == JsonValueKind.True) propLength = propertyText.Length;
                    else if (o.Value.ValueKind == JsonValueKind.False) propLength = propertyText.Length;
                    else if (o.Value.ValueKind == JsonValueKind.Null) propLength = propertyText.Length;
                    else if (o.Value.ValueKind == JsonValueKind.String)
                    {
                        propertyText = propertyText.Trim();
                        index = json.IndexOf("\"", index)+1;
                        propLength = propertyText.Length-2;
                    } else
                    {
                        propLength = propertyText.Length;
                    }

                    ret.Add(new ConfigPosition(index+jsonStartIndex, index + jsonStartIndex + propLength));
                } catch(Exception ex)
                {
                    ;
                }
            }
            return ret;
        }
    }
}
