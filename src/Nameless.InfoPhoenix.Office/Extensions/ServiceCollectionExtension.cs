using Microsoft.Extensions.DependencyInjection;
using Nameless.InfoPhoenix.Office.Impl;
using Nameless.InfoPhoenix.Office.Text;
using Nameless.InfoPhoenix.Text;

namespace Nameless.InfoPhoenix.Office {
    public static class ServiceCollectionExtension {
        #region Public Static Methods

        public static IServiceCollection RegisterOffice(this IServiceCollection self)
            => self.AddSingleton<IWordApplication, WordApplication>();

        public static IServiceCollection RegisterXPSDocumentConverter(this IServiceCollection self)
            => self.AddSingleton<InfoPhoenix.Text.IDocumentConverter, XPSDocumentConverter>();

        public static IServiceCollection RegisterWordDocumentReader(this IServiceCollection self)
            => self.AddSingleton<IDocumentReader, WordDocumentReader>();

        #endregion
    }
}
