using Microsoft.EntityFrameworkCore;
using Nameless.InfoPhoenix.Client.Data;

namespace Nameless.InfoPhoenix.Client.Tests;

public class AppDbContextTests {
    [Fact]
    public void CanCreateInMemoryContext() {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new AppDbContext(options);

        Assert.NotNull(context);
    }
}
