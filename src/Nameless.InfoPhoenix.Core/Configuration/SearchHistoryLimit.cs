using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Nameless.InfoPhoenix.Configuration;

[JsonConverter(typeof(JsonStringEnumConverter<SearchHistoryLimit>))]
public enum SearchHistoryLimit {
    [Description("Não utilizar histórico")]
    None = 0,

    [Description("Pequeno (25 entradas)")]
    Small = 25,

    [Description("Médio (50 entradas)")]
    Medium = 50,

    [Description("Grande (100 entradas)")]
    Large = 100,
}