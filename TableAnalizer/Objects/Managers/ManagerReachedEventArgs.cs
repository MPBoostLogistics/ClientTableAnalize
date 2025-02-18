namespace TableAnalizer.Objects.Managers
{
    /// <summary>
    /// Arguments for publishing manager event.
    /// </summary>
    public class ManagerReachedEventArgs(string message, bool isError, bool isExit, int traceDelay = 1500) : EventArgs
    {
        #region Properties
        public string Message { get; set; } = message;
        public bool IsError { get; set; } = isError;
        public bool IsExit { get; set; } = isExit;
        public int TraceDelay { get; set; } = traceDelay;
        #endregion
    }
}