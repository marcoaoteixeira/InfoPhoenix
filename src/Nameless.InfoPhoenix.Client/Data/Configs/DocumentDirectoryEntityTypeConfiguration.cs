using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nameless.EntityFrameworkCore.Config;
using Nameless.InfoPhoenix.Client.Data.Entities;

namespace Nameless.InfoPhoenix.Client.Data.Configs;

/// <summary>
///     Entity type configuration for <see cref="DocumentDirectory"/>.
/// </summary>
public class DocumentDirectoryEntityTypeConfiguration : EntityTypeConfigurationBase<DocumentDirectory> {
    /// <summary>
    ///     Initializes a new instance of
    ///     <see cref="DocumentDirectoryEntityTypeConfiguration"/> class.
    /// </summary>
    public DocumentDirectoryEntityTypeConfiguration() : base("DocumentDirectories") { }

    /// <inheritdoc />
    protected override void ConfigureCore(EntityTypeBuilder<DocumentDirectory> builder) {
        builder.Property(entity => entity.Label)
               .HasMaxLength(Conventions.Column.Size.Small);

        builder.Property(entity => entity.DirectoryPath)
               .HasMaxLength(Conventions.Column.Size.Medium);

        builder.Property(entity => entity.DisplayPosition);

        builder.Property(entity => entity.LastIndexingTime);

        builder.HasIndex(entity => entity.DirectoryPath, name: $"IX_{TableName}_{nameof(DocumentDirectory.DirectoryPath)}")
               .IsUnique();
    }
}
