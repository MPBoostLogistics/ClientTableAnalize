using System.Diagnostics;
using CSharpHelpers.Services;
using Microsoft.Extensions.Configuration;

namespace TableAnalizerTest
{
    public class Base_Test
    {
        // #region Fields
        // internal IConfigurationRoot? testConfig;
        // private string? TestSourcePathSingleName;
        // private string? TestTargetPathSingleName;
        // internal DirectoryInfo? testSingleClientSourceDirectory;
        // internal DirectoryInfo? testSingleClientTargetDirectory;

        // internal ReportFileManager reportFileManager;   

        // internal int reportSourceFilesForSingleClientSum;
        // internal ExcelManager excelManager; 

        // internal FileInfo[] testSingleClientFileInfos = []; 

        // internal string? testSingleClientLogin;
 
        // private int deadlineDays;
        // #endregion

        // #region Functionality
        // [OneTimeSetUp]
        // public virtual void Start() 
        // {
        //     BuildConfigTest();

        //      IConfigurationSection? stringsSessions = testConfig!
        //         .GetSection("Strings");
        //     testSingleClientLogin = stringsSessions
        //         .GetValue<string>("TestSingleClientLogin");

        //     IConfigurationSection? settingsSessions = testConfig!
        //         .GetSection("Settings");
        //     TestSourcePathSingleName = settingsSessions
        //         .GetValue<string>("TestSourcePathSingleName");
        //     TestTargetPathSingleName = settingsSessions
        //         .GetValue<string>("TestTargetPathSingleName");

        //     IConfigurationSection? numberSessions = testConfig!
        //         .GetSection("Numbers");
        //     deadlineDays = numberSessions
        //         .GetValue<int>("defaultDeadlineDays");
        //     reportSourceFilesForSingleClientSum = numberSessions
        //         .GetValue<int>("reportSourceFilesForSingleClientSum");

        //     if(FileService.GetProjectDirectory(out DirectoryInfo? directoryInfo) && directoryInfo is not null) 
        //     { 
        //         var testSoucePath = Path.Combine(directoryInfo.FullName, TestSourcePathSingleName);
        //         FileService.GetDirectoryInfo(testSoucePath, out testSingleClientSourceDirectory, true);

        //         var testTargetPath = Path.Combine(directoryInfo.FullName, TestTargetPathSingleName);
        //         FileService.GetDirectoryInfo(testTargetPath, out testSingleClientTargetDirectory, true);
        //     }

        //     reportFileManager = new(testConfig);
        //     excelManager = new(testConfig);

        //     Trace.Listeners.Add(new ConsoleTraceListener());
        // }

        // [Test, Order(0)]
        // public void GetSourceReportFilesTest() 
        // {

        //     // arrange
        //     FileService.SearchFilesInDirectory(testSingleClientSourceDirectory!.FullName, out testSingleClientFileInfos!, reportFileManager.ValidFileExtensions);

        //     // act
        //     // ...

        //     // assert 
        //     Assert.That(testSingleClientFileInfos, Is.Not.Null);
        //     Assert.That(testSingleClientFileInfos, Has.Length.EqualTo(reportSourceFilesForSingleClientSum));
        // }

        // [Test, Order(1)]
        // public void GetProjectDirectories_Test() 
        // {
        //     // asserts
        //     Assert.Multiple(() =>
        //     {
        //         Assert.That(testSingleClientSourceDirectory, Is.Not.Null);
        //         Assert.That(testSingleClientTargetDirectory, Is.Not.Null);
        //     });
        // }

        // [Test, Order(2)]
        // public async Task GetReportSourceFilesForSingleClient_Test() 
        // {
        //     // arrange
        //     Assert.That(testSingleClientFileInfos, Has.Length.EqualTo(reportSourceFilesForSingleClientSum));

        //     // act 
        //     var getSourceReportFilesTask = reportFileManager.GetSourceReportFiles(testSingleClientFileInfos);
        //     await Task.Run(() => getSourceReportFilesTask);
        //     getSourceReportFilesTask.Wait();

        //     // assert 
        //     Assert.That(reportFileManager.ReportsPaths, Has.Count.EqualTo(reportSourceFilesForSingleClientSum));
        // }

        // [OneTimeTearDown]
        // public virtual void Finish() 
        // {
        //     Trace.Flush();
        // }
        
        // private void BuildConfigTest() 
        // {
        //     if (FileService.GetProjectDirectory(out DirectoryInfo? projectDirectory)
        //             && projectDirectory != null)
        //     {
        //         string? configFileName = "testConfig.json";

        //         if(!string.IsNullOrEmpty(configFileName)) 
        //         {
        //             testConfig = new ConfigurationBuilder()
        //                 .SetBasePath(projectDirectory.FullName)
        //                 //.AddUserSecrets<BaseManager>()
        //                 .AddJsonFile(configFileName, optional: false)
        //                 .Build();
        //         } 
        //         else 
        //         {
        //             throw new Exception("Config json filename is null");
        //         }
        //     }
        // }
        
        // #endregion   
    }
}