using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nameless.Application;
using Nameless.FileSystem;
using Nameless.InfoPhoenix.Bootstrap;
using Nameless.InfoPhoenix.Domains.Entities;

namespace Nameless.InfoPhoenix.Domains.Bootstrap;

public sealed class SeedDocumentDirectoriesIntoDatabaseStep : IStep {
    private readonly string _resourcesFolderPath = typeof(SeedDocumentDirectoriesIntoDatabaseStep)
                                                   .Assembly
                                                   .GetDirectoryPath(@"..\..\..\..\resources\sample_docs");

    private readonly IServiceProvider _serviceProvider;

    public string Name => "Seeding InfoPhoenix Database for Document Directories";

    public int Order => 999;

    public bool ThrowOnFailure => true;

    public bool Skip => false;

    public SeedDocumentDirectoriesIntoDatabaseStep(IServiceProvider serviceProvider) {
        _serviceProvider = Prevent.Argument.Null(serviceProvider);
    }

    public async Task ExecuteAsync() {
        using var scope = _serviceProvider.CreateScope();

        var logger = scope.ServiceProvider.GetLogger<InitializeAppDbContextStep>();

        logger.StepExecutionStarting(this);

        try {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var applicationContext = scope.ServiceProvider.GetRequiredService<IApplicationContext>();
            var fileSystem = scope.ServiceProvider.GetRequiredService<IFileSystem>();

            var documentDirectories = CreateDocumentDirectories(applicationContext, fileSystem);
            foreach (var documentDirectory in documentDirectories) {
                if (await dbContext.DocumentDirectories.ContainsAsync(documentDirectory)) {
                    continue;
                }
                await dbContext.DocumentDirectories.AddAsync(documentDirectory);

                CopyResourceFiles(documentDirectory);
            }
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex) { logger.StepExecutionFailure(this, ex); }
        finally { logger.StepExecutionFinished(this); }
    }

    private static DocumentDirectory[] CreateDocumentDirectories(IApplicationContext applicationContext,
                                                                 IFileSystem fileSystem) {
        var sampleDocsDir = Path.Combine(applicationContext.AppDataFolderPath, "sample_docs");

        var loremIpsumDir = Path.Combine(sampleDocsDir, "lorem_ipsum");
        fileSystem.Directory.Create(loremIpsumDir);

        var poemsDir = Path.Combine(sampleDocsDir, "poems");
        fileSystem.Directory.Create(poemsDir);

        var proverbsDir = Path.Combine(sampleDocsDir, "proverbs");
        fileSystem.Directory.Create(proverbsDir);

        var storiesDir = Path.Combine(sampleDocsDir, "stories");
        fileSystem.Directory.Create(storiesDir);

        var textsDir = Path.Combine(sampleDocsDir, "texts");
        fileSystem.Directory.Create(textsDir);

        var votesDir = Path.Combine(sampleDocsDir, "votes");
        fileSystem.Directory.Create(votesDir);

        return [
            new DocumentDirectory
            {
                ID = Guid.Parse("e6fbd900-bec6-45e0-ba16-557c9cd8cbe5"),
                Label = "Lorem Ipsum",
                DirectoryPath = loremIpsumDir,
                Order = 1,
                CreatedAt = DateTime.Now,
                ModifiedAt = null
            },
            new DocumentDirectory
            {
                ID = Guid.Parse("93961c74-33c2-436f-a3b8-dbae6b644ad7"),
                Label = "Beautiful Poems",
                DirectoryPath = poemsDir,
                Order = 2,
                CreatedAt = DateTime.Now,
                ModifiedAt = null
            },
            new DocumentDirectory
            {
                ID = Guid.Parse("65acab60-0075-41e7-a0e7-494b05209f3e"),
                Label = "Thoughtful Proverbs",
                DirectoryPath = proverbsDir,
                Order = 3,
                CreatedAt = DateTime.Now,
                ModifiedAt = null
            },
            new DocumentDirectory
            {
                ID = Guid.Parse("7a09d54c-0ce1-4cd1-8582-a1ecc28f6ad2"),
                Label = "Interesting Stories",
                DirectoryPath = storiesDir,
                Order = 4,
                CreatedAt = DateTime.Now,
                ModifiedAt = null
            },
            new DocumentDirectory
            {
                ID = Guid.Parse("faf1cde5-319d-4d4c-a87d-5d297020db0f"),
                Label = "Random Texts",
                DirectoryPath = textsDir,
                Order = 5,
                CreatedAt = DateTime.Now,
                ModifiedAt = null
            },
            new DocumentDirectory
            {
                ID = Guid.Parse("4d888e0f-052e-4d48-aaf8-44febce7d65f"),
                Label = "Work Related Votes",
                DirectoryPath = votesDir,
                Order = 6,
                CreatedAt = DateTime.Now,
                ModifiedAt = null
            }
        ];
    }

    private void CopyResourceFiles(DocumentDirectory documentDirectory) {
        var documentDirectoryFolderName = Path.GetFileName(documentDirectory.DirectoryPath);
        var documentDirectoryResourcesFolderPath = Path.Combine(_resourcesFolderPath, documentDirectoryFolderName);

        foreach (var file in Directory.GetFiles(documentDirectoryResourcesFolderPath, "*.*", SearchOption.AllDirectories)) {
            var fileName = Path.GetFileName(file);

            File.Copy(file, Path.Combine(documentDirectory.DirectoryPath, fileName), overwrite: true);
        }
    }
}