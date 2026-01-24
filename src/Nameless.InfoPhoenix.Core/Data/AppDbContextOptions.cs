using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Nameless.InfoPhoenix.Data;
public class AppDbContextOptions {
    internal HashSet<Type> Interceptors { get; } = [];

    internal Func<DbContext, bool, CancellationToken, Task> SeedingAsync { get; set; } = (_, _, _) => Task.CompletedTask;

    internal Action<DbContext, bool> Seeding { get; set; } = (_, _) => { };

    public AppDbContextOptions RegisterInterceptor<TInterceptor>()
        where TInterceptor : IInterceptor {
        Interceptors.Add(typeof(TInterceptor));

        return this;
    }

    public AppDbContextOptions RegisterSeeding(Action<DbContext, bool> callback) {
        Seeding = callback;

        return this;
    }

    public AppDbContextOptions RegisterSeedingAsync(Func<DbContext, bool, CancellationToken, Task> callback) {
        SeedingAsync = callback;

        return this;
    }
}
