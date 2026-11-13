using System.ComponentModel;
using System.Globalization;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Nameless.InfoPhoenix.Client.Data;
using Nameless.InfoPhoenix.Client.Infrastructure.Validations;
using Nameless.InfoPhoenix.Client.Tests.Fakes;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.Windows.Localization;

namespace Nameless.InfoPhoenix.Client.Tests.Infrastructure.Validations;

/// <summary>
/// B6 (plan IPX-0015 section 8): <see cref="DocumentDirectoryWindowViewModelValidator"/> reads
/// every rule message once, inside its own constructor - a behavior that predates this ticket
/// and is deliberately not "fixed" here. The type change to
/// <see cref="ITranslator{T}"/> must not disturb that.
/// </summary>
public sealed class DocumentDirectoryWindowViewModelValidatorTests : IDisposable {
    // A real file-backed database, not "Data Source=:memory:" (AppDbContextTests's own
    // pattern): a bare in-memory connection string opens a fresh, schema-less database on every
    // connection EF Core happens to open, which would make the DirectoryPath uniqueness query
    // fail with "no such table" the moment the pooled connection is recycled between calls.
    private readonly string _databasePath = Path.Combine(
        Directory.CreateTempSubdirectory("ipx-0015-validator-").FullName, "app.db");

    public void Dispose() {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(Path.GetDirectoryName(_databasePath)!, recursive: true);
    }

    [Fact]
    public async Task Validate_WithALabelLeftEmpty_ReportsTheMessage_FromTheCultureActiveAtConstruction_NotAnyLaterSwitch() {
        var rootTranslator = new CultureEchoingTranslator();
        rootTranslator.SetCulture("en-GB");

        using var dbContext = CreateDbContext(_databasePath);
        dbContext.Database.EnsureCreated();
        var validator = new DocumentDirectoryWindowViewModelValidator(
            dbContext, rootTranslator.For<DocumentDirectoryWindowViewModelValidator>());

        // The switch happens AFTER construction - the frozen-at-construction messages must not
        // observe it.
        rootTranslator.SetCulture("pt-BR");

        var viewModel = new DocumentDirectoryWindowViewModel(
            new FakeFileSystemDialog(),
            new FakeMediator(),
            new FakeTranslator().For<DocumentDirectoryWindowViewModel>(),
            new FakeValidator()) {
            Label = string.Empty,
            DirectoryPath = "a-unique-directory-path",
            DisplayPosition = 0
        };

        var result = await validator.ValidateAsync(viewModel);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors, e => e.PropertyName == nameof(DocumentDirectoryWindowViewModel.Label));
        Assert.Equal("en-GB:DocumentDirectoryWindowViewModelValidator_Label_Empty_Error_Message", error.ErrorMessage);
    }

    private static AppDbContext CreateDbContext(string databasePath) {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={databasePath}").Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Composes the current culture's name into every returned value, so a test can tell which
    /// culture was active at the moment a message was actually read - matching the section
    /// composition SectionTranslator performs on every lookup (plan IPX-0015 §3.1), including
    /// the zero-argument, single-key overload.
    /// </summary>
    private sealed class CultureEchoingTranslator : ITranslator {
        public event PropertyChangedEventHandler? PropertyChanged;

        public CultureInfo CurrentCulture { get; private set; } = CultureInfo.InvariantCulture;

        public string this[string key] => $"{CurrentCulture.Name}:{key}";

        public string this[string key, object[] args] => $"{CurrentCulture.Name}:{key}";

        public void SetCulture(CultureInfo culture) {
            CurrentCulture = culture;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }

        public void SetCulture(string isoCode) {
            SetCulture(CultureInfo.GetCultureInfo(isoCode));
        }
    }
}
