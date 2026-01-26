using Microsoft.EntityFrameworkCore;
using Nameless.InfoPhoenix.Entities;

namespace Nameless.InfoPhoenix.Data;

/// <summary>
///     Represents the application <see cref="DbContext"/>.
/// </summary>
public class AppDbContext : DbContext {
    public DbSet<DocumentDirectory> DocumentDirectories => Set<DocumentDirectory>();

    public DbSet<Document> Documents => Set<Document>();

    /// <summary>
    ///     Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">
    ///     The database context options.
    /// </param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
}
