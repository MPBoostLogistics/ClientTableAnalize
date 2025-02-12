using CSharpHelpers.Services;

namespace TableAnalizerTest
{
    public class Program_Test
    {
        #region Variables and constants 
        private const string TEST_SOURCE_PATH_NAME = "TestSourceFiles";
        private const string TEST_TARGET_PATH_NAME = "TestTargetFiles";
        private DirectoryInfo? testSourceDirectory;
        private DirectoryInfo? testTargetDirectory;
        #endregion

        [OneTimeSetUp]
        public void Start() 
        {
            if(FileService.GetProjectDirectory(out DirectoryInfo? directoryInfo) && directoryInfo is not null) 
            {
                var testSoucePath = Path.Combine(directoryInfo.FullName, TEST_SOURCE_PATH_NAME);
                FileService.GetDirectoryInfo(testSoucePath, out testSourceDirectory, true);

                var testTargetPath = Path.Combine(directoryInfo.FullName, TEST_TARGET_PATH_NAME);
                FileService.GetDirectoryInfo(testTargetPath, out testTargetDirectory, true);
            }
        }

        [Test, Order(0)]
        public void GetProjectDirectories_Test() 
        {
            // asserts
            Assert.Multiple(() =>
            {
                Assert.That(testSourceDirectory, Is.Not.Null);
                Assert.That(testTargetDirectory, Is.Not.Null);
            });

        }
    }
}