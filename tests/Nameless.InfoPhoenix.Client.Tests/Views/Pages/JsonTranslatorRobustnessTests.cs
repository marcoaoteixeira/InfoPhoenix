using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using Nameless.Windows.Localization;
using Nameless.Windows.Localization.Json;

namespace Nameless.InfoPhoenix.Client.Tests.Views.Pages;

/// <summary>
/// B15/B16 (plan IPX-0015 section 8, should-have): drives the real <see cref="JsonTranslator"/>
/// parser with hand-crafted file content through a minimal <see cref="ITranslationFileProvider"/>
/// fake, instead of the disk-backed one - no real file needs to exist for either case.
/// </summary>
public sealed class JsonTranslatorRobustnessTests {
    /// <summary>
    /// B15: a duplicate property within one section must not throw out of the indexer - the
    /// whole file is rejected and logged, and every lookup against it shows the missing-key
    /// placeholder, exactly like a missing file would.
    /// </summary>
    [Fact]
    public void Indexer_WithADuplicatePropertyWithinASection_ReturnsThePlaceholder_WithoutThrowing() {
        const string malformedJson = """
            {
              "Section": {
                "Key": "First",
                "Key": "Second"
              }
            }
            """;
        var provider = new FixedContentTranslationFileProvider(malformedJson);
        using var translator = new JsonTranslator(provider, NullLogger<JsonTranslator>.Instance);

        var exception = Record.Exception(() => translator["Section_Key"]);

        Assert.Null(exception);
        Assert.Equal("[Section_Key]", translator["Section_Key"]);
    }

    /// <summary>
    /// B16: a value containing a stray, unescaped <c>{</c> (not a bare <c>{digits}</c>
    /// placeholder) reaches <c>string.Format</c> at lookup time and throws
    /// <see cref="FormatException"/> - documenting exactly why new keys (for example the D4
    /// validator messages) must never contain a literal brace.
    /// </summary>
    [Fact]
    public void Indexer_WithAStrayUnescapedBrace_ThrowsFormatException_AtLookupTime() {
        const string jsonWithStrayBrace = """
            {
              "Section": {
                "Key": "Unexpected { in the middle of the text."
              }
            }
            """;
        var provider = new FixedContentTranslationFileProvider(jsonWithStrayBrace);
        using var translator = new JsonTranslator(provider, NullLogger<JsonTranslator>.Instance);

        Assert.Throws<FormatException>(() => translator["Section_Key"]);
    }

    /// <summary>Always returns the same content for the "default" key; every other key is "not found".</summary>
    private sealed class FixedContentTranslationFileProvider : ITranslationFileProvider {
        private readonly string _content;

        public FixedContentTranslationFileProvider(string content) {
            _content = content;
        }

        public bool TryRead(string key, [NotNullWhen(true)] out string? content) {
            if (!string.Equals(key, "default", StringComparison.OrdinalIgnoreCase)) {
                content = null;

                return false;
            }

            content = _content;

            return true;
        }
    }
}
