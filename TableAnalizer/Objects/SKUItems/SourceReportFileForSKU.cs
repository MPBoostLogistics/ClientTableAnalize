namespace TableAnalizer.Objects.SKUItems
{
    public class SourceReportFileForSKU(SourceSKUReportFileKey reportFileKey, FileInfo fileInfo)
    {
        #region Properties
        public SourceSKUReportFileKey? ReportKey { get; } = reportFileKey;
        public FileInfo FileInfo { get; } = fileInfo;
        #endregion
    }
}