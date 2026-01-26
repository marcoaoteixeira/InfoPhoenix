using System.IO;
using Microsoft.EntityFrameworkCore;
using Nameless.InfoPhoenix.Data;
using Nameless.InfoPhoenix.Entities;

namespace Nameless.InfoPhoenix.Testing.Tools;

public static class StaticData {
    public static readonly string ResourcesDirectoryPath = Path.Combine(typeof(StaticData).Assembly.GetDirectoryPath(), "Resources");

    public static IEnumerable<DocumentDirectory> GetSampleDocumentDirectories() {
        var sampleDocsDirectoryPath = Path.Combine(ResourcesDirectoryPath, "sample_docs");
        Directory.CreateDirectory(sampleDocsDirectoryPath);

        var directoryPath = string.Empty;

        directoryPath = Path.Combine(sampleDocsDirectoryPath, "lorem_ipsum");
        Directory.CreateDirectory(directoryPath);
        yield return new DocumentDirectory {
            ID = Guid.Parse("a4e67f84-ecdd-4b13-a9fa-5d60f4eb3a94"),
            Label = "Lorem Ipsum",
            DirectoryPath = directoryPath,
            DisplayPosition = 0,
            LastIndexingTime = null
        };

        directoryPath = Path.Combine(sampleDocsDirectoryPath, "poems");
        Directory.CreateDirectory(directoryPath);
        yield return new DocumentDirectory {
            ID = Guid.Parse("2c11f51f-f31f-4040-8c8b-e8848a454f7a"),
            Label = "Poems",
            DirectoryPath = directoryPath,
            DisplayPosition = 1,
            LastIndexingTime = null
        };

        directoryPath = Path.Combine(sampleDocsDirectoryPath, "proverbs");
        Directory.CreateDirectory(directoryPath);
        yield return new DocumentDirectory {
            ID = Guid.Parse("975aa246-bc27-4bb3-b031-2af948c4bfa4"),
            Label = "Proverbs",
            DirectoryPath = directoryPath,
            DisplayPosition = 2,
            LastIndexingTime = null
        };

        directoryPath = Path.Combine(sampleDocsDirectoryPath, "stories");
        Directory.CreateDirectory(directoryPath);
        yield return new DocumentDirectory {
            ID = Guid.Parse("f0834f21-46f4-493c-92f9-e1a3c23d80f1"),
            Label = "Stories",
            DirectoryPath = directoryPath,
            DisplayPosition = 3,
            LastIndexingTime = null
        };

        directoryPath = Path.Combine(sampleDocsDirectoryPath, "texts");
        Directory.CreateDirectory(directoryPath);
        yield return new DocumentDirectory {
            ID = Guid.Parse("fb7f4b5b-0456-489e-8fb9-1c1f3fe8432e"),
            Label = "Texts",
            DirectoryPath = directoryPath,
            DisplayPosition = 4,
            LastIndexingTime = null
        };
    }

    public static void Seeding(AppDbContext dbContext, bool _) {
        if (dbContext.Database.IsRelational()) {
            dbContext.DocumentDirectories
                     .ExecuteDelete();

            dbContext.Documents
                     .ExecuteDelete();
        }

        if (dbContext.Database.IsInMemory()) {
            dbContext.DocumentDirectories
                     .RemoveRange(dbContext.DocumentDirectories);
            dbContext.Documents
                     .RemoveRange(dbContext.Documents);
        }

        var documentDirectories = GetSampleDocumentDirectories();

        dbContext.DocumentDirectories
                 .AddRange(documentDirectories);

        dbContext.SaveChanges();
    }

    public static async Task SeedingAsync(AppDbContext dbContext, bool _, CancellationToken cancellationToken) {
        if (dbContext.Database.IsRelational()) {
            await dbContext.DocumentDirectories
                           .ExecuteDeleteAsync(cancellationToken)
                           .SkipContextSync();

            await dbContext.Documents
                           .ExecuteDeleteAsync(cancellationToken)
                           .SkipContextSync();
        }

        if (dbContext.Database.IsInMemory()) {
            dbContext.DocumentDirectories
                     .RemoveRange(dbContext.DocumentDirectories);
            dbContext.Documents
                     .RemoveRange(dbContext.Documents);
        }

        var documentDirectories = GetSampleDocumentDirectories();

        await dbContext.DocumentDirectories
                       .AddRangeAsync(documentDirectories, cancellationToken)
                       .SkipContextSync();

        await dbContext.SaveChangesAsync(cancellationToken)
                       .SkipContextSync();
    }
}
