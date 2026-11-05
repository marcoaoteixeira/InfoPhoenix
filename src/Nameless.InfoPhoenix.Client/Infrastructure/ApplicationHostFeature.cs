using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nameless.Application;
using Nameless.EntityFrameworkCore;
using Nameless.Hosting;
using Nameless.InfoPhoenix.Client.Data;
using Nameless.Logging.Serilog;
using Nameless.Windows;
using Nameless.Windows.Localization;
using Serilog;
using Serilog.Formatting.Compact;

namespace Nameless.InfoPhoenix.Client.Infrastructure;

internal static class ApplicationHostFeature {
    public static void Configure(HostFeatureCollection features) {
        ConfigureTranslatorHostFeature(features);
        ConfigureEntityFrameworkCoreHostFeature(features);
        ConfigureLoggingFeature(features);
    }

    private static void ConfigureTranslatorHostFeature(HostFeatureCollection features) {
        features.Get<TranslatorHostFeature>().Configure(opts => opts.WithTranslator<ResourceTranslator>());
    }

    private static void ConfigureEntityFrameworkCoreHostFeature(HostFeatureCollection features) {
        var feature = new EntityFrameworkCoreHostFeature<AppDbContext>();

        feature.Configure(opts => opts.WithDbContextConfiguration((provider, ctx) => {
            var applicationContext = provider.GetRequiredService<IApplicationContext>();
            var databaseFile = applicationContext.FileManager.GetFile(
                relativePath: SysPath.Combine(
                    applicationContext.FileManager.GetDatabaseDirectory().Name,
                    Constants.Database.FileName
                )
            );
            var connStr = string.Format(Constants.Database.ConnStrPattern, databaseFile.Path);

            ctx.UseSqlite(connStr);
        }));

        features.Add(feature);
    }

    private static void ConfigureLoggingFeature(HostFeatureCollection feature) {
        feature.Get<SerilogHostFeature>().Configure(opts => opts.WithSinkConfiguration((_, sink) => sink.File(
            path: SysPath.Combine(Constants.Logging.DirectoryPath, Constants.Logging.FileNamePattern),
            rollingInterval: RollingInterval.Day,
            fileSizeLimitBytes: 16_777_216,
            retainedFileCountLimit: 3,
            formatter: new CompactJsonFormatter()
        )));
    }
}
