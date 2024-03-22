using ClientTableAnalizer.Objects.Managers;

namespace TableAnalizerTest.Managers
{
    public class ExcelReportManager_Test
    {
        #region Variables
        private ExcelReportManager _excelManager;

        private readonly string _testFilesPath = "TableAnalizerTest/files";

        private readonly string _correctFileName = "Отчет по клиенту +79164372997 (WB).xlsx";
        
        private readonly string _incorrectFileName = "Отчет по клиенту по клиенту +79164372997 (WB).xlsx";
        #endregion

        #region Functionality

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _excelManager = new();
        }

        [Test, Order(1)]
        public void GetFilesFromDir_Test() 
        {
        
        }


        [Test, Order(2)]
        public void SaveСlientLogin_Test() 
        {
            // arrange
            // ..

            // act 1 
            // ...

            // assert 1 
            //ExcelReportManager.SaveСlientLogin();

        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // great code ...
        }
        #endregion

        // arrange
        // act
        // assert
    }
}