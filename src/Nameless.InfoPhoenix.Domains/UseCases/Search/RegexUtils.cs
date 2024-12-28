using System.Text.RegularExpressions;

namespace Nameless.InfoPhoenix.Domains.UseCases.Search;

public static partial class RegexUtils {
    [GeneratedRegex("(\".*?\")", RegexOptions.IgnoreCase)]
    public static partial Regex SearchToken();
}