using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nameless.Application;
using Nameless.InfoPhoenix.Domains.Entities;
using Nameless.MediatR.Integration;
using Nameless.Search.Lucene;
using Nameless.Validation.FluentValidation;

namespace Nameless.InfoPhoenix.Domains;

public static class ServiceCollectionExtension {
    public static IServiceCollection RegisterEntityFramework(this IServiceCollection self)
        => self.AddDbContext<AppDbContext>((serviceProvider, dbContextOptionsBuilder) => {
                   var applicationContext = serviceProvider.GetRequiredService<IApplicationContext>();
                   var databasePath = Path.Combine(applicationContext.AppDataFolderPath, Constants.Common.DatabaseFileName);
                   var connectionString = string.Format(Constants.Common.ConnStrPattern, databasePath);

                   dbContextOptionsBuilder.UseSqlite(connectionString);
               })
               .AddSingleton<IRepository, Repository>();

    public static IServiceCollection RegisterFluentValidator(this IServiceCollection self, Assembly[] supportAssemblies)
        => self.AddValidationService(supportAssemblies);

    public static IServiceCollection RegisterMediator(this IServiceCollection self, Assembly[] supportAssemblies)
        => self.AddMediatR(configure => {
            configure.RegisterServicesFromAssemblies(supportAssemblies);
            configure.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

    public static IServiceCollection RegisterLuceneSearch(this IServiceCollection self)
        => self.AddSingleton<IAnalyzerSelector, InfoPhoenixAnalyzerSelector>()
               .RegisterLuceneSearch(_ => { });
}