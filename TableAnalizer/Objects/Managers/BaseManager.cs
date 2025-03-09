using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace TableAnalizer.Objects.Managers
{
    public class BaseManager 
    {
        #region Variables and constants 
        public string[] ValidFileExtensions { get; } = [".xls", ".xlsx"];
        internal IConfiguration config
        ;
        #endregion Variables and constant

        #region Properties 
        public List<string> ClientLogins { get; protected set; }

        /// <summary>
        /// Сollection of colors to indicate status of mutual settlements with clients.
        /// </summary>
        /// <remarks><i>New version</i></remarks>
        public Dictionary<SKUSellmentStatus, Color> Colors { get; } = new()
        {
            {SKUSellmentStatus.FullSet, ColorTranslator.FromHtml("#EAF6F5")},                    // #EAF6F5
            {SKUSellmentStatus.InWork, ColorTranslator.FromHtml("#fef4e2")},                     // #fef4e2
            {SKUSellmentStatus.OnSearchWarehouseAndPoints, ColorTranslator.FromHtml("#ffe2d3")}, // #ffe2d3
            {SKUSellmentStatus.OnSearchWarehouse, ColorTranslator.FromHtml("#ffe1d2")},          // #ffe1d2
            {SKUSellmentStatus.OnSearchPoints, ColorTranslator.FromHtml("#ffe7db")},             // #ffe7db
            {SKUSellmentStatus.Unknown, ColorTranslator.FromHtml("#eef5f9")}                     // #eef5f9
        };
        #endregion Properties

        #region Events
        /// <summary>
        /// Manager event delegate.
        /// </summary>
        public event EventHandler<ManagerReachedEventArgs>? ManagerReached;

        /// <summary>
        /// Manager event handler.
        /// </summary>
        /// <param name="message">Event message.</param>
        /// <remarks><i>New version</i></remarks>
        public delegate void ManagerHandler(string message);
        #endregion Events

        #region Constuctor and destructors
        protected BaseManager(IConfiguration config) 
        {
            this.config = config;

            AddConfigVariables();
        }
        #endregion

        #region Functionality
        protected virtual void AddConfigVariables()
        {
            // ...
        }

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

        public bool TryGetPropertyValue<TType, TObj>(TObj obj, string propertyName, out TType? value)
        {
            ManagerReachedEventArgs args;    

            value = default;

            if (obj is null)
            {
                args = new ("TryGetPropertyValue: object is null.", true, true);
                this.OnManagerReached(args);
                return false;
            }

            PropertyInfo? propertyInfo = typeof(TObj).GetProperty(propertyName);
            if (propertyInfo is null)
            {
                args = new ($"TryGetPropertyValue: Property '{propertyName}' not found.", true, true);
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
                args = new ($"Property '{propertyName}' is of type {propertyInfo.PropertyType}, got {typeof(TType)}.", true, true);
                this.OnManagerReached(args);
                return false;
            }

            value = typedValue;

            return true;
        }
        
        private static readonly bool IsRunningFromNUnit = 
            AppDomain.CurrentDomain
                .GetAssemblies()
                .Any(a => a != null &&
                    !string.IsNullOrEmpty(a.FullName) &&
                    a.FullName.ToLowerInvariant().StartsWith("nunit.framework"));
        
        #endregion Functionality
        
    }
}