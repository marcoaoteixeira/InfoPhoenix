using Nameless.InfoPhoenix.Testing.Tools.Mockers.Documents;
using Nameless.Testing.Tools.Attributes;

namespace Nameless.InfoPhoenix.Documents;

[UnitTest]
public class DocumentReaderManagerTests {
    [Fact]
    public async Task WhenGettingContentAsync_WhenThereIsAValidReader_ThenReturnsContent() {
        // arrange
        const string Content = "This is a test content.";
        var documentReader = new DocumentReaderMocker().WithCanRead(true)
                                                       .WithGetContentAsync(Content)
                                                       .Build();
        var sut = new DocumentService([documentReader], []);

        // act
        var actual = await sut.GetContentAsync("test.txt", CancellationToken.None);

        // assert
        Assert.Equal(Content, actual);
    }

    [Fact]
    public async Task WhenGettingContentAsync_WhenThereIsNoAReaderAvailable_ThenReturnsStringEmpty() {
        // arrange
        var sut = new DocumentService([], []);

        // act
        var actual = await sut.GetContentAsync("test.txt", CancellationToken.None);

        // assert
        Assert.Empty(actual);
    }

    [Fact]
    public async Task WhenGettingContentAsync_WhenThereIsAReaderAvailable_WhenFileCanBeReadByReader_ThenReturnsStringEmpty() {
        // arrange
        var documentReader = new DocumentReaderMocker().WithCanRead(false)
                                                       .Build();
        var sut = new DocumentService([documentReader], []);

        // act
        var actual = await sut.GetContentAsync("test.txt", CancellationToken.None);

        // assert
        Assert.Empty(actual);
    }
}
