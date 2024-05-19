namespace Nameless.InfoPhoenix.Infrastructure {
    public interface IPerformanceReporter {
        #region Methods

        IDisposable ReportExecutionTime(string? tag = null);

        #endregion
    }
}
