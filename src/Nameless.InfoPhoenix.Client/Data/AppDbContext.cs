using Microsoft.EntityFrameworkCore;
using Nameless.InfoPhoenix.Client.Data.Configs;
using Nameless.InfoPhoenix.Client.Data.Entities;

namespace Nameless.InfoPhoenix.Client.Data;

public class AppDbContext(DbContextOptions<AppDbContext> opts) : DbContext(opts) {
    public DbSet<DocumentDirectory> DocumentDirectories => Set<DocumentDirectory>();
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder builder) {
        builder.ApplyConfiguration(new DocumentDirectoryEntityTypeConfiguration());
        builder.ApplyConfiguration(new DocumentEntityTypeConfiguration());
    }
}
