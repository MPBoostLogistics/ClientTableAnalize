using System.Text.RegularExpressions;
using TableAnalizer.Utils;

namespace TableAnalizer.Objects.ReportItems
{
    /// <summary>
    /// Сущность для файла с первичнымм данными;
    /// </summary>
    public class PrimaryReportFileItem(PrimaryDataType primaryDataType, List<string> regexPatterns, int bestBeforeDate, bool isOptionalReport = true)
    {
        #region Properties
        /// <summary>
        /// Постоянные или переменные данные
        /// </summary>
        public PrimaryDataType DataType { get; } = primaryDataType;
        /// <summary>
        /// Информация о файле, содержащем данные.
        /// </summary>
        public FileInfo? Info { get; private set; } = null;
        /// <summary>
        /// Регулярка для сопоставления имени.
        /// </summary>
        public Regex[] RegexesForFileName { get; } = regexPatterns.CreateRegexArray();
        /// <summary>
        /// Дата окончания действия данных.
        /// </summary>
        public int BestBeforeDate { get; } = bestBeforeDate;
        /// <summary>
        /// Флаг обязательности отчета.
        /// </summary>
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