using System.Diagnostics;
using CSharpHelpers.Services;

namespace TableAnalizerTest
{
    public class Base_Test
    {
        #region Variables and constants 
        private const string TEST_SOURCE_PATH_SINGLE_NAME = "TestSingleClientSourceFiles";
        private const string TEST_TARGET_PATH_NAME = "TestSingleClientTargetFiles";
        internal DirectoryInfo? testSingleClientSourceDirectory;
        internal DirectoryInfo? testSingleClientTargetDirectory;

        private bool isMulty; 
        private int deadlineDays;
        #endregion

        #region Functionality
        [OneTimeSetUp]
        public virtual void Start() 
        {
            isMulty = false;
            deadlineDays = 14;

            if(FileService.GetProjectDirectory(out DirectoryInfo? directoryInfo) && directoryInfo is not null) 
            {
                var testSoucePath = Path.Combine(directoryInfo.FullName, TEST_SOURCE_PATH_SINGLE_NAME);
                FileService.GetDirectoryInfo(testSoucePath, out testSingleClientSourceDirectory, true);

                var testTargetPath = Path.Combine(directoryInfo.FullName, TEST_TARGET_PATH_NAME);
                FileService.GetDirectoryInfo(testTargetPath, out testSingleClientTargetDirectory, true);
            }

            isMulty = false;
            deadlineDays = 21;

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
        #endregion
        
    }
}