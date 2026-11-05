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

    public bool TryGet<TValue>(string key, [NotNullWhen(true)] out TValue? value) {
        if (_values.TryGetValue(key, out var stored) && stored is TValue typed) {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public void Set<TValue>(string key, TValue value) {
        _values[key] = value;

        CallLog.Add($"Set:{key}");
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
