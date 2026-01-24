using Microsoft.Extensions.Logging.Abstractions;
using Nameless.Testing.Tools;
using Nameless.Testing.Tools.Attributes;

namespace Nameless.InfoPhoenix.Documents;

[UnitTest]
public class XpsDocumentReaderTests {
    [Fact]
    public void WhenCallingCanRead_WhenFileIsAXps_ThenReturnsTrue() {
        // arrange
        var logger = NullLogger<XpsDocumentReader>.Instance;
        var sut = new XpsDocumentReader(logger);
        const string FilePath = "test.xps";

        // act
        var actual = sut.CanRead(FilePath);

        // assert
        Assert.True(actual);
    }

    [Fact]
    public void WhenCallingCanRead_WhenFileIsANotAXps_ThenReturnsFalse() {
        // arrange
        var logger = NullLogger<XpsDocumentReader>.Instance;
        var sut = new XpsDocumentReader(logger);
        const string FilePath = "test.xkcd";

        // act
        var actual = sut.CanRead(FilePath);

        // assert
        Assert.False(actual);
    }

    [StaFact]
    public async Task WhenGettingContentAsync_ThenReturnsFileContent() {
        // arrange
        var logger = NullLogger<XpsDocumentReader>.Instance;
        var sut = new XpsDocumentReader(logger);
        var filePath = ResourcesHelper.GetFilePath("sample_docs/proverbs/proverbs.xps");
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
