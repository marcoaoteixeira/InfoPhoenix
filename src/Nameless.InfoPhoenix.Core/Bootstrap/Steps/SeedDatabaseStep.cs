using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nameless.Bootstrap;
using Nameless.InfoPhoenix.Data;
using Nameless.InfoPhoenix.Entities;
using Nameless.Infrastructure;
using Nameless.WPF.Bootstrap;

namespace Nameless.InfoPhoenix.Bootstrap.Steps;

public class SeedDatabaseStep : StepBase {
    private static readonly string ResourcesFolderPath = Path.Combine(
        typeof(SeedDatabaseStep).Assembly.GetDirectoryPath(),
        @"..\..\..\..\..\..\resources\sample_docs"
    );

    private readonly IServiceProvider _provider;

    public override string Name => "Seed Database Step";

    public SeedDatabaseStep(IServiceProvider provider) {
        _provider = provider;
    }

    public override async Task ExecuteAsync(FlowContext context, CancellationToken cancellationToken) {
        var progress = context.GetStepProgress();

        await progress.ReportAsync(this, "Inserindo dados iniciais na base de dados...");

        await using var scope = _provider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var applicationContext = scope.ServiceProvider.GetRequiredService<IApplicationContext>();

        await progress.ReportAsync(this, "Criando diretório de documentos...");
        var documentDirectories = CreateDocumentDirectories(applicationContext);

        foreach (var documentDirectory in documentDirectories) {
            if (await dbContext.DocumentDirectories.ContainsAsync(documentDirectory, cancellationToken)
                               .SkipContextSync()) {
                continue;
            }

            await dbContext.DocumentDirectories.AddAsync(documentDirectory, cancellationToken);

            await progress.ReportAsync(this, "Criando cópias dos recursos...");
            CopyResourceFiles(documentDirectory);
        }

        await progress.ReportAsync(this, "Persistindo informações...");

        await dbContext.SaveChangesAsync(cancellationToken).SkipContextSync();

        await progress.ReportAsync(this, "Finalizado!");
    }

    private static DocumentDirectory[] CreateDocumentDirectories(IApplicationContext applicationContext) {
        var sampleDocsDir = Path.Combine(applicationContext.DataDirectoryPath, "sample_docs");

        var loremIpsumDir = Path.Combine(sampleDocsDir, "lorem_ipsum");
        Directory.CreateDirectory(loremIpsumDir);

        var poemsDir = Path.Combine(sampleDocsDir, "poems");
        Directory.CreateDirectory(poemsDir);

        var proverbsDir = Path.Combine(sampleDocsDir, "proverbs");
        Directory.CreateDirectory(proverbsDir);

        var storiesDir = Path.Combine(sampleDocsDir, "stories");
        Directory.CreateDirectory(storiesDir);

        var textsDir = Path.Combine(sampleDocsDir, "texts");
        Directory.CreateDirectory(textsDir);

        return [
            new DocumentDirectory
            {
                ID = Guid.Parse("e6fbd900-bec6-45e0-ba16-557c9cd8cbe5"),
                Label = "Lorem Ipsum",
                DirectoryPath = loremIpsumDir,
                DisplayPosition =  1,
                CreatedAt = DateTime.Now,
                ModifiedAt = null
            },
            new DocumentDirectory
            {
                ID = Guid.Parse("93961c74-33c2-436f-a3b8-dbae6b644ad7"),
                Label = "Beautiful Poems",
                DirectoryPath = poemsDir,
                DisplayPosition =  2,
                CreatedAt = DateTime.Now,
                ModifiedAt = null
            },
            new DocumentDirectory
            {
                ID = Guid.Parse("65acab60-0075-41e7-a0e7-494b05209f3e"),
                Label = "Thoughtful Proverbs",
                DirectoryPath = proverbsDir,
                DisplayPosition =  3,
                CreatedAt = DateTime.Now,
                ModifiedAt = null
            },
            new DocumentDirectory
            {
                ID = Guid.Parse("7a09d54c-0ce1-4cd1-8582-a1ecc28f6ad2"),
                Label = "Interesting Stories",
                DirectoryPath = storiesDir,
                DisplayPosition =  4,
                CreatedAt = DateTime.Now,
                ModifiedAt = null
            },
            new DocumentDirectory
            {
                ID = Guid.Parse("faf1cde5-319d-4d4c-a87d-5d297020db0f"),
                Label = "Random Texts",
                DirectoryPath = textsDir,
                DisplayPosition =  5,
                CreatedAt = DateTime.Now,
                ModifiedAt = null
            }
        ];
    }

    private static void CopyResourceFiles(DocumentDirectory documentDirectory) {
        var documentDirectoryFolderName = Path.GetFileName(documentDirectory.DirectoryPath);
        var documentDirectoryResourcesFolderPath = Path.Combine(
            ResourcesFolderPath,
            documentDirectoryFolderName
        );

        foreach (var file in Directory.GetFiles(documentDirectoryResourcesFolderPath, "*.*", SearchOption.AllDirectories)) {
            var fileName = Path.GetFileName(file);

            File.Copy(file, Path.Combine(documentDirectory.DirectoryPath, fileName), overwrite: true);
        }
    }
}
