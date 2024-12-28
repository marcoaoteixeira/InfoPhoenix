using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nameless.InfoPhoenix.Domains.Entities;

// This is necessary to use the EF Core CLI tool
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext> {
    public AppDbContext CreateDbContext(string[] args) {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=database.db")
            .Options;

        return new AppDbContext(options);
    }
}