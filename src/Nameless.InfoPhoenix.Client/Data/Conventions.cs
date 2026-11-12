namespace Nameless.InfoPhoenix.Client.Data;

internal static class Conventions {
    internal static class Column {
        internal static class Size {
            internal static readonly int Small = 1024;
            internal static readonly int Medium = 1024 * 4;
            internal static readonly int Huge = 1024 * 16;
            internal static readonly int Max = int.MaxValue;
        }
    }
}
