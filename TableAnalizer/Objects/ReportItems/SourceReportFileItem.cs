using System.Text.RegularExpressions;

namespace TableAnalizer.Objects.ReportItems
{
    public class PrimaryReportFileItem(PrimaryDataType primaryDataType, string regexPattern, int bestBeforeDate, bool isOptionalReport = true)
    {
        #region Properties
        public PrimaryDataType DataType { get; } = primaryDataType;
        public FileInfo? Info { get; private set; } = null;
        public Regex RegexForFileName { get; } = new(regexPattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        public int BestBeforeDate { get; } = bestBeforeDate;
        public bool IsOptionalReport { get; set; } = isOptionalReport;
        #endregion

        #region Functionality
        public void Set(FileInfo fileInfo)
        {
            Info = fileInfo;
        }
        #endregion
    }






    public class SourceReportFileItem
    {
        #region Properties
        public SourceReportItemKey Key { get; }
        public FileInfo Info { get; }
        private DateTime UpdateAt { get; }
        private double AgeDataInDays { get; }
        private bool IsControlAgeOfData { get; }
        #endregion

        #region Constructors
        public SourceReportFileItem(SourceReportItemKey key, FileInfo fileInfo, bool isControlAgeOfData, in int dataAgeLimitInDays)
        {
            Key = key;
            Info = fileInfo;
            UpdateAt = Info.LastWriteTime;
            AgeDataInDays = (DateTime.Now - UpdateAt).TotalDays;

            IsControlAgeOfData = isControlAgeOfData;

            if (IsControlAgeOfData && AgeDataInDays > dataAgeLimitInDays)
            {
                throw new Exception($"\nАge of report data with flag \"{Key}\" is {(int)AgeDataInDays} days, which is greater than set value: {dataAgeLimitInDays}.\nRefresh data and try again.\n");
            }
        }
        #endregion
    }
}