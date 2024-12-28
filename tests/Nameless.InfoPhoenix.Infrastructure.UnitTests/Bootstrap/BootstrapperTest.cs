using Nameless.InfoPhoenix.Bootstrap;
using Nameless.Test.Utils.Mockers;

namespace Nameless.InfoPhoenix.Infrastructure.Bootstrap;

public class BootstrapperTest {
    [Test]
    public async Task WhenRunAsync_WithMultipleSteps_ThenStepsShouldBeOrdered() {
        // arrange
        var executionOrder = new List<int>();
        var steps = new[] {
            new DelegateStep(step => executionOrder.Add(step.Order), order: 1),
            new DelegateStep(step => executionOrder.Add(step.Order), order: 4),
            new DelegateStep(step => executionOrder.Add(step.Order), order: 2),
            new DelegateStep(step => executionOrder.Add(step.Order), order: 5),
            new DelegateStep(step => executionOrder.Add(step.Order), order: 3),
        };

        var sut = new Bootstrapper(steps, new LoggerMocker<Bootstrapper>().Build());

        // act
        await sut.RunAsync();

        // assert
        Assert.That(executionOrder, Is.Ordered.Ascending);
    }

    public class DelegateStep : IStep {
        private readonly Action<IStep> _action;

        public string Name => "Delegate Step";
        public int Order { get; }
        public bool ThrowOnFailure { get; }
        public bool Skip { get; }

        public DelegateStep(Action<IStep> action, int order = 0, bool thrownOnFailure = false, bool skip = false) {
            _action = action;

            Order = order;
            ThrowOnFailure = thrownOnFailure;
            Skip = skip;
        }

        public Task ExecuteAsync() {
            if (Skip) {
                return Task.CompletedTask;
            }

            try { _action(this); }
            catch {
                if (ThrowOnFailure) {
                    throw;
                }
            }

            return Task.CompletedTask;
        }
    }
}