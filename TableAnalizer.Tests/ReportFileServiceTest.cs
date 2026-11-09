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

        // Определение количества необходимых файлов с постоянными данными
        int constantDataTypeCount = reportFileService.PrimaryReportFiles
                .Count(p => p.Value.DataType is PrimaryDataType.Constant && !p.Value.IsOptionalReport);
        Assert.Equal(4, constantDataTypeCount);


    }
}