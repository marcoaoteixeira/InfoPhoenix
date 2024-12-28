namespace Nameless.InfoPhoenix.Bootstrap;

public interface IStep {
    string Name { get; }
    
    int Order { get; }
    
    bool ThrowOnFailure { get; }

    bool Skip { get; }

    Task ExecuteAsync();
}