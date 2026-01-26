using Nameless.Bootstrap;
using Nameless.WPF.Bootstrap;

namespace Nameless.InfoPhoenix.Bootstrap.Steps;

public class MeaninglessStep : StepBase {
    public override async Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken) {
        var progress = context.GetStepProgress();

        await progress.ReportAsync(this, "First meaningless message...", delay: 500);
        await progress.ReportAsync(this, "Second meaningless message...", delay: 500);
        await progress.ReportAsync(this, "Third meaningless message...", delay: 500);
        await progress.ReportAsync(this, "Fourth meaningless message...", delay: 500);
        await progress.ReportAsync(this, "Fifth meaningless message...", delay: 500);
        await progress.ReportAsync(this, "Sixth meaningless message...", delay: 500);
        await progress.ReportAsync(this, "Seventh meaningless message...", delay: 500);
        await progress.ReportAsync(this, "Eighth meaningless message...", delay: 500);
        await progress.ReportAsync(this, "Ninth meaningless message...", delay: 500);
        await progress.ReportAsync(this, "Tenth meaningless message...", delay: 500);
    }
}
