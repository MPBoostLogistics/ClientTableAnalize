using System.Diagnostics;

namespace TableAnalizerTest
{
    public class BaseTests
    {
        #region Variables

        //...

        #endregion

        #region Functionality

        [OneTimeSetUp]
        protected virtual void StartTest()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
        }

        [OneTimeTearDown]
        protected virtual void EndTest()
        {
            Trace.Flush();
        }
        #endregion

        // arrange
        // act
        // assert
    }
}