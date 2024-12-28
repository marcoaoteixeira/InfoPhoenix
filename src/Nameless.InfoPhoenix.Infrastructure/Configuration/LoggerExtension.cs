using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.Infrastructure.Configuration;

internal static class LoggerExtension {
    private static readonly Action<ILogger<AppConfigurationManager>, Exception?> AppConfigurationMissingPhysicalFilePathDelegate
            = LoggerMessage.Define(logLevel: LogLevel.Warning,
                                   eventId: default,
                                   formatString: "O caminho físico do arquivo de configuração do aplicativo não está acessível.",
                                   options: null);

    internal static void AppConfigurationMissingPhysicalFilePath(this ILogger<AppConfigurationManager> self)
        => AppConfigurationMissingPhysicalFilePathDelegate(self, null /* exception */);
}