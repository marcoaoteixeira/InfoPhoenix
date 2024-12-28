using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nameless.InfoPhoenix.Bootstrap;
using Nameless.InfoPhoenix.Domains.Entities;

namespace Nameless.InfoPhoenix.Domains.Bootstrap;

public sealed class InitializeAppDbContextStep : IStep {
    private readonly IServiceProvider _serviceProvider;

    public string Name => "Initializing InfoPhoenix Database Context";

    public int Order => 1;

    public bool ThrowOnFailure => true;

    public bool Skip => false;

    public InitializeAppDbContextStep(IServiceProvider serviceProvider) {
        _serviceProvider = Prevent.Argument.Null(serviceProvider);
    }

    public async Task ExecuteAsync() {
        using var scope = _serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetLogger<InitializeAppDbContextStep>();

        logger.StepExecutionStarting(this);

        try {
            if (dbContext.Database.IsRelational()) {
                await dbContext.Database.MigrateAsync();
            }
            else {
                logger.NonRelationalDatabaseSkipMigration();
            }
        }
        catch (Exception ex) { logger.StepExecutionFailure(this, ex); }
        finally { logger.StepExecutionFinished(this); }
    }
}