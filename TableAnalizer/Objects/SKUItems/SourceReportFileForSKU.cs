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
        const string transferKey = "перемещение";
        const string receivedRemainingKey = "приходы";
        const string additionalKey = "дополнительный";
        const string clientReportlKey = "clientreport";
        const string wbKey = "(wb)";
        const string ozKey = "(ozon)";
        #endregion 

        #region Properties
        public SourceSKUReportFileKey? ReportKey { get; }
        public FileInfo FileInfo { get; }
        public string? Login { get; private set; }
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

            if (SplitStringIntoSubstrings(fileName, [SPACE_SEPARATOR, LOW_LINE_SEPARATOR], out string[]? substrings) &&
                substrings is not null)
            {
                // дополнительный 30012925.xlsx                  2

                // Юсупов расходы 22052024.xlsx                  3
                // Общее списание 230124.xlsx                    3
                // Башорин перемещение 190724.xlsx               3
                // ClientReport 79687978888 03-12-2024           3

                // Отчет по клиенту +79164372997 (WB).xlsx       5
                // Отчет по клиенту +79164372997 (Ozon).xlsx     5
                
                // Юсупов приходы и остатки 220524 (WB).xlsx     6
                // Юсупов приходы и остатки 220524 (Ozon).xlsx   6

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
                    // дополнительный
                    case additionalKey:
                        ReportKey = SourceSKUReportFileKey.Additional;
                        break;

                    // списание
                    case writeOffShortagesKey:
                        ReportKey = SourceSKUReportFileKey.WriteOffShortages;
                        break;

                    // расходы
                    case shippedKey:
                        ReportKey = SourceSKUReportFileKey.Shipped;
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
                                    ReportKey = SourceSKUReportFileKey.RemainingWB;
                                    break;
                                case ozKey:
                                    // Юсупов приходы и остатки 220324 (OZ).xlsx
                                    ReportKey = SourceSKUReportFileKey.RemainingOZ;
                                    break;
                                default:
                                    break;    
                            }
                        }
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

                    // перемещение на фулфилмент
                    case transferKey:
                        ReportKey = SourceSKUReportFileKey.Transfer;
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

                if (!ReportKey.HasValue)
                    throw new Exception($"Cannot determine match key for report file at path\n{fileName} for keystring {keyString}");
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