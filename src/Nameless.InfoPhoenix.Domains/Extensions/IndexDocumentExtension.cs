using Nameless.Search;

namespace Nameless.InfoPhoenix.Domains;

internal static class IndexDocumentExtension {
    internal static IDocument Set(this IDocument self, IndexFields field, bool value, FieldOptions options)
        => self.Set(field.ToString(), value, options);

    internal static IDocument Set(this IDocument self, IndexFields field, string value, FieldOptions options)
        => self.Set(field.ToString(), value, options);

    internal static IDocument Set(this IDocument self, IndexFields field, byte value, FieldOptions options)
        => self.Set(field.ToString(), value, options);

    internal static IDocument Set(this IDocument self, IndexFields field, short value, FieldOptions options)
        => self.Set(field.ToString(), value, options);

    internal static IDocument Set(this IDocument self, IndexFields field, int value, FieldOptions options)
        => self.Set(field.ToString(), value, options);

    internal static IDocument Set(this IDocument self, IndexFields field, long value, FieldOptions options)
        => self.Set(field.ToString(), value, options);

    internal static IDocument Set(this IDocument self, IndexFields field, float value, FieldOptions options)
        => self.Set(field.ToString(), value, options);

    internal static IDocument Set(this IDocument self, IndexFields field, double value, FieldOptions options)
        => self.Set(field.ToString(), value, options);

    internal static IDocument Set(this IDocument self, IndexFields field, DateTimeOffset value, FieldOptions options)
        => self.Set(field.ToString(), value, options);

    internal static IDocument Set(this IDocument self, IndexFields field, DateTime value, FieldOptions options)
        => self.Set(field.ToString(), value, options);
}
