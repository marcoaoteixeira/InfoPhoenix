using System.Text.Json.Serialization;

namespace Nameless.InfoPhoenix.Configuration;

[JsonConverter(typeof(JsonStringEnumConverter<SearchHistoryLimit>))]
public enum SearchHistoryLimit {
    None = 0,

    Small = 25,

    Medium = 50,

    Large = 100,
}