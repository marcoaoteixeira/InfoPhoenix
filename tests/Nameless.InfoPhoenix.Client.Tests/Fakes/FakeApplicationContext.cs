using Nameless.Application;
using Nameless.IO;

namespace Nameless.InfoPhoenix.Client.Tests.Fakes;

/// <summary>
/// Hand-rolled <see cref="IApplicationContext"/> fake. Only the members a given test actually
/// reads are given a meaningful value; everything else throws if ever touched, so a test that
/// accidentally depends on it fails loudly instead of silently reading a meaningless default.
/// </summary>
internal sealed class FakeApplicationContext : IApplicationContext {
    public string ApplicationName { get; init; } = "InfoPhoenix.Tests";

    public string Version { get; init; } = "0.0.0-test";

    public string EnvironmentName => throw new NotSupportedException();

    public IFileManager FileManager { get; init; } = new UnsupportedFileManager();

    public string GetEnvironmentVariable(string name) {
        throw new NotSupportedException();
    }

    private sealed class UnsupportedFileManager : IFileManager {
        public string Root => throw new NotSupportedException();

        public IFile GetFile(string relativePath) {
            throw new NotSupportedException();
        }

        public IDirectory GetDirectory(string relativePath) {
            throw new NotSupportedException();
        }

        public string GetFullPath(string relativePath) {
            throw new NotSupportedException();
        }
    }
}
