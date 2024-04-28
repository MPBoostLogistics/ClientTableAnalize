using ClientTableAnalizer.Enums;

namespace ClientTableAnalizer.Objects.Managers
{
    public class BaseManager
    {
        protected const string REPORT_EXCEL_FILE_EXTENSION = ".xlsx";

        #region Properties 
        public string? ClientLogin { get; protected set; } = string.Empty;

        /// <summary>
        /// Сollection of colors to indicate status of mutual settlements with clients.
        /// </summary>
        /// <remarks><i>New version</i></remarks>
        public Dictionary<SKUSellmentStatus, System.Drawing.Color> Colors {get;} = new () 
        {
            {SKUSellmentStatus.FullSet, System.Drawing.ColorTranslator.FromHtml("#D5F5E3")}, 
            {SKUSellmentStatus.InWork, System.Drawing.ColorTranslator.FromHtml("#FDEBD0")}, 
            {SKUSellmentStatus.OnSearchWarehouseAndPoints, System.Drawing.ColorTranslator.FromHtml("#F5B7B1")},                  
            {SKUSellmentStatus.OnSearchWarehouse, System.Drawing.ColorTranslator.FromHtml("#EBDEF0")},  
            {SKUSellmentStatus.OnSearchPoint, System.Drawing.ColorTranslator.FromHtml("#FDEDEC")},
            {SKUSellmentStatus.Unknown, System.Drawing.ColorTranslator.FromHtml("#E5E8E8")}
        };
        #endregion

        #region
        /// <summary>
        /// Manager event delegate.
        /// </summary>
        /// <remarks><i>New version</i></remarks>
        public event EventHandler<ManagerReachedEventArgs>? ManagerReached;

        #endregion

        /// <summary>
        /// Manager event handler.
        /// </summary>
        /// <param name="message">Event message.</param>
        /// <remarks><i>New version</i></remarks>
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

        /// <summary>
        /// Raises a manager event with arguments.
        /// </summary>
        /// <param name="e">Manager event arguments.</param>
        /// <remarks><i>New version</i></remarks>
        protected virtual void OnManagerReached(ManagerReachedEventArgs e) =>
            ManagerReached?.Invoke(this, e);
    }

}