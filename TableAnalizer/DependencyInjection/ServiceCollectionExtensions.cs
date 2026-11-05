using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TableAnalizer.Application;
using TableAnalizer.Configuration;
using TableAnalizer.Console;
using TableAnalizer.Services;

/// <summary>
/// Регистрация зависомостей.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddTransient<IPrimaryDataWorkflow, PrimaryDataWorkflow>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ProgramOptions>(options =>
        {
            configuration.GetSection("Flags").Bind(options);
            configuration.GetSection("Messages").Bind(options);
            configuration.GetSection("Numbers").Bind(options);
            configuration.GetSection("Formulas").Bind(options);
            configuration.GetSection("Collections").Bind(options);
            configuration.GetSection("Strings").Bind(options);
        });

        services.AddTransient<IParsingService, ParsingService>();
        services.AddTransient<IConsoleService, ConsoleService>();
        services.AddTransient<IReportFileService, ReportFileService>();
        services.AddTransient<IExcelFileService, ExcelFileService>();
        services.AddTransient<ISKUService, SKUService>();
        services.AddTransient<IAnalyticalService, AnalyticalService>();

        return services;
    }
}