using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Moq;
using Nameless.InfoPhoenix.Utils;

namespace Nameless.InfoPhoenix.Infrastructure.Impl {
    public class PerformanceReporterTests {
        [Test]
        public void WhenReportExecutionTime_LogStartStop() {
            // arrange
            var loggerMock = new Mock<ILogger<PerformanceReporter>>();
            var sut = new PerformanceReporter(loggerMock.Object);

            // act
            using (sut.ReportExecutionTime()) { }

            // assert
            loggerMock
                .VerifyInformation(message => message.StartsWith($"[{nameof(WhenReportExecutionTime_LogStartStop)}]: START"))
                .VerifyInformation(message => message.StartsWith($"[{nameof(WhenReportExecutionTime_LogStartStop)}]: STOP"));
        }

        [Test]
        public async Task WhenReportExecutionTime_LogPeriodOfTime() {
            // arrange
            const int DELAY_MS = 50;
            var loggerMock = new Mock<ILogger<PerformanceReporter>>();
            var sut = new PerformanceReporter(loggerMock.Object);

            // act
            using (sut.ReportExecutionTime()) {
                await Task.Delay(DELAY_MS);
            }

            // assert
            loggerMock
                .VerifyInformation(message => message.StartsWith($"[{nameof(WhenReportExecutionTime_LogPeriodOfTime)}]: START"))
                .VerifyInformation(message => {
                    var groups = Regex.Match(message, "Execution time: (.*)")
                                      .Groups;

                    if (groups.Count != 2) {
                        return false;
                    }

                    var value = groups[1].Value;
                    if (!TimeSpan.TryParse(value, out var timeSpan)) {
                        return false;
                    }

                    var minRange = DELAY_MS * 0.9;
                    var maxRange = DELAY_MS * 1.5;

                    return timeSpan.Milliseconds >= minRange &&
                           timeSpan.Milliseconds <= maxRange;
                });
        }
    }
}
