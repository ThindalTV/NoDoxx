using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
