using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nameless.InfoPhoenix.Client.Data;

/// <summary>
///     Only necessary for <c>dotnet ef *</c>
/// </summary>
public sealed class AppDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext> {
    public AppDbContext CreateDbContext(string[] args) {
        var databaseFilePath = SysPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
                      .UseSqlite($"Data Source={databaseFilePath}")
                      .Options;

        return new AppDbContext(options);
    }
}
