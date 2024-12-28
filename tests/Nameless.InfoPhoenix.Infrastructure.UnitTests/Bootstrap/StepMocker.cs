using Nameless.InfoPhoenix.Bootstrap;
using Nameless.Test.Utils;

namespace Nameless.InfoPhoenix.Infrastructure.Bootstrap;

public class StepMocker : MockerBase<IStep> {
    public StepMocker WithName(string name) {
        InnerMock.Setup(mock => mock.Name)
                 .Returns(name);

        return this;
    }

    public StepMocker WithOrder(int order) {
        InnerMock.Setup(mock => mock.Order)
                 .Returns(order);

        return this;
    }

    public StepMocker WithThrowOnFailure(bool throwOnFailure) {
        InnerMock.Setup(mock => mock.ThrowOnFailure)
                 .Returns(throwOnFailure);

        return this;
    }

    public StepMocker WithSkip(bool skip) {
        InnerMock.Setup(mock => mock.Skip)
                 .Returns(skip);

        return this;
    }

    public StepMocker WhenExecuteAsync(Action callback) {
        InnerMock.Setup(mock => mock.ExecuteAsync())
                 .Callback(callback)
                 .Returns(Task.CompletedTask);

        return this;
    }
}