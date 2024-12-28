using Microsoft.EntityFrameworkCore;

namespace Nameless.InfoPhoenix.Domains.Entities;

public class AppDbContext : DbContext {
    public virtual DbSet<DocumentDirectory> DocumentDirectories
        => Set<DocumentDirectory>();

    public virtual DbSet<Document> Documents
        => Set<Document>();

    public AppDbContext(DbContextOptions<AppDbContext> opts)
        : base(opts) { }

    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);

        builder
            .Entity<DocumentDirectory>(entity => {
                entity.ToTable("DocumentDirectories");

                entity.HasKey(prop => prop.ID);

                entity.Property(prop => prop.Label)
                      .HasMaxLength(1024);
                entity.Property(prop => prop.DirectoryPath)
                      .HasMaxLength(1024);
                entity.Property(prop => prop.Order);
                entity.Property(prop => prop.LastIndexingTime);
                entity.Property(prop => prop.CreatedAt);
                entity.Property(prop => prop.ModifiedAt);

                entity.HasIndex(prop => prop.DirectoryPath)
                      .IsUnique();

                entity.HasMany<Document>()
                      .WithOne()
                      .HasForeignKey(prop => prop.DocumentDirectoryID)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);
            });

        builder
            .Entity<Document>(entity => {
                entity.ToTable("Documents");

                entity.HasKey(prop => prop.ID);

                entity.Property(prop => prop.FilePath)
                      .HasMaxLength(1024);
                entity.Property(prop => prop.Content)
                      .HasMaxLength(int.MaxValue);
                entity.Property(prop => prop.RawFile);
                entity.Property(prop => prop.LastIndexingTime);
                entity.Property(prop => prop.LastWriteTime);
                entity.Property(prop => prop.CreatedAt);
                entity.Property(prop => prop.ModifiedAt);

                entity.HasIndex(prop => prop.FilePath)
                      .IsUnique();
            });
    }
}
