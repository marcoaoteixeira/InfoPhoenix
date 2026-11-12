using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nameless.Contracts;
using Nameless.InfoPhoenix.Client.Data;
using Nameless.Windows.Bootstrap;
using Nameless.Windows.Bootstrap.Notification;
using Nameless.Windows.Localization;

namespace Nameless.InfoPhoenix.Client.Bootstrap.Steps;

/// <summary>
///     Entity Framework Core Execute Migration Step
/// </summary>
public class EntityFrameworkCoreExecuteMigrationStep : StepBase {
    private readonly IServiceProvider _provider;
    private readonly ITranslator _translator;

    /// <inheritdoc />
    public override string DisplayName => _translator["EntityFrameworkCoreExecuteMigrationStep_DisplayName"];

    /// <summary>
    ///     Initializes a new instance of
    ///     <see cref="EntityFrameworkCoreExecuteMigrationStep"/> class.
    /// </summary>
    /// <param name="provider">
    ///     The service provider.
    /// </param>
    /// <param name="translator">
    ///     The translator.
    /// </param>
    public EntityFrameworkCoreExecuteMigrationStep(IServiceProvider provider, ITranslator translator) {
        _provider = Throws.When.Null(provider);
        _translator = Throws.When.Null(translator);
    }
    
    /// <inheritdoc />
    public override async Task ExecuteAsync(IProgress<StepProgress> progress, CancellationToken cancellationToken) {
        progress.ReportInformation(DisplayName, _translator["EntityFrameworkCoreExecuteMigrationStep_ExecuteAsync_Starting"]);

        await using var scope = _provider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        progress.ReportInformation(DisplayName, _translator["EntityFrameworkCoreExecuteMigrationStep_ExecuteAsync_Finished"]);
    }
}
