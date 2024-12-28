using System.Linq.Expressions;
using Moq;

namespace Nameless.Test.Utils;

public abstract class MockerBase<T> where T : class {
    protected Mock<T> InnerMock { get; } = new();

    public virtual T Build()
        => InnerMock.Object;

    public void Verify(Expression<Action<T>> expression)
        => Verify(expression, Times.Once());

    public void Verify(Expression<Action<T>> expression, Times times)
        => InnerMock.Verify(expression, times);

    public void Verify(Expression<Func<T, bool>> expression)
        => Verify(expression, Times.Once());

    public void Verify(Expression<Func<T, bool>> expression, Times times)
        => InnerMock.Verify(expression, times);

    public void ResetVerificationCount()
        => InnerMock.Invocations.Clear();
}