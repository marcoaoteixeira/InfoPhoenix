using System.Text.Json;
using Nameless.ObjectModel;

namespace Nameless.InfoPhoenix.Testing.Tools;

public static class ErrorExtensions {
    extension(Error self) {
        public string ToJson() {
            return JsonSerializer.Serialize(self);
        }
    }

    extension(Error[] self) {
        public string ToJson() {
            return JsonSerializer.Serialize(self);
        }
    }
}
