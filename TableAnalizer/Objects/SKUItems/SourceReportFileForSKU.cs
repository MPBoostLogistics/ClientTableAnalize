using System.Text.RegularExpressions;

namespace TableAnalizer.Objects.SKUItems
{
    public class SourceReportFileForSKU
    {
        #region Constants and variables
        public const string SPACE_SEPARATOR = "\u0020";
        public const string LOW_LINE_SEPARATOR = "_";
        const string comaSeparator = "\u002E";
        const string ransomsKey = "отчет";
        const string writeOffShortagesKey = "списание";
        const string shippedKey = "расходы";
        const string transferKey = "перемещения";
        const string additionalKey = "дополнительный";
        const string clientReportlKey = "clientreport";
        const string wbKey = "(wb)";
        const string ozKey = "(ozon)";
        #endregion 

        #region Properties
        public SourceSKUReportFileKey? ReportKey { get; }
        public FileInfo FileInfo { get; }
        public string? Login { get; private set; }

        // Regex
        private const string BALANCE_REPORT_REGEX_PATTERN  = @"\d+\s+\bбаланс\b";
        private static readonly Regex balanceReportRegex = new(BALANCE_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1));
        private const string ADDITIONAL_REPORT_REGEX_PATTERN  = @"\d+\s+\bдополнительный\b";
        private static readonly Regex additionalReportRegex = new(ADDITIONAL_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1));
        private const string SHIPPED_REPORT_REGEX_PATTERN  = @"\d+\s+\bрасходы\b";
        private static readonly Regex shippedReportRegex = new(SHIPPED_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1));
        private const string TRANSFER_REPORT_REGEX_PATTERN  = @"\d+\s+\bперемещения\b";
        private static readonly Regex transferReportRegex = new(TRANSFER_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1));

        #endregion

        #region Constructors
        public SourceReportFileForSKU(FileInfo fileInfo, bool isMulty = false)
        {
            FileInfo = fileInfo;

            var fileName = Path.GetFileName(fileInfo.Name);

            if(fileName.StartsWith("~$")) 
            {
                fileName = fileName.TrimStart('~','$');
            }

            var balanceReportMatch = balanceReportRegex.Match(fileName.Trim());
            var additionalReportMatch = additionalReportRegex.Match(fileName.Trim());
            var shippedReportMatch = shippedReportRegex.Match(fileName.Trim());
            var transferReportMatch = transferReportRegex.Match(fileName.Trim());

            if(balanceReportMatch is not null && balanceReportMatch.Success) 
            {
                ReportKey = SourceSKUReportFileKey.Balance;
            } 
            else if(additionalReportMatch is not null && additionalReportMatch.Success) 
            {
                ReportKey = SourceSKUReportFileKey.Additional;
            }
            else if(shippedReportMatch is not null && shippedReportMatch.Success) 
            {
                ReportKey = SourceSKUReportFileKey.Shipped;
            }
            else if(transferReportMatch is not null && transferReportMatch.Success) 
            {
                ReportKey = SourceSKUReportFileKey.Transfered;
            }
            else 
            {
                if (SplitStringIntoSubstrings(fileName, [SPACE_SEPARATOR, LOW_LINE_SEPARATOR], out string[]? substrings) &&
                    substrings is not null)
                {
                    // Общее списание 230124.xlsx                    3
                    // 89003000879 перемещения 190724.xlsx           3
                    // ClientReport 79687978888 03-12-2024           3

                    // Отчет по клиенту +79164372997 (Ozon).xlsx     5

                    int keyIndex;
                    switch (substrings.Length)
                    {
                        case 3: case 6:
                            switch(isMulty) 
                            {
                                case true:
                                    keyIndex = 0;
                                    break;
                                case false:
                                    keyIndex = 1;
                                    break;
                            }
                            break;
                        default:
                            keyIndex = 0;
                            break;
                    }

                    string keyString  = substrings[keyIndex].Trim().ToLower();

                    switch (keyString)
                    {
                        // списание
                        case writeOffShortagesKey:
                            ReportKey = SourceSKUReportFileKey.WriteOffShortages;
                            break;
                            
                        //  выкупы
                        case ransomsKey:
                            if (SplitStringIntoSubstrings(substrings.Last(), [comaSeparator], out string[]? mpStrings) &&
                                mpStrings != null)
                            {
                                var mpString = mpStrings.First().Trim().ToLower();
                                switch (mpString) 
                                {
                                    case wbKey:
                                        // Отчет по клиенту +79164372997 (WB).xlsx
                                        ReportKey = SourceSKUReportFileKey.RansomsWB;
                                        break;
                                    case ozKey:
                                        // Отчет по клиенту +79164372997 (Ozon).xlsx
                                        ReportKey = SourceSKUReportFileKey.RansomsOZ;
                                        break;
                                    default:
                                        break;    
                                }
                            }

                            if (Login is null && substrings[3] != null) 
                                SaveСlientLogin(substrings[3]);
                            break;
                            
                        // клиентский отчет
                        case clientReportlKey:
                            ReportKey = SourceSKUReportFileKey.Client;

                            if (Login is null && substrings[1] != null) 
                                SaveСlientLogin(substrings[1]);

                            break;

                        default:
                            break;
                    }
                }

                if (!ReportKey.HasValue)
                    throw new ArgumentException(nameof(ReportKey), $"Cannot determine match key for report file at path\n{fileName}.");
            }
        }

        /// <summary>
        /// Splits a string into substrings based on separators.
        /// </summary>
        /// <param name="sourceString">Source string.</param>
        /// <param name="separators">Array of string separators.</param>
        /// <param name="substrings">An array of substrings derived from source string.</param>
        /// <returns>Logical flag for performing an operation.</returns>
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

        /// <summary>
        /// Saves client login.
        /// </summary>
        /// <param name="loginString">A string containing client's login.</param>
        /// <remarks><i>New version, tested</i></remarks>
        public void SaveСlientLogin(string loginString)
        {
            var loginException = new Exception($"Client login cannot be determined and saved.");

            var loginChars = loginString.Trim().ToList();

            if (loginChars is null || loginChars.Count == 0) throw loginException;

            // Clear non-digits in login
            for (int i = 0; i < loginChars.Count; i++)
                if (!int.TryParse(loginChars[i].ToString(), out _)) loginChars.RemoveAt(i);

            Login = new string(loginChars.ToArray());
        }

        #endregion
    }
}