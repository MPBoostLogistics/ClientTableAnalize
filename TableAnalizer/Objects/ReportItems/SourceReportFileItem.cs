namespace TableAnalizer.Objects.ReportItems
{
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

            if(IsControlAgeOfData && AgeDataInDays > dataAgeLimitInDays) 
            {
                throw new Exception($"\nАge of report data with flag \"{Key}\" is {(int)AgeDataInDays} days, which is greater than set value: {dataAgeLimitInDays}.\nRefresh data and try again.\n");
            }
        }
        #endregion
    }
}