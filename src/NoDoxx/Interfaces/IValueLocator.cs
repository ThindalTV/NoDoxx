using System;
using System.Collections.Generic;

namespace NoDoxx.Interfaces
{
    internal interface IValueLocator
    {
        IEnumerable<ConfigPosition> FindConfigValues(string fileContent);
    }
}
