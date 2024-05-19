using System.IO.Compression;
using MediatR;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Request;
using Nameless.InfoPhoenix.Responses;
using Nameless.Infrastructure;

namespace Nameless.InfoPhoenix.Handlers {
    public sealed class PerformDatabaseBackupRequestHandler : IRequestHandler<PerformDatabaseBackupRequest, EmptyResponse> {
        #region Private Read-Only Fields

        private readonly IApplicationContext _applicationContext;
        private readonly ILogger _logger;

        #endregion

        #region Public Constructors

        public PerformDatabaseBackupRequestHandler(IApplicationContext applicationContext, ILogger<PerformDatabaseBackupRequestHandler> logger) {
            _applicationContext = Guard.Against.Null(applicationContext, nameof(applicationContext));
            _logger = Guard.Against.Null(logger, nameof(logger));
        }

        #endregion

        #region IRequestHandler<PerformDatabaseBackupRequest, EmptyResponse> Members

        public async Task<EmptyResponse> Handle(PerformDatabaseBackupRequest request, CancellationToken cancellationToken) {
            var backupFolder = Path.Combine(_applicationContext.ApplicationDataFolderPath,
                                            Root.Names.BACKUP_FOLDER);
            var backupFilePath = Path.Combine(_applicationContext.ApplicationDataFolderPath,
                                              Root.Names.BACKUP_FOLDER,
                                              $"{DateTime.Now:yyyyMMddHHmmss}.bkp");
            var databaseFilePath = Path.Combine(_applicationContext.ApplicationDataFolderPath,
                                                Root.Names.DATABASE);

            try {
                Directory.CreateDirectory(backupFolder);
                File.Copy(databaseFilePath, backupFilePath, overwrite: true);

                await using (var backupFileStream = File.Open(backupFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
                await using (var compressFileStream = File.Create($"{backupFilePath}.gz"))
                await using (var gzipStream = new GZipStream(compressFileStream, CompressionLevel.Optimal)) {
                    await backupFileStream.CopyToAsync(gzipStream, cancellationToken);
                }

                File.Delete(backupFilePath);

                return new EmptyResponse();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error while creating database backup file.");
                return new EmptyResponse {
                    Error = "Erro ao concluir o backup da base de dados. Veja o log da aplicação para mais detalhes."
                };
            }
        } 

        #endregion
    }
}
