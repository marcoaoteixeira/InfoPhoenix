using Nameless.InfoPhoenix.Application.ErrorHandling;
using Nameless.InfoPhoenix.Bootstrap;

namespace Nameless.InfoPhoenix.Application.Bootstrap;
public sealed class StartExceptionWardenShiftStep : IStep {
    private readonly IExceptionWarden _exceptionWarden;

    public string Name => "Starting Exception Warden Shift";
    
    public int Order => 0;
    
    public bool ThrowOnFailure => false;
    
    public bool Skip => false;

    public StartExceptionWardenShiftStep(IExceptionWarden exceptionWarden) {
        _exceptionWarden = Prevent.Argument.Null(exceptionWarden);
    }

    public Task ExecuteAsync() {
        _exceptionWarden.StartShift();

        return Task.CompletedTask;
    }
}
