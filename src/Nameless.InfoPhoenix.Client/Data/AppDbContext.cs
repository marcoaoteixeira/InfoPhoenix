using Microsoft.EntityFrameworkCore;

namespace Nameless.InfoPhoenix.Client.Data;

public class AppDbContext(DbContextOptions<AppDbContext> opts) : DbContext(opts);
