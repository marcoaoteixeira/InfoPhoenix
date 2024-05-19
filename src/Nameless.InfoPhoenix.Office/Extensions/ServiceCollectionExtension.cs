using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Office.Impl;

namespace Nameless.InfoPhoenix.Office {
    public static class ServiceCollectionExtension {
        #region Private Constants

        private const string WORD_APPLICATION_REG_KEY = $"{nameof(WordApplication)}::362d5577-0bd7-4cbb-a484-4b1b974862c3";

        #endregion

        #region Public Static Methods

        public static IServiceCollection RegisterOfficeSuite(this IServiceCollection self)
            => self
               .AddKeyedSingleton<IWordApplication, WordApplication>(WORD_APPLICATION_REG_KEY)
               .AddSingleton<IOfficeSuite>(provider => {
                   var wordApplication = provider.GetRequiredKeyedService<IWordApplication>(WORD_APPLICATION_REG_KEY);
                   var logger = provider
                       .GetRequiredService<ILoggerFactory>()
                       .CreateLogger<OfficeSuite>();

                   return new OfficeSuite(wordApplication, logger);
               });

        #endregion
    }
}
