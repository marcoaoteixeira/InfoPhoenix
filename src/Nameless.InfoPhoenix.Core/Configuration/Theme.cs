using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Nameless.InfoPhoenix.Configuration;

[JsonConverter(typeof(JsonStringEnumConverter<Theme>))]
public enum Theme {
    [Description("Claro")]
    Light,

    [Description("Escuro")]
    Dark,

    [Description("Alto Contraste")]
    HighContrast
}