using System.Text.RegularExpressions;

namespace Nameless.InfoPhoenix.Internals;

internal static partial class RegexCache {
    [GeneratedRegex("(\".*?\")", RegexOptions.IgnoreCase)]
    public static partial Regex ContentSearchTokenPattern();
}
