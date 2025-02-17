using System.Diagnostics;
using CSharpHelpers.Services;
using Microsoft.Extensions.Configuration;
using TableAnalizer.Objects.Managers;

namespace TableAnalizerTest
{
    public class Base_Test
    {
        #region Variables and constants 
        internal IConfigurationRoot? testConfig;
        private string? TestSourcePathSingleName;
        private string? TestTargetPathSingleName;
        internal DirectoryInfo? testSingleClientSourceDirectory;
        internal DirectoryInfo? testSingleClientTargetDirectory;

        internal ReportFileManager reportFileManager;   

        internal int reportSourceFilesForSingleClientSum;
        internal ExcelManager excelManager; 
        internal string[] validFileExtensions = []; 


        private bool isMulty; 
        private int deadlineDays;
        #endregion

        #region Functionality
        [OneTimeSetUp]
        public virtual void Start() 
        {
            BuildConfigTest();

            IConfigurationSection? settingsSessions = testConfig!.GetSection("Settings");
            TestSourcePathSingleName = settingsSessions.GetValue<string>("TestSourcePathSingleName");
            TestTargetPathSingleName = settingsSessions.GetValue<string>("TestTargetPathSingleName");
            isMulty = settingsSessions.GetValue<bool>("isMulty");

            IConfigurationSection? numberSessions = testConfig!.GetSection("Numbers");
            deadlineDays = numberSessions.GetValue<int>("defaultDeadlineDays");
            reportSourceFilesForSingleClientSum = numberSessions.GetValue<int>("reportSourceFilesForSingleClientSum");

            if(FileService.GetProjectDirectory(out DirectoryInfo? directoryInfo) && directoryInfo is not null) 
            { 
                var testSoucePath = Path.Combine(directoryInfo.FullName, TestSourcePathSingleName);
                FileService.GetDirectoryInfo(testSoucePath, out testSingleClientSourceDirectory, true);

                var testTargetPath = Path.Combine(directoryInfo.FullName, TestTargetPathSingleName);
                FileService.GetDirectoryInfo(testTargetPath, out testSingleClientTargetDirectory, true);
            }

            reportFileManager = new();
            validFileExtensions = reportFileManager.validFileExtensions;

            excelManager = new();

            Trace.Listeners.Add(new ConsoleTraceListener());

            Task<int> getReportFilesTask = 
                reportFileManager.GetSourceReportFiles(testSingleClientSourceDirectory!.FullName, false, validFileExtensions);
            Task.Run(() => getReportFilesTask);
            getReportFilesTask.Wait();
        }

        [Test, Order(0)]
        public void GetProjectDirectories_Test() 
        {
            // asserts
            Assert.Multiple(() =>
            {
                Assert.That(testSingleClientSourceDirectory, Is.Not.Null);
                Assert.That(testSingleClientTargetDirectory, Is.Not.Null);
            });
        }

        [OneTimeTearDown]
        public void Finish() 
        {
            Trace.Flush();
        }
        
        private void BuildConfigTest() 
        {
            if (FileService.GetProjectDirectory(out DirectoryInfo? projectDirectory)
                    && projectDirectory != null)
            {
                string? configFileName = "testConfig.json";

                if(!string.IsNullOrEmpty(configFileName)) 
                {
                    Trace.WriteLine($"\n{configFileName}");

                    testConfig = new ConfigurationBuilder()
                        .SetBasePath(projectDirectory.FullName)
                        //.AddUserSecrets<BaseManager>()
                        .AddJsonFile(configFileName, optional: false)
                        .Build();
                } 
                else 
                {
                    throw new Exception("Config json filename is null");
                }
            }
        }
        
        #endregion
        
    }
}