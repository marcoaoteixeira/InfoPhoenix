using Microsoft.EntityFrameworkCore.Diagnostics;
using Nameless.InfoPhoenix.Data;

namespace Nameless.InfoPhoenix.Testing.Tools;

public class AppDbContextFactoryOptions {
    internal HashSet<IInterceptor> Interceptors { get; } = [];

    internal Func<AppDbContext, bool, CancellationToken, Task>? Seeding { get; set; }

    public string? DatabaseName { get; set; }

    public bool UseInMemory { get; set; }

    public AppDbContextFactoryOptions UseInterceptor(IInterceptor interceptor) {
        Interceptors.Add(interceptor);

        return this;
    }

    public AppDbContextFactoryOptions UseSeeding(Func<AppDbContext, bool, CancellationToken, Task> seeding) {
        Seeding = seeding;

        return this;
    }
}