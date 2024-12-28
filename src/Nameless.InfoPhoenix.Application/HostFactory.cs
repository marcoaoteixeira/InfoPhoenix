using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nameless.InfoPhoenix.Application;

public sealed class HostFactory {
    private readonly string[] _args;

    private Action<IServiceCollection> _configureServices;

    private HostFactory(string[] args) {
        _args = args;
        _configureServices = _ => { };
    }

    public static HostFactory Create(params string[] args) => new(args);

    public HostFactory SetConfigureServices(Action<IServiceCollection> configureServices) {
        _configureServices = Prevent.Argument.Null(configureServices);

        return this;
    }

    public IHost Build()
        => Host.CreateDefaultBuilder(_args)
               .ConfigureHostConfiguration(builder => builder.AddJsonFile(path: "AppSettings.json",
                                                                          optional: true,
                                                                          reloadOnChange: true))
               .ConfigureServices(_configureServices)
               .Build();
}