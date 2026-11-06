using Nameless.InfoPhoenix.Client.Features.PerformApplicationBackup;
using Nameless.InfoPhoenix.Client.Features.PerformApplicationRestore;

namespace Nameless.InfoPhoenix.Client.Tests.Features.PerformApplicationRestore;

/// <summary>T2 (plan §5): direct unit tests of the decided D1 "schema only" compatibility rule.</summary>
public sealed class BackupCompatibilityTests {
    private static BackupManifest Manifest(
        int formatVersion = 1,
        string application = "InfoPhoenix",
        string? databaseMigrationId = null,
        bool database = false) {
        return new BackupManifest(
            FormatVersion: formatVersion,
            Application: application,
            AppVersion: "v1.0.0",
            DatabaseMigrationId: databaseMigrationId,
            CreatedAtUtc: DateTimeOffset.UnixEpoch,
            Contents: new BackupManifestContents(Database: database, SearchIndex: false, Configuration: true));
    }

    [Fact]
    public void Evaluate_WithNullMigrationIdsAndDatabasePresent_IsCompatible() {
        var manifest = Manifest(database: true, databaseMigrationId: null);

        var result = BackupCompatibility.Evaluate(manifest, expectedMigrationId: null);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_WithEqualNonNullMigrationIds_IsCompatible() {
        var manifest = Manifest(database: true, databaseMigrationId: "20260101_Initial");

        var result = BackupCompatibility.Evaluate(manifest, expectedMigrationId: "20260101_Initial");

        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("20260101_Initial")]
    [InlineData("some-other-id")]
    public void Evaluate_WithDatabaseAbsent_IsCompatible_RegardlessOfMigrationId(string? expected) {
        var manifest = Manifest(database: false, databaseMigrationId: "whatever-the-backup-claims");

        var result = BackupCompatibility.Evaluate(manifest, expectedMigrationId: expected);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_WithMismatchingMigrationIds_AndDatabasePresent_IsSchemaMismatch() {
        var manifest = Manifest(database: true, databaseMigrationId: "20260101_Initial");

        var result = BackupCompatibility.Evaluate(manifest, expectedMigrationId: "20270101_Later");

        Assert.NotNull(result);
        Assert.Equal("SchemaMismatch", result.ReasonKey);
        Assert.Equal(["20260101_Initial", "20270101_Later"], result.Args);
    }

    [Fact]
    public void Evaluate_WithNullBackupMigrationId_AndNonNullExpected_AndDatabasePresent_RendersDashForNull() {
        var manifest = Manifest(database: true, databaseMigrationId: null);

        var result = BackupCompatibility.Evaluate(manifest, expectedMigrationId: "20270101_Later");

        Assert.NotNull(result);
        Assert.Equal(["-", "20270101_Later"], result.Args);
    }

    [Fact]
    public void Evaluate_WithUnsupportedFormatVersion_IsUnsupportedFormat() {
        var manifest = Manifest(formatVersion: 2);

        var result = BackupCompatibility.Evaluate(manifest, expectedMigrationId: null);

        Assert.NotNull(result);
        Assert.Equal("UnsupportedFormat", result.ReasonKey);
        Assert.Equal([2], result.Args);
    }

    [Fact]
    public void Evaluate_WithWrongApplication_IsWrongApplication() {
        var manifest = Manifest(application: "Other");

        var result = BackupCompatibility.Evaluate(manifest, expectedMigrationId: null);

        Assert.NotNull(result);
        Assert.Equal("WrongApplication", result.ReasonKey);
        Assert.Equal(["Other"], result.Args);
    }

    [Fact]
    public void Evaluate_WithBothFormatAndApplicationWrong_FormatWinsByCheckOrder() {
        var manifest = Manifest(formatVersion: 2, application: "Other");

        var result = BackupCompatibility.Evaluate(manifest, expectedMigrationId: null);

        Assert.NotNull(result);
        Assert.Equal("UnsupportedFormat", result.ReasonKey);
    }
}