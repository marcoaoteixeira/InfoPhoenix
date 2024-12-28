namespace Nameless.InfoPhoenix.Domains.UseCases.Search;

public readonly record struct SearchTerm {
    public string Value { get; init; }
    public bool Suggested { get; init; }

    public SearchTerm(string value, bool suggested = false) {
        Value = value;
        Suggested = suggested;
    }
}
