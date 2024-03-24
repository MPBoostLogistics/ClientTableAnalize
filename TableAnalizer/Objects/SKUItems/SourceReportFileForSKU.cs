using ClientTableAnalizer.Enums;

namespace ClientTableAnalizer.Objects.SKUItems
{
    public class SourceReportFileForSKU
    {
        #region Constants and variables
        const string spaceSeparator = "\u0020";
        const string comaSeparator = "\u002E";
        const string shortagesKey = "недостачи";
        const string writeOffShortagesKey = "списание";
        const string reGradesKey = "пересорты";
        const string shippedKey = "расходы";
        const string receivedRemainingKey = "приходы";
        const string redemtionKey = "отчет";

        const string wbKey = "(wb)";
        const string ozKey = "(ozon)";
        #endregion 

        #region Properties
        public SourceReportFileKey? ReportKey { get; }
        public FileInfo FileInfo { get; }
        #endregion

        #region Constructors
        public SourceReportFileForSKU(FileInfo fileInfo)
        {
            FileInfo = fileInfo;

            var fileName = Path.GetFileName(fileInfo.Name);
            string keyString = string.Empty;

            if (SplitStringIntoSubstrings(fileName, new string[] { spaceSeparator }, out string[]? substrings) &&
                substrings is not null)
            {
                // Отчет по клиенту +79164372997 (WB).xlsx       5
                // Отчет по клиенту +79164372997 (Ozon).xlsx     5
                // Юсупов недостачи 170124.xlsx                // 3
                // Общее списание 230124.xlsx                  // 3
                // Юсупов пересорты 130324.xlsx                // 3
                // Юсупов расходы 22032024.xlsx                // 3

                // Юсупов приходы и остатки 220324 (WB).xlsx   // 6
                // Юсупов приходы и остатки 220324 (OZ).xlsx   // 6

                // Отчет по клиенту +79164372997 (WB).xlsx     // 5
                // Юсупов приходы и остатки 220324 (OZ).xlsx   // 5

                int keyIndex = substrings.Length == 3 || substrings.Length == 6 ? 1 : 0;
                keyString = substrings[keyIndex].TrimStart().TrimEnd().ToLower();

                switch (keyString)
                {
                    // недостачи
                    case shortagesKey:
                        ReportKey = SourceReportFileKey.Shortages;
                        break;

                    // списание
                    case writeOffShortagesKey:
                        ReportKey = SourceReportFileKey.WriteOffShortages;
                        break;

                    // пересорты
                    case reGradesKey:
                        ReportKey = SourceReportFileKey.ReGrades;
                        break;

                    // расходы
                    case shippedKey:
                        ReportKey = SourceReportFileKey.Shipped;
                        break;

                    // приходы и остатки
                    case receivedRemainingKey:
                        if (SplitStringIntoSubstrings(substrings.Last(), new string [] {comaSeparator}, out string[]? mps) &&
                           mps != null)
                        {
                            var mpString = mps.First().Trim().ToLower();
                            switch (mpString) 
                            {
                                case wbKey:
                                    // Юсупов приходы и остатки 220324 (WB).xlsx
                                    ReportKey = SourceReportFileKey.RemainingWB;
                                    break;
                                case ozKey:
                                    // Юсупов приходы и остатки 220324 (OZ).xlsx
                                    ReportKey = SourceReportFileKey.RemainingOZ;
                                    break;
                                default:
                                    break;    
                            }
                        }
                        break;

                    // отчет 
                    case redemtionKey:
                        if (SplitStringIntoSubstrings(substrings.Last(), new string [] {comaSeparator}, out string[]? mpStrings) &&
                            mpStrings != null)
                        {
                            var mpString = mpStrings.First().Trim().ToLower();
                            switch (mpString) 
                            {
                                case wbKey:
                                    // Отчет по клиенту +79164372997 (WB).xlsx
                                    ReportKey = SourceReportFileKey.RedemtionWB;
                                    break;
                                case ozKey:
                                    // Отчет по клиенту +79164372997 (Ozon).xlsx
                                    ReportKey = SourceReportFileKey.RedemtionOZ;
                                    break;
                                default:
                                    break;    
                            }
                        }
                        break;

                    default:
                        break;
                }
            }

            if (!ReportKey.HasValue)
                throw new Exception($"Cannot determine match key for report file at path\n{fileName} for keystring {keyString}");
        }

        public static bool SplitStringIntoSubstrings(string sourceString, string[] separators, out string[]? substrings)
        {
            substrings = null;

            foreach (var separator in separators)
            {
                if (sourceString.Contains(separator))
                {
                    substrings = sourceString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    break;
                }
            }

            return substrings is not null && substrings.Length > 0;
        }

        #endregion
    }
}