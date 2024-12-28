namespace Nameless.InfoPhoenix.Telemetry;

public interface IPerformanceReporter {
    IDisposable ExecutionTime(string tag);
}