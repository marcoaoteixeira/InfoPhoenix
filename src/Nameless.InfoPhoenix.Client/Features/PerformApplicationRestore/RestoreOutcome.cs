namespace Nameless.InfoPhoenix.Client.Features.PerformApplicationRestore;

/// <summary>
/// Outcome of a <see cref="PerformApplicationRestoreRequest"/> (plan IPX-0011 §3.1, a refinement
/// of IPX-0008 §3.1's enum).
/// </summary>
public enum RestoreOutcome {
    /// <summary>The swap committed. Live data was replaced; the user must relaunch to use it.</summary>
    Completed,

    /// <summary>Rejected by a validation check. No live file was touched.</summary>
    Blocked,

    /// <summary>
    /// An IO or unexpected failure in staging or commit. The live state is as it was before -
    /// untouched if the failure was pre-commit, reversed if it happened during the commit.
    /// </summary>
    RolledBack,

    /// <summary>
    /// An in-commit failure where at least one reverse rename also failed. The leftover
    /// <c>*.restore-old-*</c> paths are named in the reported Failure entry.
    /// </summary>
    RollbackIncomplete
}
