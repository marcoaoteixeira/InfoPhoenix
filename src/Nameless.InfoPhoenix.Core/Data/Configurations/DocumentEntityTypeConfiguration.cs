using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static Nameless.InfoPhoenix.Data.Configurations.Naming.Documents;

namespace Nameless.InfoPhoenix.Data.Configurations;

/// <summary>
///     Entity type configuration for <see cref="Document"/>.
/// </summary>
public class DocumentEntityTypeConfiguration : IEntityTypeConfiguration<Document> {
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Document> builder) {
        builder.ToTable(Table);

        builder.Property(entity => entity.ID)
               .HasColumnName(Columns.ID)
               .ValueGeneratedOnAdd();

        builder.HasKey(entity => entity.ID)
               .HasName(Constraints.Pk);

        builder.Property(entity => entity.FilePath)
               .HasColumnName(Columns.FilePath)
               .HasMaxLength(4096);

        builder.Property(entity => entity.Content)
               .HasColumnName(Columns.Content)
               .HasMaxLength(int.MaxValue);

        builder.Property(entity => entity.RawFile)
               .HasColumnName(Columns.RawFile);

        builder.Property(entity => entity.LastIndexingTime)
               .HasColumnName(Columns.LastIndexingTime);

        builder.Property(entity => entity.LastWriteTime)
               .HasColumnName(Columns.LastWriteTime);

        builder.Property(entity => entity.CreatedAt)
               .HasColumnName(Columns.CreatedAt);

        builder.Property(entity => entity.ModifiedAt)
               .HasColumnName(Columns.ModifiedAt);

        builder.HasIndex(entity => entity.ID, name: Indexes.ID);
        builder.HasIndex(entity => entity.FilePath, name: Indexes.FilePath)
               .IsUnique();
    }
}
