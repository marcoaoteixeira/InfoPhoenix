using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Nameless.Search.Lucene;

namespace Nameless.InfoPhoenix.Domains;
public sealed class InfoPhoenixAnalyzerSelector : IAnalyzerSelector {
    private static readonly CharArraySet EmptyCharSetArray = new(LuceneVersion.LUCENE_48, Enumerable.Empty<string>(), ignoreCase: true);

    public AnalyzerSelectorResult GetAnalyzer(string indexName)
        => new(new StandardAnalyzer(LuceneVersion.LUCENE_48, stopWords: EmptyCharSetArray));
}
