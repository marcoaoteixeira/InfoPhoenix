using System.IO;
using Microsoft.EntityFrameworkCore;
using Nameless.InfoPhoenix.Data;

namespace Nameless.InfoPhoenix.Testing.Tools;

public static class AppDbContextFactory {
    public static readonly string BaseDirectoryPath = Path.Combine(typeof(AppDbContextFactory).Assembly.GetDirectoryPath(), "databases");

    public static async Task<AppDbContext> CreateAsync(Action<AppDbContextFactoryOptions>? configure = null, CancellationToken cancellationToken = default) {
        var innerConfigure = configure ?? (_ => { });
        var options = new AppDbContextFactoryOptions();

        innerConfigure(options);

        var builder = new DbContextOptionsBuilder<AppDbContext>();

        builder = options.UseInMemory
            ? builder.ConfigureInMemoryDatabase(options)
            : builder.ConfigureFileDatabase(options);

        builder.AddInterceptors(options.Interceptors)
               .UseAsyncSeeding(options.Seeding ?? ((_, _, _) => Task.CompletedTask));

        var dbContext = new AppDbContext(builder.Options);

        // It will trigger the seeding process.
        if (options.UseInMemory) { await dbContext.Database.EnsureCreatedAsync(cancellationToken); }
        else { await dbContext.Database.MigrateAsync(cancellationToken); }

        return dbContext;
    }

    extension(DbContextOptionsBuilder<AppDbContext> self)
    {
        private DbContextOptionsBuilder<AppDbContext> ConfigureFileDatabase(AppDbContextFactoryOptions options) {
            Directory.CreateDirectory(BaseDirectoryPath);

            var databaseName = options.DatabaseName ?? Guid.NewGuid().ToString("N");

            var databaseFilePath = Path.Combine(BaseDirectoryPath, $"{databaseName}.db");
            var connectionString = string.Format(Constants.Database.ConnStrPattern, databaseFilePath);

            return self.UseSqlite(connectionString);
        }

        private DbContextOptionsBuilder<AppDbContext> ConfigureInMemoryDatabase(AppDbContextFactoryOptions options) {
            var databaseName = options.DatabaseName ?? Guid.NewGuid().ToString("N");

            return self.UseInMemoryDatabase($"{databaseName}.db");
        }
    }
}