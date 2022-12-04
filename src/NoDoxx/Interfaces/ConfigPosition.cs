using System;

namespace NoDoxx.Interfaces
{
    internal sealed class ConfigPosition : IEquatable<ConfigPosition>
    {
        public int StartIndex { get; }
        public int EndIndex { get; }
        public ConfigType Type { get; }
        public string Contents { get; }
        public ContentsType ContentsType { get; }

        public ConfigPosition(int startIndex, int endIndex, ConfigType type, ContentsType contentsType, string contents)
        {
            Contents = contents;
            StartIndex = startIndex;
            EndIndex = endIndex;
            Type = type;
            ContentsType = contentsType;
        }

        /// <summary>
        /// Used for debug purposes
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Type} {StartIndex} - {EndIndex}: {Contents}";
        }

        public bool Equals(ConfigPosition other)
        {
            return StartIndex == other.StartIndex
                && EndIndex == other.EndIndex
                && Type == other.Type
                && ContentsType == other.ContentsType
                && Contents == other.Contents;
        }

        /// <summary>
        /// Checks if this position is entirely inside another one
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsInside(ConfigPosition other)
        {
            // the other is inside this one but the contents is NOT the same
            return StartIndex >= other.StartIndex
                && EndIndex <= other.EndIndex
                && Contents != other.Contents;
        }
    }
}
