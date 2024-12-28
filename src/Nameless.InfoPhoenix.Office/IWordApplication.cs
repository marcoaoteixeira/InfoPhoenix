namespace Nameless.InfoPhoenix.Office;

public interface IWordApplication : IDisposable {
    IWordDocument Open(string filePath);
}