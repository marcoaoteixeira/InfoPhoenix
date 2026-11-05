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

    /// <summary>
    /// Every indexer read, in call order (plan IPX-0010 §6) - lets a test assert that a given
    /// key was actually requested with the expected interpolation arguments (for example the
    /// restore overwrite warning's file name), additive to the bare key-echoing behavior below.
    /// </summary>
    public List<(string Key, object[] Args)> Requests { get; } = [];

    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.InvariantCulture;

    public string this[string key] {
        get {
            Requests.Add((key, []));

            return key;
        }
    }

    public string this[string key, object[] args] {
        get {
            Requests.Add((key, args));

            return key;
        }
    }

    public void SetCulture(CultureInfo culture) {
        CurrentCulture = culture;
        SetCultureCalls.Add(culture.Name);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public void SetCulture(string isoCode) {
        SetCulture(CultureInfo.GetCultureInfo(isoCode));
    }
}
