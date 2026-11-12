using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TableAnalizer.Configuration;
using TableAnalizer.Objects;
using TableAnalizer.Services;
using Xunit.Abstractions;

namespace TableAnalizer.Tests;

public class ReportFileServiceTest(HostFixture fixture, ITestOutputHelper output) : BaseTest(fixture, output)
{
    [Fact]
    public async Task ReportServiceTypeTest()
    {
        using var scope = _host.Services.CreateScope();
        var reportFileService = scope.ServiceProvider.GetRequiredService<IReportFileService>();

        Assert.IsType<ReportFileService>(reportFileService);
    }

    [Fact]
    public async Task ConstantDataTest()
    {
        // Создание сервиса, получение нужной службы и  опций.
        using var scope = _host.Services.CreateScope();
        var reportFileService = scope.ServiceProvider.GetRequiredService<IReportFileService>();
        ProgramOptions? options = _host.Services
            .GetRequiredService<IOptions<ProgramOptions>>()
            .Value;

        // Путь для поиска директории с постоянными данными предоставлен
        Assert.NotNull(options.ConstantDataReportsPath);
        Assert.Equal(reportFileService.ConstantDataReportsPath, options.ConstantDataReportsPath);

        // Проверка директории с постояннми данными по предоставленному пути
        reportFileService.GetDirectoryOfPrimaryData(null, PrimaryDataType.Constant, out DirectoryInfo? constantDatadirectoryInfo);
        Assert.NotNull(constantDatadirectoryInfo);

        // Получение файлов из директории
        FileInfo[] fileInfos = [];
        reportFileService.GetFilesOfPrimaryData(constantDatadirectoryInfo, ref fileInfos!, [.. options.ValidFilesExtentionsList!]);
        Assert.Equal(4, fileInfos.Length);

        // Проверка предоставленных файлов (имя сооветствует регулярке).
        var isExistensCheck = reportFileService.SearchFilesByDataTypeAndRegex(PrimaryDataType.Constant, fileInfos);
        Assert.True(isExistensCheck);

        // Анализ валидности данных 
        var isValidityCheck = reportFileService.CheckDataValidity(PrimaryDataType.Constant);
        Assert.True(isValidityCheck);
    }
}