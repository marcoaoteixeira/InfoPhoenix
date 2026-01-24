using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Nameless.InfoPhoenix.Office;

public static class ServiceCollectionExtensions {
    extension(IServiceCollection self) {
        public IServiceCollection RegisterOffice() {
            self.TryAddSingleton<IWordApplication, WordApplication>();

            return self;
        }
    }
}
