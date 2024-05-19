using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.Bootstrapping.Impl {
    public sealed class Bootstrap : IBootstrap {
        #region Private Read-Only Fields

        private readonly IStep[] _steps;
        private readonly ILogger _logger;

        #endregion

        #region Public Constructors

        public Bootstrap(IEnumerable<IStep> steps, ILogger logger) {
            _steps = Guard.Against.Null(steps, nameof(steps)).ToArray();
            _logger = Guard.Against.Null(logger, nameof(logger));
        }

        #endregion

        #region IBootstrap Members

        public async Task RunAsync() {
            var currentSteps = _steps.Where(step => !step.Skip)
                                     .OrderBy(item => item.Order);
            foreach (var step in currentSteps) {
                try { await step.ExecuteAsync(); }
                catch (Exception ex) {
                    _logger.LogError(exception: ex,
                                     message: "Error while executing step \"{StepName}\"",
                                     args: step.Name);

                    if (step.ThrowOnFailure) {
                        throw;
                    }
                }
            }
        }

        #endregion
    }
}
