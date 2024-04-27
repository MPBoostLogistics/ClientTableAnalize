namespace ClientTableAnalizer.Objects.Managers
{
    public class BaseManager
    {
        protected const string REPORT_EXCEL_FILE_EXTENSION = ".xlsx";

        #region Properties 
        public string? ClientLogin { get; protected set; } = string.Empty;
        #endregion

        #region
        /// <summary>
        /// Manager event delegate.
        /// </summary>
        public event EventHandler<ManagerReachedEventArgs>? ManagerReached;

        #endregion

        public delegate void ManagerHandler(string message);

        #region Constuctor and destructors
        protected BaseManager(){ }
        #endregion


        // Из старой реалиазции
        private static string FormСorrectPath(string path)
        {
            var copy = path;

            // Clear "\"
            var charArray = copy.Trim().ToList();
            for (int i = 0; i < charArray.Count; i++)
            {
                if (charArray[i] == '\u005c') charArray.RemoveAt(i);
            }

            copy = new string(charArray.ToArray());

            // Add "/"
            return $"/{copy}";
        }

        protected static int? ParseValueToInt(object? obj) =>
            obj is not null && !string.IsNullOrEmpty(obj.ToString()) ?
            int.Parse(obj.ToString()!.Trim()) :
            null;

        /// <summary>
        /// Raises a manager event with arguments.
        /// </summary>
        /// <param name="e">Manager event arguments.</param>ю
        protected virtual void OnManagerReached(ManagerReachedEventArgs e) =>
            ManagerReached?.Invoke(this, e);
    }

}