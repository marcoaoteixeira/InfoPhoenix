using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nameless.Infrastructure;
using Nameless.Lucene;
using Nameless.Lucene.Impl;
using Nameless.Lucene.Options;

namespace Nameless.InfoPhoenix.Domain {
    public static class ServiceCollectionExtension {
        #region Private Constants

        private const string ANALYZER_PROVIDER_TOKEN = $"{nameof(AnalyzerProvider)}::6b375c0e-bffc-4d98-9e3d-3692216e6b15";

        #endregion

        #region Public Static Methods

        public static IServiceCollection RegisterAppDbContext(this IServiceCollection self) {
            self
                .AddDbContext<AppDbContext>((serviceProvider, dbContextOptionsBuilder) => {
                    var applicationContext = serviceProvider.GetRequiredService<IApplicationContext>();
                    var databasePath = Path.Combine(applicationContext.ApplicationDataFolderPath, "database.db");
                    var connectionString = $"Data Source={databasePath}";

                    dbContextOptionsBuilder.UseSqlite(connectionString);
                });

            return self;
        }

        public static IServiceCollection RegisterLucene(this IServiceCollection self)
            => self
                   .AddKeyedSingleton<IAnalyzerProvider>(ANALYZER_PROVIDER_TOKEN, new AnalyzerProvider([]))
                   .AddSingleton(IndexProviderResolver);

        #endregion

        #region Private Static Methods

        private static IIndexManager IndexProviderResolver(IServiceProvider provider) {
            var applicationContext = provider.GetRequiredService<IApplicationContext>();
            var analyzerProvider = provider.GetRequiredKeyedService<IAnalyzerProvider>(ANALYZER_PROVIDER_TOKEN);
            var options = provider.GetPocoOptions<LuceneOptions>();
            var logger = provider.GetLogger<Lucene.Impl.Index>();
            var result = new IndexManager(applicationContext, analyzerProvider, logger, options);

            return result;
        }

        #endregion
    }
}
