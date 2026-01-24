using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nameless.InfoPhoenix.Entities;
using static Nameless.InfoPhoenix.Data.Configurations.Naming.DocumentDirectories;

namespace Nameless.InfoPhoenix.Data.Configurations;

/// <summary>
///     Entity type configuration for <see cref="DocumentDirectory"/>.
/// </summary>
public class DocumentDirectoryEntityTypeConfiguration : IEntityTypeConfiguration<DocumentDirectory> {
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DocumentDirectory> builder) {
        builder.ToTable(Table);

        builder.Property(entity => entity.ID)
               .HasColumnName(Columns.ID)
               .ValueGeneratedOnAdd();

        builder.HasKey(entity => entity.ID)
               .HasName(Constraints.Pk);

        builder.Property(entity => entity.Label)
               .HasColumnName(Columns.Label)
               .HasMaxLength(1024);

        builder.Property(entity => entity.DirectoryPath)
               .HasColumnName(Columns.DirectoryPath)
               .HasMaxLength(4096);

        builder.Property(entity => entity.DisplayPosition)
               .HasColumnName(Columns.DisplayPosition);

        builder.Property(entity => entity.LastIndexingTime)
               .HasColumnName(Columns.LastIndexingTime);

        builder.Property(entity => entity.CreatedAt)
               .HasColumnName(Columns.CreatedAt);

        builder.Property(entity => entity.ModifiedAt)
               .HasColumnName(Columns.ModifiedAt);

        builder.HasIndex(entity => entity.ID, name: Indexes.ID);
        builder.HasIndex(entity => entity.DirectoryPath, name: Indexes.DirectoryPath)
               .IsUnique();
    }
}
