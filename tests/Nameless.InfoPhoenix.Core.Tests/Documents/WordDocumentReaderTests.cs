using Microsoft.Extensions.Logging.Abstractions;
using Nameless.InfoPhoenix.Office;
using Nameless.Testing.Tools;
using Nameless.Testing.Tools.Attributes;
using Nameless.Testing.Tools.Helpers;

namespace Nameless.InfoPhoenix.Documents;

[UnitTest]
public class WordDocumentReaderTests {
    [Theory]
    [InlineData("test.docx")]
    [InlineData("test.doc")]
    [InlineData("test.rtf")]
    public void WhenCallingCanRead_WhenFileIsAWord_ThenReturnsTrue(string filePath) {
        // arrange
        var logger = NullLogger<WordDocumentReader>.Instance;
        var wordApplication = Quick.Mock<IWordApplication>();
        var sut = new WordDocumentReader(wordApplication, logger);

        // act
        var actual = sut.CanRead(filePath);

        // assert
        Assert.True(actual);
    }

    [Fact]
    public void WhenCallingCanRead_WhenFileIsANotAWord_ThenReturnsFalse() {
        // arrange
        var logger = NullLogger<WordDocumentReader>.Instance;
        var wordApplication = Quick.Mock<IWordApplication>();
        var sut = new WordDocumentReader(wordApplication, logger);
        const string FilePath = "test.xkcd";

        // act
        var actual = sut.CanRead(FilePath);

        // assert
        Assert.False(actual);
    }

    [Theory]
    [InlineData("sample_docs/proverbs/proverbs.docx")]
    [InlineData("sample_docs/proverbs/proverbs.doc")]
    [InlineData("sample_docs/proverbs/proverbs.rtf")]
    public async Task WhenGettingContentAsync_ThenReturnsFileContent(string relativeFilePath) {
        // arrange
        var logger = NullLogger<WordDocumentReader>.Instance;
        using var wordApplication = new WordApplication();
        var sut = new WordDocumentReader(wordApplication, logger);
        var filePath = ResourcesHelper.GetFilePath(relativeFilePath);
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
