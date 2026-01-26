using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Nameless.InfoPhoenix.Entities;
using Nameless.WPF.DependencyInjection;

namespace Nameless.InfoPhoenix.Data;

/// <summary>
///     Auditing save changes interceptor.
/// </summary>
[ServiceLifetime(Lifetime = ServiceLifetime.Singleton)]
public class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor {
    private readonly TimeProvider _timeProvider;

    /// <summary>
    ///     Initializes a new instance of
    ///     <see cref="AuditableEntitySaveChangesInterceptor"/> class.
    /// </summary>
    /// <param name="timeProvider">
    ///     The time provider.
    /// </param>
    public AuditableEntitySaveChangesInterceptor(TimeProvider timeProvider) {
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "InvertIf", Justification = "Avoid duplicate the return statement.")]
    [SuppressMessage("ReSharper", "SwitchStatementMissingSomeEnumCasesNoDefault", Justification = "We don't care for the other branches.")]
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) {
        var dbContext = eventData.Context;
        if (dbContext is not null) {
            var entries = dbContext.ChangeTracker.Entries<Entity>();
            var now = _timeProvider.GetUtcNow();

            foreach (var entry in entries) {
                switch (entry.State) {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = now;
                        break;
                    case EntityState.Modified:
                        entry.Entity.ModifiedAt = now;
                        break;
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
