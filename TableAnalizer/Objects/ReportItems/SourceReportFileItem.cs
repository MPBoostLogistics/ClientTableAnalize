namespace TableAnalizer.Objects.ReportItems
{
    public class SourceReportFileItem
    {
        public SourceReportItemKey Key { get; }
        public FileInfo Info { get; }
        private DateTime CreateAt { get; }
        private double AgeDataInDays { get; }
        private bool IsControlAgeOfData { get; }

        public SourceReportFileItem(SourceReportItemKey key, FileInfo fileInfo, bool isControlAgeOfData, in int dataAgeLimitInDays)
        {
            Key = key;
            Info = fileInfo;
            CreateAt = Info.CreationTime;
            AgeDataInDays = (DateTime.Now - CreateAt).TotalDays;

            IsControlAgeOfData = isControlAgeOfData;

            if(IsControlAgeOfData && AgeDataInDays > dataAgeLimitInDays) 
            {
                throw new Exception($"\nАge of report data with flag \"{Key}\" is {(int)AgeDataInDays} days, which is greater than set value: {dataAgeLimitInDays}.\nRefresh data and try again.\n");
            }

        }
    }
}