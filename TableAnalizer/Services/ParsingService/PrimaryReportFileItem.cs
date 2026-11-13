using System.Text.RegularExpressions;
using TableAnalizer.Utils;

namespace TableAnalizer.Objects.ReportItems
{
    /// <summary>
    /// Сущность для файла с первичнымм данными;
    /// </summary>
    public class PrimaryReportFileItem(PrimaryDataType primaryDataType, List<string> regexPatterns, int dataValidityPeriodInDays, bool isOptionalReport = true)
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
        /// Cрок окончания актуальности данных в днях.
        /// </summary>
        public int DataValidityPeriodInDays { get; } = dataValidityPeriodInDays;
        /// <summary>
        /// Флаг обязательности отчета.
        /// </summary>
        public bool IsOptionalReport { get; set; } = isOptionalReport;
        #endregion

        #region Functionality
        /// <summary>
        /// Записывает объект FileInfo.
        /// </summary>
        /// <param name="fileInfo">Объект FileInfo для отчета. </param>
        public void Set(FileInfo fileInfo)
        {
            Info = fileInfo;
        }

        /// <summary>
        /// Сбрасывает информацию о файле.
        /// Используется перед каждым прогоном поиска, чтобы исключить
        /// использование данных предыдущего клиента.
        /// </summary>
        public void Reset()
        {
            Info = null;
        }

        /// <summary>
        /// Проверяет не истекла ли актуальность предоставляемых данных.
        /// </summary>
        /// <returns></returns>
        public bool IsDataHasNotExpired()
        {
            if (DataValidityPeriodInDays == 0) return true;
            return Info?.LastWriteTime.AddDays(DataValidityPeriodInDays) > DateTime.Now;
        }
        #endregion
    }
}