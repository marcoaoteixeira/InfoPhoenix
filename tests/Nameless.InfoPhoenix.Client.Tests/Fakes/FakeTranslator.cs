using System.ComponentModel;
using System.Globalization;
using Nameless.Windows.Localization;

namespace Nameless.InfoPhoenix.Client.Tests.Fakes;

/// <summary>
/// Hand-rolled <see cref="ITranslator"/> fake. Records every <see cref="SetCulture(string)"/>
/// call and echoes the key back as the "translated" value, so tests can assert both what was
/// requested and what the indexer returned without needing real resources.
/// </summary>
internal sealed class FakeTranslator : ITranslator {
    public event PropertyChangedEventHandler? PropertyChanged;

    public List<string> SetCultureCalls { get; } = [];

    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.InvariantCulture;

    public string this[string key] => key;

    public string this[string key, object[] args] => key;

    public void SetCulture(CultureInfo culture) {
        CurrentCulture = culture;
        SetCultureCalls.Add(culture.Name);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public void SetCulture(string isoCode) {
        SetCulture(CultureInfo.GetCultureInfo(isoCode));
    }
}
