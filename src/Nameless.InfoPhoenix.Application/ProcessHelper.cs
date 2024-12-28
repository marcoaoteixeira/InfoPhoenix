using System.Diagnostics;

namespace Nameless.InfoPhoenix.Application;
public static class ProcessHelper {
    private const string EXPLORER_APP = "explorer.exe";
    private const string NOTEPAD_APP = "notepad.exe";

    public static void OpenDirectory(string directoryPath) {
        using var process = Process.Start(fileName: EXPLORER_APP,
                                          arguments: directoryPath);
    }

    public static void OpenTextFile(string filePath) {
        using var process = Process.Start(fileName: NOTEPAD_APP,
                                          arguments: filePath);
    }

    public static void OpenFile(string filePath) {
        using var process = Process.Start(fileName: EXPLORER_APP,
                                          arguments: filePath);
    }
}
