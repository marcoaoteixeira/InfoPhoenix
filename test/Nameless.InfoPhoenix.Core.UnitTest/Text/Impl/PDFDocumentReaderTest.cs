namespace Nameless.InfoPhoenix.Text.Impl {
    [Category(Constants.Categories.RUNS_ON_DEV_MACHINE)]
    public class PDFDocumentReaderTest {
        [Test]
        public void Read_Simple_Pdf_Document() {
            // arrange
            var filePath = typeof(PDFDocumentReaderTest).Assembly.GetDirectoryPath("Resources", "proverbs.pdf");
            var sut = new PDFDocumentReader();

            // act
            var content = sut.GetContent(filePath);

            // assert
            Assert.Multiple(() => {
                Assert.That(content, Is.Not.Empty);
                Assert.That(content.Contains("proverb highlights the importance of taking action"), Is.True);
            });
        }
    }
}