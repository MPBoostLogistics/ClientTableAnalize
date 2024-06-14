using System.Diagnostics;
using System.Reflection;
using TableAnalizer.Enums;

namespace TableAnalizer.Objects.Managers
{
    public class BaseManager 
    {
        #region Variables and constants 
        public const string EXCEL_FILE_EXTENSION = ".xlsx";
        #endregion Variables and constant

        #region Properties 
        public string? ClientLogin { get; protected set; } = string.Empty;

        /// <summary>
        /// Сollection of colors to indicate status of mutual settlements with clients.
        /// </summary>
        /// <remarks><i>New version</i></remarks>
        public Dictionary<SKUSellmentStatus, System.Drawing.Color> Colors { get; } = new()
        {
            {SKUSellmentStatus.FullSet, System.Drawing.ColorTranslator.FromHtml("#98fb98")},
            {SKUSellmentStatus.InWork, System.Drawing.ColorTranslator.FromHtml("#ffd700")},
            {SKUSellmentStatus.OnSearchWarehouseAndPoints, System.Drawing.ColorTranslator.FromHtml("#f08080")},
            {SKUSellmentStatus.OnSearchWarehouse, System.Drawing.ColorTranslator.FromHtml("#ffa07a")},
            {SKUSellmentStatus.OnSearchPoint, System.Drawing.ColorTranslator.FromHtml("#ffc4ad")},
            {SKUSellmentStatus.Unknown, System.Drawing.ColorTranslator.FromHtml("#add8e6")}
        };
        #endregion Properties

        #region Events
        /// <summary>
        /// Manager event delegate.
        /// </summary>
        /// <remarks><i>New version</i></remarks>
        public event EventHandler<ManagerReachedEventArgs>? ManagerReached;

        /// <summary>
        /// Manager event handler.
        /// </summary>
        /// <param name="message">Event message.</param>
        /// <remarks><i>New version</i></remarks>
        public delegate void ManagerHandler(string message);
        #endregion Events

        #region Constuctor and destructors
        protected BaseManager() { }
        #endregion

        #region Functionality
        /// <summary>
        /// Raises a manager event with arguments.
        /// </summary>
        /// <param name="e">Manager event arguments.</param>
        /// <remarks><i>New version</i></remarks>
        protected virtual void OnManagerReached(ManagerReachedEventArgs e) =>
            ManagerReached?.Invoke(this, e);

        /// <summary>
        /// Stops a launched Stopwatch and returns its execution time in seconds.
        /// </summary>
        /// <param name="stopwatch">Launched Stopwatch.</param>
        /// <returns>Stopwatch execution time in seconds.</returns>
        /// <remarks><i>New version</i></remarks>
        protected static double StopFuctionExecuting(ref Stopwatch stopwatch) 
        {
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds / 1000;
        }
        #endregion Functionality

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

        public bool TryGetPropertyValue<TType, TObj>(TObj obj, string propertyName, out TType? value)
        {
            ManagerReachedEventArgs args;    

            value = default;

            if (obj is null)
            {
                args = new ("TryGetPropertyValue: object is null.", 1000, true);
                this.OnManagerReached(args);
                return false;
            }

            PropertyInfo? propertyInfo = typeof(TObj).GetProperty(propertyName);
            if (propertyInfo is null)
            {
                args = new ($"TryGetPropertyValue: Property '{propertyName}' not found.", 1000, true);
                this.OnManagerReached(args);
                return false;
            }

            object? propertyValue = propertyInfo.GetValue(obj);
            if (propertyValue is null && Nullable.GetUnderlyingType(typeof(TType)) is not null)
            {
                return true;
            }

            if (propertyValue is not TType typedValue)
            {
                args = new ($"Property '{propertyName}' is of type {propertyInfo.PropertyType}, got {typeof(TType)}.", 1000, true);
                this.OnManagerReached(args);
                return false;
            }

            value = typedValue;

            return true;
        }

        
    }
}