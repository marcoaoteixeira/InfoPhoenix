namespace Nameless.InfoPhoenix.UseCases.Search;

public readonly record struct ContentSearchTokens(
    string[] Include,
    string[] Exclude,
    string[] Exact
);