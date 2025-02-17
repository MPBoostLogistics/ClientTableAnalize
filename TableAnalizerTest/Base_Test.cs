using System.Diagnostics;
using CSharpHelpers.Services;
using Microsoft.Extensions.Configuration;

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
            deadlineDays = settingsSessions.GetValue<int>("DefaultDeadlineDays");

            if(FileService.GetProjectDirectory(out DirectoryInfo? directoryInfo) && directoryInfo is not null) 
            { 
                var testSoucePath = Path.Combine(directoryInfo.FullName, TestSourcePathSingleName);
                FileService.GetDirectoryInfo(testSoucePath, out testSingleClientSourceDirectory, true);

                var testTargetPath = Path.Combine(directoryInfo.FullName, TestTargetPathSingleName);
                FileService.GetDirectoryInfo(testTargetPath, out testSingleClientTargetDirectory, true);
            }

            Trace.Listeners.Add(new ConsoleTraceListener());
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
        
        internal void BuildConfigTest() 
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