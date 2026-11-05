using Nameless.Windows.Dialogs.Message;

namespace Nameless.InfoPhoenix.Client.Tests.Fakes;

/// <summary>
/// Hand-rolled, shared <see cref="IMessageDialog"/> fake (plan IPX-0010 §6). Unlike
/// <c>MainWindowTests</c>'s private one, this records every call's message and resolved options
/// so a test can assert the icon/buttons/title of the restore overwrite warning. Defaults to
/// <see cref="MessageDialogResult.No"/>, the safe (non-destructive) answer.
/// </summary>
internal sealed class FakeMessageDialog : IMessageDialog {
    public List<(string Message, MessageDialogOptions Options)> Calls { get; } = [];

    public MessageDialogResult ResultToReturn { get; set; } = MessageDialogResult.No;

    /// <summary>
    /// When set, <see cref="Show"/> throws this instead of recording the call and returning
    /// <see cref="ResultToReturn"/> - simulates a bug-path failure (plan B11).
    /// </summary>
    public Exception? ThrowOnShow { get; set; }

    public MessageDialogResult Show(string message, Action<MessageDialogOptions> configure) {
        var options = new MessageDialogOptions();
        configure(options);

        if (ThrowOnShow is not null) {
            throw ThrowOnShow;
        }

        Calls.Add((message, options));

        return ResultToReturn;
    }
}