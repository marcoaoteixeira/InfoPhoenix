using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Telemetry;

namespace Nameless.InfoPhoenix.Infrastructure.Telemetry;

public sealed class PerformanceReporter : IPerformanceReporter {
    private readonly IClock _clock;
    private readonly ILogger<PerformanceReporter> _logger;

    public PerformanceReporter(IClock clock, ILogger<PerformanceReporter> logger) {
        _clock = Prevent.Argument.Null(clock);
        _logger = Prevent.Argument.Null(logger);
    }

    public IDisposable ExecutionTime(string tag)
        => new Stopwatcher(Prevent.Argument.NullOrWhiteSpace(tag), _clock, _logger);

    private class Stopwatcher : IDisposable {
        private readonly string _tag;
        private readonly IClock _clock;
        private readonly ILogger _logger;

        private Stopwatch? _stopwatch;
        private bool _disposed;

        public Stopwatcher(string tag, IClock clock, ILogger logger) {
            _tag = tag;
            _clock = clock;
            _logger = logger;
            _stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("[{Tag}]: Starting at {TimeStamp:G}...", tag, _clock.GetUtcNow());
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing) {
            if (_disposed) { return; }
            if (disposing && _stopwatch is not null) {
                _logger.LogInformation("[{Tag}]: Finished at {TimeStamp:G}. Total execution time: {TotalExecutionTime}",
                                       _tag,
                                       _clock.GetUtcNow(),
                                       TimeSpan.FromMilliseconds(_stopwatch.ElapsedMilliseconds));
            }

            _stopwatch = null;
            _disposed = true;
        }
    }
}