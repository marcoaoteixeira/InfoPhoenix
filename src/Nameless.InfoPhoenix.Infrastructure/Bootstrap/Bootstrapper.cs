using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Bootstrap;

namespace Nameless.InfoPhoenix.Infrastructure.Bootstrap;

public sealed class Bootstrapper : IBootstrapper {
    private readonly IStep[] _steps;
    private readonly ILogger<Bootstrapper> _logger;

    public Bootstrapper(IEnumerable<IStep> steps, ILogger<Bootstrapper> logger) {
        _steps = Prevent.Argument.Null(steps).ToArray();
        _logger = Prevent.Argument.Null(logger);
    }

    public async Task RunAsync() {
        var currentSteps = _steps.Where(step => !step.Skip)
                                 .OrderBy(item => item.Order);
        foreach (var step in currentSteps) {
            try { await step.ExecuteAsync(); }
            catch (Exception ex) {
                if (!step.ThrowOnFailure) {
                    continue;
                }

                _logger.StopBootstrapperDueStepExecutionFailure(ex);

                throw;
            }
        }
    }
}