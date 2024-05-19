using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nameless.InfoPhoenix.Infrastructure {
    public sealed class HostFactory {
        #region Private Read-Only Fields

        private readonly string[] _args;

        #endregion

        #region Private Fields

        private Action<IServiceCollection> _configureServices;

        #endregion

        #region Private Constructors

        private HostFactory(string[] args) {
            _args = args;
            _configureServices = _ => { };
        }

        #endregion

        #region Public Static Methods

        public static HostFactory Create(params string[] args) => new(args);

        #endregion

        #region Public Methods

        public HostFactory SetConfigureServices(Action<IServiceCollection> configureServices) {
            _configureServices = Guard.Against.Null(configureServices, nameof(configureServices));

            return this;
        }

        public IHost Build()
            => Host
               .CreateDefaultBuilder(_args)
               .ConfigureHostConfiguration(builder
                    => builder.AddJsonFile(path: "AppSettings.json",
                                           optional: true,
                                           reloadOnChange: true))
               .ConfigureServices(_configureServices)
               .Build();

        #endregion
    }
}
