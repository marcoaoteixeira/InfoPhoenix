using Nameless.Contracts;
using Nameless.IO;
using Nameless.IO.Physical;
using Nameless.Mediator.Requests;
using Nameless.Windows.Events;
using Nameless.Windows.Helpers;
using Nameless.Windows.Localization;
using Nameless.Windows.Notification;
using Nameless.Windows.Notification.SnackBar;

namespace Nameless.InfoPhoenix.Client.Features.OpenCurrentLogFile;

/// <summary>
/// Opens the current (most recently written) application log file in the default viewer.
/// </summary>
/// <remarks>
/// Deviations from the sample (plan D4/D5): the lookup directory is rooted at
/// <see cref="AppContext.BaseDirectory"/> through the same <see cref="Constants.Logging.Directory"/>
/// name the Serilog sink uses, so both agree on one absolute location regardless of the
/// process's working directory. The newest file is picked with <c>OrderByDescending</c>
/// (not the sample's <c>OrderBy</c>, which opens the oldest retained file), and a missing
/// logs directory is treated as "no log file" rather than throwing.
/// </remarks>
public class OpenCurrentLogFileRequestHandler : IRequestHandler<OpenCurrentLogFileRequest> {
    private readonly IEventPublisher _eventPublisher;
    private readonly IFileManager _fileManager;
    private readonly ITranslator<OpenCurrentLogFileRequestHandler> _translator;

    public OpenCurrentLogFileRequestHandler(
        IEventPublisher eventPublisher,
        PhysicalFileManagerFactory fileManagerFactory,
        ITranslator<OpenCurrentLogFileRequestHandler> translator) {
        _eventPublisher = Throws.When.Null(eventPublisher);
        _fileManager = Throws.When.Null(fileManagerFactory).GetOrCreate(AppContext.BaseDirectory);
        _translator = Throws.When.Null(translator);
    }

    public async Task HandleAsync(OpenCurrentLogFileRequest request, CancellationToken cancellationToken) {
        var logDirectory = _fileManager.GetDirectory(Constants.Logging.Directory);
        var currentFile = FindCurrentLogFile(logDirectory);

        if (currentFile is null) {
            await _eventPublisher.PublishWarningAsync<SnackBarNotificationEvent>(
                title: _translator["Missing_Log_File_Title"],
                message: _translator["Missing_Log_File_Message"]
            );

            return;
        }

        ProcessHelper.OpenFile(Quote(currentFile.Path));
    }

    /// <summary>
    /// Picks the most recently written file in <paramref name="logDirectory"/>, or
    /// <see langword="null"/> when the directory does not exist or is empty. Internal (rather
    /// than private) so tests can assert the selection - <c>OrderByDescending</c>, not the
    /// sample's <c>OrderBy</c> (plan D5) - without triggering the real <c>explorer.exe</c>
    /// launch that the rest of <see cref="HandleAsync"/> performs.
    /// </summary>
    internal static IFile? FindCurrentLogFile(IDirectory logDirectory) {
        return logDirectory.Exists
            ? logDirectory.GetFiles("*").OrderByDescending(file => file.LastWriteTime).FirstOrDefault()
            : null;
    }

    private static string Quote(string path) {
        return $"\"{path}\"";
    }
}
