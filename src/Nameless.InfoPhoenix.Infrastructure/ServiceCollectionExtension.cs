using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Nameless.Application;
using Nameless.InfoPhoenix.Bootstrap;
using Nameless.InfoPhoenix.Configuration;
using Nameless.InfoPhoenix.Infrastructure.Bootstrap;
using Nameless.InfoPhoenix.Infrastructure.Configuration;
using Nameless.InfoPhoenix.Infrastructure.Telemetry;
using Nameless.InfoPhoenix.Telemetry;
using NLog.Extensions.Logging;

namespace Nameless.InfoPhoenix.Infrastructure;

public static class ServiceCollectionExtension {
    public static IServiceCollection RegisterApplicationContext(this IServiceCollection self, Version appVersion)
        => self.RegisterApplicationContext(useCommonAppDataFolder: false, appVersion);

    public static IServiceCollection RegisterAppConfigurationManager(this IServiceCollection self)
        => self.AddSingleton<IAppConfigurationManager, AppConfigurationManager>();
    
    public static IServiceCollection RegisterBootstrapper(this IServiceCollection self, Assembly[] supportAssemblies) {
        var steps = supportAssemblies.SelectMany(assembly => assembly.SearchForImplementations<IStep>());

        foreach (var step in steps) {
            self.AddTransient(typeof(IStep), step);
        }

        return self.AddTransient<IBootstrapper, Bootstrapper>();
    }

    public static IServiceCollection RegisterFileProvider(this IServiceCollection self)
        => self.AddSingleton<IFileProvider>(provider => {
            var applicationContext = provider.GetRequiredService<IApplicationContext>();

            return new PhysicalFileProvider(applicationContext.AppDataFolderPath);
        });

    public static IServiceCollection RegisterLogging(this IServiceCollection self)
        => self.AddLogging(configure => {
            configure.ClearProviders();
            configure.AddNLog();
        });

    public static IServiceCollection RegisterTelemetryServices(this IServiceCollection self)
        => self.AddSingleton<IPerformanceReporter, PerformanceReporter>();
}