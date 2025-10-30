using System.Text.RegularExpressions;
using TableAnalizer.Utils;

namespace TableAnalizer.Objects.ReportItems
{
    public class PrimaryReportFileItem(PrimaryDataType primaryDataType, List<string> regexPatterns, int bestBeforeDate, bool isOptionalReport = true)
    {
        #region Properties
        public PrimaryDataType DataType { get; } = primaryDataType;
        public FileInfo? Info { get; private set; } = null;
        public Regex[] RegexesForFileName { get; } = regexPatterns.CreateRegexArray();
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
}