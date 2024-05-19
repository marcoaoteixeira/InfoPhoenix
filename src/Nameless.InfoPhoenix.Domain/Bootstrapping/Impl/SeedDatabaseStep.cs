using Nameless.InfoPhoenix.Domain.Entities;
using Nameless.Infrastructure;

namespace Nameless.InfoPhoenix.Bootstrapping.Impl {
    public sealed class SeedDatabaseStep : IStep {
        private readonly IApplicationContext _applicationContext;

        public SeedDatabaseStep(IApplicationContext applicationContext) {
            _applicationContext = applicationContext ?? throw new ArgumentNullException();
        }

        public string Name => "Seeding Database (TEST PURPOSE)";
        public int Order => 999;
        public bool ThrowOnFailure => false;
        public bool Skip => true;

        public Task ExecuteAsync() {
            var loremFolder = Path.Combine(_applicationContext.ApplicationDataFolderPath, "sample_docs", "Lorem");
            var poemsFolder = Path.Combine(_applicationContext.ApplicationDataFolderPath, "sample_docs", "Poems");
            var votesFolder = Path.Combine(_applicationContext.ApplicationDataFolderPath, "sample_docs", "Votes");

            var documentDirectories = new DocumentDirectory[] {
                new() {
                    ID = Guid.Parse("e6fbd900-bec6-45e0-ba16-557c9cd8cbe5"),
                    Label = "Lorem Ipsun",
                    DirectoryPath = loremFolder,
                    Order = 1,
                    CreatedAt = DateTime.Now,
                    ModifiedAt = null
                },
                new() {
                    ID = Guid.Parse("93961c74-33c2-436f-a3b8-dbae6b644ad7"),
                    Label = "Beautiful Poems",
                    DirectoryPath = poemsFolder,
                    Order = 2,
                    CreatedAt = DateTime.Now,
                    ModifiedAt = null
                },
                new() {
                    ID = Guid.Parse("4d888e0f-052e-4d48-aaf8-44febce7d65f"),
                    Label = "Votes",
                    DirectoryPath = votesFolder,
                    Order = 3,
                    CreatedAt = DateTime.Now,
                    ModifiedAt = null
                }
            };

            return Task.CompletedTask;
        }
    }
}
