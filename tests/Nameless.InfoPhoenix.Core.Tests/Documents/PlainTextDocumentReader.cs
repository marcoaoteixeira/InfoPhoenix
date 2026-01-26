using Microsoft.Extensions.Logging.Abstractions;
using Nameless.Testing.Tools;
using Nameless.Testing.Tools.Attributes;
using Nameless.Testing.Tools.Helpers;

namespace Nameless.InfoPhoenix.Documents;

[UnitTest]
public class PlainTextDocumentReaderTests {
    [Fact]
    public void WhenCallingCanRead_WhenFileIsAPlainText_ThenReturnsTrue() {
        // arrange
        var logger = NullLogger<PlainTextDocumentReader>.Instance;
        var sut = new PlainTextDocumentReader(logger);
        const string FilePath = "test.txt";

        // act
        var actual = sut.CanRead(FilePath);

        // assert
        Assert.True(actual);
    }

    [Fact]
    public void WhenCallingCanRead_WhenFileIsANotAPlainText_ThenReturnsFalse() {
        // arrange
        var logger = NullLogger<PlainTextDocumentReader>.Instance;
        var sut = new PlainTextDocumentReader(logger);
        const string FilePath = "test.xkcd";

        // act
        var actual = sut.CanRead(FilePath);

        // assert
        Assert.False(actual);
    }

    [Fact]
    public async Task WhenGettingContentAsync_ThenReturnsFileContent() {
        // arrange
        var logger = NullLogger<PlainTextDocumentReader>.Instance;
        var sut = new PlainTextDocumentReader(logger);
        var filePath = ResourcesHelper.GetFilePath("sample_docs/proverbs/proverbs.txt");
        const string ExpectedContent = "The best time to plant a tree was twenty years ago. The second best time is now.";

        // act
        var actual = await sut.GetContentAsync(filePath, CancellationToken.None);

        // assert
        Assert.Multiple(() => {
            Assert.NotNull(actual);
            Assert.Contains(ExpectedContent, actual);
        });
    }
}
