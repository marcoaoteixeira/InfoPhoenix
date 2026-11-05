using Nameless.Lucene;

namespace Nameless.InfoPhoenix.Client.Tests.Fakes;

/// <summary>
/// Hand-rolled <see cref="IIndexProvider"/> fake (plan IPX-0009 §6). Only <see cref="CloseAll"/>
/// is exercised by the backup handler; <see cref="Get(string)"/> is never called by it, so it
/// throws loudly if a test accidentally depends on it.
/// </summary>
internal sealed class FakeIndexProvider : IIndexProvider {
    public int CloseAllCallCount { get; private set; }

    public Exception? ThrowOnCloseAll { get; set; }

    public IIndex Get(string? indexName) {
        throw new NotSupportedException();
    }

    public void CloseAll() {
        CloseAllCallCount++;

        if (ThrowOnCloseAll is not null) {
            throw ThrowOnCloseAll;
        }
    }

    public void Dispose() {
        // Nothing to dispose; this fake never opens anything real.
    }
}
