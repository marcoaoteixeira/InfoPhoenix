using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nameless.InfoPhoenix.Data;

// This is necessary to use the EF Core CLI tool
public class AppDbContextDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext> {
    public AppDbContext CreateDbContext(string[] args) {
        var databaseFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
                      .UseSqlite($"Data Source={databaseFilePath}")
                      .Options;

        return new AppDbContext(options);
    }
}