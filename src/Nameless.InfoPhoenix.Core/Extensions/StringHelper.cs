namespace Nameless.InfoPhoenix.Extensions;

internal static class StringHelper {
    internal static string RemoveDoubleQuotes(string value) {
        return value.Replace("\"", string.Empty);
    }
}
