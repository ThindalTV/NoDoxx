using System.Collections.Generic;

namespace NoDoxx.Interfaces
{
    internal class ConfigPosition
    {
        public int StartIndex { get; }
        public int EndIndex { get; }

        public ConfigPosition(int startIndex, int endIndex)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }

    internal interface IValueLocator
    {
        IEnumerable<ConfigPosition> FindConfigValues(string fileContent);
    }
}
