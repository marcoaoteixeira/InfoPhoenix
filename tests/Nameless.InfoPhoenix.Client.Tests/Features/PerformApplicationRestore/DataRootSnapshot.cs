using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Nameless.InfoPhoenix.Client.Tests.Features.PerformApplicationRestore;

/// <summary>
/// Fitness helper F4 (plan IPX-0011 §4): captures relative path, size, SHA-256 and
/// <see cref="FileInfo.LastWriteTimeUtc"/> for every file under one or more roots, so a test can
/// assert "exactly as they were" literally, instead of checking a handful of files by hand.
/// <c>temporary\</c> is excluded by default - a restore's own staging churn there is expected to
/// disappear, and asserting on it would make every test couple to that implementation detail.
/// </summary>
internal sealed record DataRootSnapshot(IReadOnlyDictionary<string, FileFingerprint> Files) {
    internal static DataRootSnapshot Capture(params string[] roots) {
        var files = new Dictionary<string, FileFingerprint>();

        foreach (var root in roots) {
            if (!Directory.Exists(root)) {
                continue;
            }

            foreach (var filePath in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)) {
                var relativePath = Path.GetRelativePath(root, filePath);

                if (relativePath.StartsWith("temporary" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                var key = $"{Path.GetFullPath(root)}|{relativePath}";
                var info = new FileInfo(filePath);

                files[key] = new FileFingerprint(info.Length, info.LastWriteTimeUtc, ComputeSha256(filePath));
            }
        }

        return new DataRootSnapshot(files);
    }

    private static string ComputeSha256(string filePath) {
        using var stream = File.OpenRead(filePath);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }
}

internal sealed record FileFingerprint(long Length, DateTime LastWriteTimeUtc, string Sha256);
