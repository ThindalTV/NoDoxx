using System.Collections.Generic;

namespace NoDoxx.Interfaces
{
    internal class ConfigPosition
    {
        public int StartIndex { get; }
        public int EndIndex { get; }
        public ConfigType Type { get; }
        public string Contents { get; }

        public ConfigPosition(int startIndex, int endIndex, ConfigType type, string contents)
        {
            Contents = contents;
            StartIndex = startIndex;
            EndIndex = endIndex;
            Type = type;
        }

        public override string ToString()
        {
            return $"{Type} {StartIndex} - {EndIndex}: {Contents}";
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
