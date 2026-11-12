using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nameless.EntityFrameworkCore.Config;
using Nameless.InfoPhoenix.Client.Data.Entities;

namespace Nameless.InfoPhoenix.Client.Data.Configs;

/// <summary>
///     Entity type configuration for <see cref="Document"/>.
/// </summary>
public class DocumentEntityTypeConfiguration : EntityTypeConfigurationBase<Document> {
    /// <summary>
    ///     Initializes a new instance of
    ///     <see cref="DocumentEntityTypeConfiguration"/> class.
    /// </summary>
    public DocumentEntityTypeConfiguration() : base("Documents") { }

    /// <inheritdoc />
    protected override void ConfigureCore(EntityTypeBuilder<Document> builder) {
        builder.Property(entity => entity.FilePath)
               .HasMaxLength(Conventions.Column.Size.Medium);

        builder.Property(entity => entity.Content)
               .HasMaxLength(Conventions.Column.Size.Max);

        builder.Property(entity => entity.RawFile);

        builder.Property(entity => entity.LastIndexingTime);

        builder.Property(entity => entity.LastWriteTime);

        builder.HasOne(entity => entity.DocumentDirectory)
               .WithMany(documentDirectory => documentDirectory.Documents)
               .HasForeignKey(entity => entity.DocumentDirectoryID)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => entity.FilePath, name: $"IX_{TableName}_{nameof(Document.FilePath)}")
               .IsUnique();
    }
}
