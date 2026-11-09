using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TableAnalizer.Configuration;
using Xunit.Abstractions;

namespace TableAnalizer.Tests;

public class HostTest(HostFixture fixture, ITestOutputHelper output) : BaseTest(fixture, output)
{
    [Fact]
    public void ConfigOptionsTest()
    {
        ProgramOptions? options = _host.Services
            .GetRequiredService<IOptions<ProgramOptions>>()
            .Value;

        Assert.NotNull(options);
        Assert.Equal(options.BaseFinalReportsDirectoryPath, BaseFinalReportsDirectoryPathTest);
        Assert.Equal(options.ConstantDataReportsPath, ConstantDataReportsPathTest);

        _host.Dispose();
    }
}