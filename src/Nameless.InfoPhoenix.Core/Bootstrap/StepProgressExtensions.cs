using Nameless.Bootstrap;
using Nameless.WPF.Bootstrap;

namespace Nameless.InfoPhoenix.Bootstrap;

internal static class StepProgressExtensions {
    extension(IProgress<StepReport> self) {
        internal void Report(IStep step, string message) {
            self.Report(new StepReport(step.Name, message));
        }

        internal async Task ReportAsync(IStep step, string message, int delay = 125) {
            self.Report(new StepReport(step.Name, message));

            await Task.Delay(delay);
        }
    }
}
