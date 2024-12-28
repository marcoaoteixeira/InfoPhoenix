namespace Nameless.Test.Utils.Mockers;

public class ClockMocker : MockerBase<IClock> {
    public ClockMocker WithUtcNow(DateTime dateTime) {
        InnerMock.Setup(mock => mock.GetUtcNow())
                 .Returns(dateTime);

        return this;
    }
}