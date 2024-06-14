namespace TableAnalizer.Objects.Managers
{
    /// <summary>
    /// Arguments for publishing manager event.
    /// </summary>
    public class ManagerReachedEventArgs : EventArgs
    {
        #region Properties
        public string Message { get; set; }
        public int Delay { get; set; }
        public bool Exit { get; set; }
        #endregion

        #region Constructors
        public ManagerReachedEventArgs(string message, int delay, bool exit)
        {
            Message = message;
            Delay = delay;
            Exit = exit;
        }
        #endregion
    }
}