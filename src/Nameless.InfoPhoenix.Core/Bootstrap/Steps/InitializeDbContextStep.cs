using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nameless.Bootstrap;
using Nameless.InfoPhoenix.Data;
using Nameless.InfoPhoenix.Resources;
using Nameless.WPF.Bootstrap;

namespace Nameless.InfoPhoenix.Bootstrap.Steps;

/// <summary>
///     Step to initialize the DbContext.
/// </summary>
public class InitializeDbContextStep : StepBase {
    private readonly IServiceProvider _provider;

    /// <inheritdoc />
    public override string Name => Strings.InitializeDbContextBootstrapStep_Name;

    /// <summary>
    ///     Initializes a new instance of
    ///     <see cref="InitializeDbContextStep"/> class.
    /// </summary>
    /// <param name="provider">
    ///     The service provider.
    /// </param>
    public InitializeDbContextStep(IServiceProvider provider) {
        _provider = provider;
    }

    /// <inheritdoc />
    public override async Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken) {
        var progress = context.GetStepProgress();

        await progress.ReportAsync(this, "Inicializando a base de dados...");

        using var scope = _provider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetLogger<InitializeDbContextStep>();

        if (!dbContext.Database.IsRelational()) {
            await progress.ReportAsync(this, "Banco de dados não relacional.");

            logger.SkipMigrationForNonRelationalDatabase();

            return;
        }

        await progress.ReportAsync(this, "Executando migrações...");

        await dbContext.Database.MigrateAsync(cancellationToken);

        await progress.ReportAsync(this, "Migrações executadas com sucesso!");
    }
}
