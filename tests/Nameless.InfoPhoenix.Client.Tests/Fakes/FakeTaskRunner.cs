using Nameless.Windows.TaskRunner;

namespace Nameless.InfoPhoenix.Client.Tests.Fakes;

/// <summary>
/// Hand-rolled <see cref="ITaskRunner"/> fake (plan IPX-0008 §9/IPX-0009 B14).
/// <see cref="CreateBuilder"/> always throws: the real <see cref="TaskRunnerBuilder"/> needs a
/// live WPF dispatcher to run, so <c>AppConfigurationPageViewModel.PerformApplicationBackupAsync</c>
/// cannot be unit-tested past construction - only the backup handler is (IPX-0008 §3.3's "the
/// ViewModel's run path cannot be unit-tested" rule).
/// </summary>
internal sealed class FakeTaskRunner : ITaskRunner {
    /// <summary>
    /// Counts every <see cref="CreateBuilder"/> call, even though it always throws - lets a test
    /// prove a ViewModel actually reached the TaskRunner seam (plan IPX-0011 B26) without being
    /// able to run a real window.
    /// </summary>
    public int CreateBuilderCount { get; private set; }

    public TaskRunnerBuilder CreateBuilder() {
        CreateBuilderCount++;

        throw new NotSupportedException();
    }
}