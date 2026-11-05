using System.Diagnostics.CodeAnalysis;
using Nameless.Windows.AppConfiguration;

namespace Nameless.InfoPhoenix.Client.Tests.Fakes;

/// <summary>
/// Hand-rolled <see cref="IAppConfigurationManager"/> fake: a dictionary-backed
/// <see cref="TryGet{TValue}"/>/<see cref="Set{TValue}"/> pair plus an ordered call log
/// ("Set:&lt;key&gt;", "Save") a test can assert against, and a configurable
/// <see cref="SaveChangesAsync"/> delegate (a completed task by default, or a
/// <see cref="TaskCompletionSource"/>-backed one to control exactly when a save completes).
/// </summary>
internal sealed class FakeAppConfigurationManager : IAppConfigurationManager {
    private readonly Dictionary<string, object?> _values = [];

    public List<string> CallLog { get; } = [];

    public Func<CancellationToken, Task> OnSaveChangesAsync { get; set; } = _ => Task.CompletedTask;

    public int SaveChangesAsyncCallCount { get; private set; }

    /// <summary>
    /// When set, every <see cref="TryGet{TValue}"/> throws this instead of reading the backing
    /// dictionary - simulates an unreadable <c>app.config</c> (locked file/ACL denied), the
    /// kind of failure the real manager's lazy <c>AppConfiguration</c> getter lets escape.
    /// </summary>
    public Exception? ThrowOnTryGet { get; set; }

    /// <summary>
    /// When set, every <see cref="Set{TValue}"/> throws this instead of writing - simulates a
    /// persistently unreadable file where even recovery's reset cannot write in memory.
    /// </summary>
    public Exception? ThrowOnSet { get; set; }

    public bool TryGet<TValue>(string key, [NotNullWhen(true)] out TValue? value) {
        if (ThrowOnTryGet is not null) {
            throw ThrowOnTryGet;
        }

        if (_values.TryGetValue(key, out var stored) && stored is TValue typed) {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public void Set<TValue>(string key, TValue value) {
        if (ThrowOnSet is not null) {
            throw ThrowOnSet;
        }

        _values[key] = value;

        CallLog.Add($"Set:{key}");
    }

    /// <summary>
    /// Stores a value directly, bypassing <see cref="Set{TValue}"/> and <see cref="CallLog"/> -
    /// so a test can seed a wrong-typed or otherwise invalid stored value (plan 2.2/AC4)
    /// without that seeding itself showing up as a recorded "Set".
    /// </summary>
    public void Seed(string key, object? value) {
        _values[key] = value;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken) {
        SaveChangesAsyncCallCount++;
        CallLog.Add("Save");

        await OnSaveChangesAsync(cancellationToken);
    }

    public void Reload() {
        throw new NotSupportedException();
    }
}