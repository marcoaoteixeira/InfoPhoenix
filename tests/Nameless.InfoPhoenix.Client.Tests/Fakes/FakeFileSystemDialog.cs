using Nameless.Windows.Dialogs.FileSystem;

namespace Nameless.InfoPhoenix.Client.Tests.Fakes;

/// <summary>
/// Hand-rolled <see cref="IFileSystemDialog"/> fake (plan IPX-0010 §6). Only
/// <see cref="OpenFile"/> is exercised by the restore flow; <see cref="OpenDirectory"/> and
/// <see cref="OpenSave"/> throw, so a test that accidentally depends on them fails loudly.
/// </summary>
internal sealed class FakeFileSystemDialog : IFileSystemDialog {
    public int OpenFileCount { get; private set; }

    public FileSelectionOptions? LastFileSelectionOptions { get; private set; }

    /// <summary>
    /// Returned by the next <see cref="OpenFile"/> call. Defaults to empty, which models the
    /// user cancelling the dialog (plan D1).
    /// </summary>
    public IEnumerable<string> FilesToReturn { get; set; } = [];

    /// <summary>
    /// When set, <see cref="OpenFile"/> throws this instead of returning
    /// <see cref="FilesToReturn"/> - simulates a bug-path failure (plan B8/robustness row).
    /// </summary>
    public Exception? ThrowOnOpenFile { get; set; }

    public IEnumerable<string> OpenFile(Action<FileSelectionOptions> configure) {
        var options = new FileSelectionOptions();
        configure(options);

        LastFileSelectionOptions = options;
        OpenFileCount++;

        if (ThrowOnOpenFile is not null) {
            throw ThrowOnOpenFile;
        }

        return FilesToReturn;
    }

    public IEnumerable<string> OpenDirectory(Action<DirectorySelectionOptions> configure) {
        throw new NotSupportedException();
    }

    public string OpenSave(Action<SaveSelectionOptions> configure) {
        throw new NotSupportedException();
    }
}
