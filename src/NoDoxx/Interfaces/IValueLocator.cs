using System.Collections.Generic;

namespace NoDoxx.Interfaces
{
    internal class ConfigPosition
    {
        public int StartIndex { get; }
        public int EndIndex { get; }
        public ConfigType Type { get; }

        public ConfigPosition(int startIndex, int endIndex, ConfigType type)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            Type = type;
        }
    }

    internal enum ConfigType
    {
        Value,
        Comment
    }

    internal interface IValueLocator
    {
        IEnumerable<ConfigPosition> FindConfigValues(string fileContent);
    }
}
