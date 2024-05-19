using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Bootstrapping;

namespace Nameless.InfoPhoenix.Domain.Bootstrapping.Impl {
    public sealed class InitializeAppDbContextStep : IStep {
        #region Private Read-Only Fields

        private readonly IServiceProvider _serviceProvider;

        #endregion

        #region Public Constructors

        public InitializeAppDbContextStep(IServiceProvider serviceProvider) {
            _serviceProvider = Guard.Against.Null(serviceProvider, nameof(serviceProvider));
        }

        #endregion

        #region IStep Members

        public string Name => "Initializing Application DbContext";
        public int Order => 0;
        public bool ThrowOnFailure => true;
        public bool Skip => false;

        public async Task ExecuteAsync() {
            using var scope = _serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetLogger<InitializeAppDbContextStep>();

            logger
                .LogInformation("Initializing DB Context for InfoPhoenix");

            await dbContext.Database.EnsureCreatedAsync();
            await dbContext.Database.MigrateAsync();
        }

        #endregion
    }
}
