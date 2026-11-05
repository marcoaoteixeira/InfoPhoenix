namespace Nameless.InfoPhoenix.Client.Tests.Fakes;

/// <summary>
/// Hand-rolled <see cref="TimeProvider"/> fake with a fixed <see cref="GetUtcNow"/> (plan
/// IPX-0009 §6: "Do not add Microsoft.Extensions.TimeProvider.Testing for this").
/// </summary>
internal sealed class FakeTimeProvider : TimeProvider {
    public DateTimeOffset UtcNow { get; set; } = new(2026, 11, 5, 14, 30, 12, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() {
        return UtcNow;
    }
}
