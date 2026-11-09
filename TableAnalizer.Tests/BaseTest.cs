using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace TableAnalizer.Tests;

public class HostFixture
{
    public IHost Host { get; } = Program.CreateHost([]);
    public void Dispose() => Host.Dispose();
}

public class BaseTest(HostFixture fixture, ITestOutputHelper output) : IClassFixture<HostFixture>
{
    #region Fields
    protected readonly IHost _host = fixture.Host;
    protected readonly ITestOutputHelper _output = output;
    #endregion

    #region Properties
    protected static string BaseFinalReportsDirectoryPathTest { get; } = "/Users/dmitrijsamarcev";
    protected static string ConstantDataReportsPathTest { get; } = "/Users/dmitrijsamarcev/Library/Mobile Documents/com~apple~CloudDocs/MPBoost/Clients/ConstantDataReports";
    #endregion

    #region Methods
    [Fact]
    public void CreateHostTest()
    {
        Assert.NotNull(_host);
    }
    #endregion
}