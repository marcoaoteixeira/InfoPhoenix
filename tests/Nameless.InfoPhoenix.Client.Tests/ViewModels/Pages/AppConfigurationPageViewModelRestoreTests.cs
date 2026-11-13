using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Nameless.InfoPhoenix.Client.Tests.Fakes;
using Nameless.InfoPhoenix.Client.Tests.Views.Windows;
using Nameless.InfoPhoenix.Client.ViewModels.Pages;
using Nameless.IO.Physical;
using Nameless.Windows.AppConfiguration;
using Nameless.Windows.Dialogs.Message;
using Nameless.Windows.Localization;
using Nameless.Windows.Notification;
using Nameless.Windows.Notification.SnackBar;

namespace Nameless.InfoPhoenix.Client.Tests.ViewModels.Pages;

/// <summary>
/// Exercises <c>AppConfigurationPageViewModel.PerformApplicationRestoreCommand</c> (plan
/// IPX-0010 §6): pick a backup, confirm the overwrite, stop at the temporary "not yet
/// available" notification (D5). Uses a real <c>PhysicalFileManager</c> over a per-test temp
/// root (same reasoning as <see cref="AppConfigurationPageViewModelIntegrationTests"/>) because
/// the command's own D2 step (<c>backupDirectory.Create()</c>) is real filesystem behavior.
/// </summary>
[Collection(WpfUiThemeTestCollection.Name)]
public sealed class AppConfigurationPageViewModelRestoreTests : IDisposable {
    private readonly string _tempRoot = Directory.CreateTempSubdirectory("ipx-0010-").FullName;

    private string BackupsDirectory => Path.Combine(_tempRoot, "backups");

    public void Dispose() {
        Directory.Delete(_tempRoot, recursive: true);
    }

    /// <summary>B1 (AC1): a fresh install with no backups\ folder yet.</summary>
    [Fact]
    public void PerformApplicationRestore_OnAFreshInstall_CreatesBackupsFolder_AndOpensDialogThere() {
        StaWindowTestHarness.Run(() => {
            var fileSystemDialog = new FakeFileSystemDialog();
            var vm = CreateViewModel(fileSystemDialog: fileSystemDialog);

            vm.PerformApplicationRestoreCommand.ExecuteAsync(null).GetAwaiter().GetResult();

            Assert.Equal(1, fileSystemDialog.OpenFileCount);
            Assert.True(Directory.Exists(BackupsDirectory));

            var options = fileSystemDialog.LastFileSelectionOptions!;
            Assert.Equal(BackupsDirectory, options.Root);
            Assert.EndsWith("|*.zip", options.Filter);
            Assert.Contains("(*.zip)", options.Filter);
            Assert.False(options.Multiselect);
            Assert.Equal("AppConfigurationPageViewModel_Restore_SelectFile_Title", options.Title);
        });
    }

    /// <summary>B2 (AC1/D2): a FILE (not a folder) named "backups" blocks Create().</summary>
    [Fact]
    public void PerformApplicationRestore_WhenBackupsPathIsBlockedByAFile_StillOpensDialog_WithoutThrowingOrFailing() {
        StaWindowTestHarness.Run(() => {
            File.WriteAllText(BackupsDirectory, "not a directory");

            var fileSystemDialog = new FakeFileSystemDialog();
            var eventPublisher = new FakeEventPublisher();
            var vm = CreateViewModel(fileSystemDialog: fileSystemDialog, eventPublisher: eventPublisher);

            var exception = Record.Exception(
                () => vm.PerformApplicationRestoreCommand.ExecuteAsync(null).GetAwaiter().GetResult());

            Assert.Null(exception);
            Assert.Equal(1, fileSystemDialog.OpenFileCount);
            Assert.Equal(BackupsDirectory, fileSystemDialog.LastFileSelectionOptions!.Root);
            Assert.Empty(eventPublisher.PublishedEvents);
        });
    }

    /// <summary>B3 (AC3): the file dialog returns [] (cancelled) - nothing proceeds.</summary>
    [Fact]
    public void PerformApplicationRestore_WhenFileDialogIsCancelled_NothingProceeds() {
        StaWindowTestHarness.Run(() => {
            var messageDialog = new FakeMessageDialog();
            var eventPublisher = new FakeEventPublisher();
            var mediator = new FakeMediator();
            var manager = new FakeAppConfigurationManager();
            var vm = CreateViewModel(messageDialog: messageDialog, eventPublisher: eventPublisher, mediator: mediator, manager: manager);

            vm.PerformApplicationRestoreCommand.ExecuteAsync(null).GetAwaiter().GetResult();

            Assert.Empty(messageDialog.Calls);
            Assert.Empty(eventPublisher.PublishedEvents);
            Assert.Empty(mediator.ExecutedRequests);
            Assert.Empty(manager.CallLog);
        });
    }

    /// <summary>B4 (AC2): a real zip picked under backups\ triggers exactly one warning dialog.</summary>
    [Fact]
    public void PerformApplicationRestore_WhenAZipIsPicked_ShowsExactlyOneWarningDialog_NamingTheFile() {
        StaWindowTestHarness.Run(() => {
            var zipPath = CreateZipUnderBackups("InfoPhoenix-backup-20261105T143012Z.zip");
            var fileSystemDialog = new FakeFileSystemDialog { FilesToReturn = [zipPath] };
            var messageDialog = new FakeMessageDialog();
            var translator = new FakeTranslator();
            var vm = CreateViewModel(fileSystemDialog: fileSystemDialog, messageDialog: messageDialog, translator: translator);

            vm.PerformApplicationRestoreCommand.ExecuteAsync(null).GetAwaiter().GetResult();

            var call = Assert.Single(messageDialog.Calls);
            Assert.Equal(MessageDialogIcon.Warning, call.Options.Icon);
            Assert.Equal(MessageDialogButtons.YesNo, call.Options.Buttons);
            Assert.Equal("AppConfigurationPageViewModel_Restore_ConfirmOverwrite_Title", call.Options.Title);

            var request = Assert.Single(translator.Requests, r => r.Key == "AppConfigurationPageViewModel_Restore_ConfirmOverwrite_Message");
            Assert.Equal(["InfoPhoenix-backup-20261105T143012Z.zip"], request.Args);
        });
    }

    /// <summary>B5 (AC3): every non-Yes warning result stops - no events, no mediator, no config access.</summary>
    [Theory]
    [InlineData(MessageDialogResult.No)]
    [InlineData(MessageDialogResult.Cancel)]
    [InlineData(MessageDialogResult.Ok)]
    public void PerformApplicationRestore_WhenWarningIsDeclined_NothingProceeds(MessageDialogResult declined) {
        StaWindowTestHarness.Run(() => {
            var zipPath = CreateZipUnderBackups("InfoPhoenix-backup-20261105T143012Z.zip");
            var fileSystemDialog = new FakeFileSystemDialog { FilesToReturn = [zipPath] };
            var messageDialog = new FakeMessageDialog { ResultToReturn = declined };
            var eventPublisher = new FakeEventPublisher();
            var mediator = new FakeMediator();
            var manager = new FakeAppConfigurationManager();
            var vm = CreateViewModel(
                fileSystemDialog: fileSystemDialog, messageDialog: messageDialog,
                eventPublisher: eventPublisher, mediator: mediator, manager: manager);

            vm.PerformApplicationRestoreCommand.ExecuteAsync(null).GetAwaiter().GetResult();

            Assert.Empty(eventPublisher.PublishedEvents);
            Assert.Empty(mediator.ExecutedRequests);
            Assert.Empty(manager.CallLog);
        });
    }

    /// <summary>
    /// B6 (AC2 stop point/D5, updated by IPX-0011 B26): a confirmed (Yes) restore now reaches
    /// <c>ITaskRunner.CreateBuilder</c> exactly once. The fake throws <see cref="NotSupportedException"/>
    /// (a real <see cref="Nameless.Windows.TaskRunner.TaskRunnerBuilder"/> needs a live WPF
    /// dispatcher, so the run itself is not unit-testable - plan §3.3/IPX-0008), which the
    /// command's own bug-path catch turns into exactly one Failure snackbar. The mediator is
    /// never reached, and the picked zip is left untouched.
    /// </summary>
    [Fact]
    public void PerformApplicationRestore_WhenWarningIsConfirmed_ReachesTheTaskRunnerExactlyOnce_AndPublishesOneFailureSnackbar() {
        StaWindowTestHarness.Run(() => {
            var zipPath = CreateZipUnderBackups("InfoPhoenix-backup-20261105T143012Z.zip");
            var bytesBefore = File.ReadAllBytes(zipPath);
            var lastWriteBefore = File.GetLastWriteTimeUtc(zipPath);

            var fileSystemDialog = new FakeFileSystemDialog { FilesToReturn = [zipPath] };
            var messageDialog = new FakeMessageDialog { ResultToReturn = MessageDialogResult.Yes };
            var eventPublisher = new FakeEventPublisher();
            var mediator = new FakeMediator();
            var manager = new FakeAppConfigurationManager();
            var taskRunner = new FakeTaskRunner();
            var vm = CreateViewModel(
                fileSystemDialog: fileSystemDialog, messageDialog: messageDialog,
                eventPublisher: eventPublisher, mediator: mediator, manager: manager, taskRunner: taskRunner);

            vm.PerformApplicationRestoreCommand.ExecuteAsync(null).GetAwaiter().GetResult();

            Assert.Equal(1, taskRunner.CreateBuilderCount);
            var notification = Assert.Single(eventPublisher.PublishedEvents.OfType<SnackBarNotificationEvent>());
            Assert.Equal(NotificationType.Failure, notification.Type);
            Assert.Equal("AppConfigurationPageViewModel_Restore_Failure_Message", notification.Message);
            Assert.Empty(mediator.ExecutedRequests);
            Assert.Empty(manager.CallLog);
            Assert.Equal(bytesBefore, File.ReadAllBytes(zipPath));
            Assert.Equal(lastWriteBefore, File.GetLastWriteTimeUtc(zipPath));
        });
    }

    /// <summary>B7 (D3): a file picked from OUTSIDE backups\ is accepted the same way.</summary>
    [Fact]
    public void PerformApplicationRestore_WhenZipIsPickedOutsideBackupsFolder_BehavesLikeAnyOtherPick() {
        StaWindowTestHarness.Run(() => {
            var outsideDirectory = Directory.CreateTempSubdirectory("ipx-0010-outside-").FullName;
            try {
                var zipPath = Path.Combine(outsideDirectory, "copied-from-elsewhere.zip");
                File.WriteAllBytes(zipPath, [1, 2, 3]);

                var fileSystemDialog = new FakeFileSystemDialog { FilesToReturn = [zipPath] };
                var messageDialog = new FakeMessageDialog { ResultToReturn = MessageDialogResult.Yes };
                var eventPublisher = new FakeEventPublisher();
                var taskRunner = new FakeTaskRunner();
                var vm = CreateViewModel(
                    fileSystemDialog: fileSystemDialog, messageDialog: messageDialog,
                    eventPublisher: eventPublisher, taskRunner: taskRunner);

                vm.PerformApplicationRestoreCommand.ExecuteAsync(null).GetAwaiter().GetResult();

                Assert.Single(messageDialog.Calls);
                Assert.Equal(1, taskRunner.CreateBuilderCount);
                Assert.Single(eventPublisher.PublishedEvents.OfType<SnackBarNotificationEvent>());
            }
            finally {
                Directory.Delete(outsideDirectory, recursive: true);
            }
        });
    }

    /// <summary>Robustness row (plan §6): a dialog failure never escapes the command.</summary>
    [Fact]
    public void PerformApplicationRestore_WhenFileDialogThrows_PublishesExactlyOneFailureSnackbar_AndDoesNotThrow() {
        StaWindowTestHarness.Run(() => {
            var fileSystemDialog = new FakeFileSystemDialog { ThrowOnOpenFile = new InvalidOperationException("boom") };
            var eventPublisher = new FakeEventPublisher();
            var translator = new FakeTranslator();
            var vm = CreateViewModel(fileSystemDialog: fileSystemDialog, eventPublisher: eventPublisher, translator: translator);

            var exception = Record.Exception(
                () => vm.PerformApplicationRestoreCommand.ExecuteAsync(null).GetAwaiter().GetResult());

            Assert.Null(exception);
            var notification = Assert.Single(eventPublisher.PublishedEvents.OfType<SnackBarNotificationEvent>());
            Assert.Equal(NotificationType.Failure, notification.Type);
            Assert.Equal("AppConfigurationPageViewModel_Restore_Failure_Message", notification.Message);
            var request = Assert.Single(translator.Requests, r => r.Key == "AppConfigurationPageViewModel_Restore_Failure_Message");
            Assert.Equal(["boom"], request.Args);
        });
    }

    /// <summary>B11 (should-have): a message-dialog failure never escapes the command either.</summary>
    [Fact]
    public void PerformApplicationRestore_WhenMessageDialogThrows_PublishesExactlyOneFailureSnackbar_AndDoesNotThrow() {
        StaWindowTestHarness.Run(() => {
            var zipPath = CreateZipUnderBackups("InfoPhoenix-backup-20261105T143012Z.zip");
            var fileSystemDialog = new FakeFileSystemDialog { FilesToReturn = [zipPath] };
            var messageDialog = new FakeMessageDialog { ThrowOnShow = new InvalidOperationException("boom") };
            var eventPublisher = new FakeEventPublisher();
            var translator = new FakeTranslator();
            var vm = CreateViewModel(
                fileSystemDialog: fileSystemDialog, messageDialog: messageDialog,
                eventPublisher: eventPublisher, translator: translator);

            var exception = Record.Exception(
                () => vm.PerformApplicationRestoreCommand.ExecuteAsync(null).GetAwaiter().GetResult());

            Assert.Null(exception);
            var notification = Assert.Single(eventPublisher.PublishedEvents.OfType<SnackBarNotificationEvent>());
            Assert.Equal(NotificationType.Failure, notification.Type);
            Assert.Equal("AppConfigurationPageViewModel_Restore_Failure_Message", notification.Message);
            var request = Assert.Single(translator.Requests, r => r.Key == "AppConfigurationPageViewModel_Restore_Failure_Message");
            Assert.Equal(["boom"], request.Args);
        });
    }

    /// <summary>
    /// B10: two sequential invocations - the first cancelled, the second confirmed - prove no
    /// state leaks between runs (only the second invocation's effects are observed).
    /// </summary>
    [Fact]
    public void PerformApplicationRestore_InvokedTwice_FirstCancelledThenConfirmed_LeaksNoStateBetweenRuns() {
        StaWindowTestHarness.Run(() => {
            var zipPath = CreateZipUnderBackups("InfoPhoenix-backup-20261105T143012Z.zip");
            var fileSystemDialog = new FakeFileSystemDialog(); // first call: FilesToReturn defaults to [] (cancelled)
            var messageDialog = new FakeMessageDialog { ResultToReturn = MessageDialogResult.Yes };
            var eventPublisher = new FakeEventPublisher();
            var taskRunner = new FakeTaskRunner();
            var vm = CreateViewModel(
                fileSystemDialog: fileSystemDialog, messageDialog: messageDialog,
                eventPublisher: eventPublisher, taskRunner: taskRunner);

            vm.PerformApplicationRestoreCommand.ExecuteAsync(null).GetAwaiter().GetResult();

            Assert.Equal(1, fileSystemDialog.OpenFileCount);
            Assert.Empty(messageDialog.Calls);
            Assert.Empty(eventPublisher.PublishedEvents);
            Assert.Equal(0, taskRunner.CreateBuilderCount);

            fileSystemDialog.FilesToReturn = [zipPath];

            vm.PerformApplicationRestoreCommand.ExecuteAsync(null).GetAwaiter().GetResult();

            Assert.Equal(2, fileSystemDialog.OpenFileCount);
            Assert.Single(messageDialog.Calls);
            Assert.Equal(1, taskRunner.CreateBuilderCount);
            Assert.Single(eventPublisher.PublishedEvents.OfType<SnackBarNotificationEvent>());
        });
    }

    private string CreateZipUnderBackups(string fileName) {
        Directory.CreateDirectory(BackupsDirectory);
        var path = Path.Combine(BackupsDirectory, fileName);
        File.WriteAllBytes(path, [1, 2, 3]);

        return path;
    }

    private AppConfigurationPageViewModel CreateViewModel(
        FakeAppConfigurationManager? manager = null,
        FakeFileSystemDialog? fileSystemDialog = null,
        FakeMessageDialog? messageDialog = null,
        FakeTranslator? translator = null,
        FakeMediator? mediator = null,
        FakeEventPublisher? eventPublisher = null,
        FakeTaskRunner? taskRunner = null) {
        var applicationContext = new FakeApplicationContext {
            FileManager = new PhysicalFileManagerFactory().GetOrCreate(_tempRoot)
        };

        return new AppConfigurationPageViewModel(
            manager ?? new FakeAppConfigurationManager(),
            applicationContext,
            new FakeApplicationThemeApplier(),
            fileSystemDialog ?? new FakeFileSystemDialog(),
            messageDialog ?? new FakeMessageDialog(),
            (translator ?? new FakeTranslator()).For<AppConfigurationPageViewModel>(),
            mediator ?? new FakeMediator(),
            eventPublisher ?? new FakeEventPublisher(),
            taskRunner ?? new FakeTaskRunner(),
            NullLogger<AppConfigurationPageViewModel>.Instance
        );
    }
}