using System.Diagnostics;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Interfaces;
using ClientTableAnalizer.Objects.Parsing;
using ClientTableAnalizer.Objects.Repositories;
using ClientTableAnalizer.Objects.SKUItems;
using ClientTableAnalizer.Objects.SKUItems.SKUBalances;
using OfficeOpenXml;

namespace ClientTableAnalizer.Objects.Managers
{
    public class SKUManager : BaseManager
    {
        #region Properties 
        public int DeadlineDays { get; private set; }
        public IRepository<int, SKU> SKUs { get; }
        public IRepository<int, List<ParsingRowEntity>> SkuRansomsBalanceUnits { get; }
        public IRepository<int, List<ParsingRowEntity>> SkuRemainingBalanceUnits { get; }

        /// <summary>
        /// Returns number of SKUs in collection.
        /// </summary>
        /// <returns>Number of SKUs in collection.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public int SKUsCount() => SKUs.Count();

        #endregion

        #region Constructors and destructors
        public SKUManager(ExcelManager excelReportManager)
        {
            SKUs = new SKURepo();
            SkuRansomsBalanceUnits = new ParsingRowEntityRepo();
            SkuRemainingBalanceUnits = new ParsingRowEntityRepo();
        }
        #endregion

        #region Functionality

        /// <summary>
        /// Sets period before deadline when paying a client (in days).
        /// </summary>
        /// <param name="days">Period until deadline, days</param>
        /// <remarks><i>New version, tested</i></remarks>
        public void SetDeadlineDays(int days) => DeadlineDays = days;

        /// <summary>
        /// Processes data from seller ransoms reports.
        /// </summary>
        /// <param name="reportFileKey">Key of source file with report.</param>
        /// <returns>Function execution time in seconds.</returns>
        /// <remarks><i>New version</i></remarks>
        public async Task<double> ReceivingAndProcessingRansomsData(SourceSKUReportFileKey reportFileKey)
        {
            // Start
            Stopwatch stopwatch = Stopwatch.StartNew();

            string workSheetName = reportFileKey switch
            {
                SourceSKUReportFileKey.RansomsWB or SourceSKUReportFileKey.RansomsOZ => ExcelManager.RANSOMS_WORKSHEET_NAME,
                SourceSKUReportFileKey.RemainingWB or SourceSKUReportFileKey.RemainingOZ or SourceSKUReportFileKey.Shipped => ExcelManager.REMAINING_WORKSHEET_NAME,
                _ => String.Empty,
            };

            // Get source excel worksheet by name
            ExcelManager.GetExcelWorksheetByName(workSheetName, reportFileKey, out ExcelWorksheet? currentWorksheet);
            if (currentWorksheet is null)
            {
                await Task.Delay(100);
                OnManagerReached(new($"В файле книги по ключу {reportFileKey} нет листа с именем {ExcelManager.RANSOMS_WORKSHEET_NAME}", 2000, true));
                return StopFuctionExecuting(ref stopwatch);
            }

            // Get ranges from source excel worksheet
            List<RangeSourceReportRows> sourceReportRowsRanges = reportFileKey switch
            {
                SourceSKUReportFileKey.RansomsWB or SourceSKUReportFileKey.RansomsOZ =>
                    new(1) { new(ExcelManager.RansomsSourceCells) },
                SourceSKUReportFileKey.RemainingWB or SourceSKUReportFileKey.RemainingOZ =>
                    new(2) { new(ExcelManager.RemainingSourceCells01, ExcelManager.FIRST_ROOM_NAME),
                             new(ExcelManager.RemainingSourceCells02, ExcelManager.SECOND_ROOM_NAME,(ExcelManager.LAST_MARKER_KEY, ExcelManager.LAST_MARKER_KEY_OFFSET))},
                SourceSKUReportFileKey.Shipped =>
                    new(1) { new(ExcelManager.ShippedSourceCells)},     
                _ => new(),
            };
            ExcelManager.GetRangeSourceReportRows(in currentWorksheet, ref sourceReportRowsRanges);

            // Get raw data from cells in source excel worksheet 
            ExcelManager.GetRawDataFromExcelRows(in currentWorksheet, ref sourceReportRowsRanges);

            // Union collections
            List<Dictionary<string, ItemForParsing>> rawRowsItems = new() { };
            foreach (var sourceReportRowsRange in sourceReportRowsRanges)
            {
                sourceReportRowsRange.RowsWithRawData.ForEach(rawRowsItems.Add);
            }

            // Processings:
            switch (reportFileKey)
            {
                // Ransoms processing:
                case SourceSKUReportFileKey.RansomsWB or SourceSKUReportFileKey.RansomsOZ:

                    // Save balances for sku's
                    SaveSKUBalanceUnits(reportFileKey, rawRowsItems);

                    // Make sku's from ransoms balance units 
                    var makeSKUsTask = UpdateSKUsFromBalanceUnits(reportFileKey);
                    await Task.Run(async () => await makeSKUsTask);
                    makeSKUsTask.Wait();

                    // Report
                    static bool FindSkusWithRansoms(SKU sku) => sku.SKURansomsBalance != null;
                    var skusWithRansomsCount = SKUs.GetValueCount(FindSkusWithRansoms);
             
                    OnManagerReached(new($"Записано артикулов из выкупов, всего: {skusWithRansomsCount}", 1000, false));
                    break;

                // Remaining processing:
                case SourceSKUReportFileKey.RemainingWB or SourceSKUReportFileKey.RemainingOZ:

                    // Save balances for sku's
                    SaveSKUBalanceUnits(reportFileKey, rawRowsItems);

                    // Make sku's from ransoms balance units 
                    var updateTask = UpdateSKUsFromBalanceUnits(reportFileKey);
                    await Task.Run(async () => await updateTask);
                    updateTask.Wait();

                    // Report
                    static bool FindSkusWithRemaining(SKU sku) => sku.SKURemainingBalance != null;
                    var skusWithRemainingCount = SKUs.GetValueCount(FindSkusWithRemaining);
                    OnManagerReached(new($"Записано артикулов из баланса склада, всего: {skusWithRemainingCount}", 1000, false));

                    break;
                default:
                    break;
            };

            // Finish
            return StopFuctionExecuting(ref stopwatch);
        }

        /// <summary>
        /// Creates and saves SKURansomsBalanceUnit collection from a list of raw data dictionaries.
        /// </summary>
        /// <param name="reportFileKey">Key of source file with report.</param>
        /// <param name="rowsWithRawData">List of raw data dictionaries.</param>
        /// <returns>Number of keys in final dictionary.</returns>
        /// <exception cref="ArgumentException">Exception when passing an empty dictionary.</exception>
        /// <remarks><i>New version, tested</i></remarks>
        public int SaveSKUBalanceUnits(SourceSKUReportFileKey reportFileKey, List<Dictionary<string, ItemForParsing>> rowsWithRawData)
        {
            bool isRansoms = reportFileKey == SourceSKUReportFileKey.RansomsWB || reportFileKey == SourceSKUReportFileKey.RansomsOZ;
            bool isRemaining = reportFileKey == SourceSKUReportFileKey.RemainingWB || reportFileKey == SourceSKUReportFileKey.RemainingOZ;

            List<SourceSKUReportFileKey> validKeys = new()
            {
                SourceSKUReportFileKey.RansomsWB,
                SourceSKUReportFileKey.RansomsOZ,
                SourceSKUReportFileKey.RemainingWB,
                SourceSKUReportFileKey.RemainingOZ
            };

            if (!validKeys.Contains(reportFileKey))
            {
                OnManagerReached(new ManagerReachedEventArgs($"An incorrect key ({reportFileKey}) was passed to save balance units", 2000, true));
                return 0;
            }

            if (isRansoms)
            {
                var skuRansomsBalanceDataUnits = from rowWithRawData in rowsWithRawData
                                                 let unit = new SKURansomsBalanceDataUnit(rowWithRawData)
                                                 group unit by unit.SKUNumber 
                                                 into unitGroup  
                                                 select unitGroup;

                foreach (var group in skuRansomsBalanceDataUnits)
                {
                    var list = (((List<SKURansomsBalanceDataUnit>?)group.ToList())?.Cast<ParsingRowEntity>().ToList()) ?? 
                        throw new InvalidCastException("Невозможно привести List<SKURansomsBalanceDataUnit> к List<ParsingRowEntity>");
                    
                    SkuRansomsBalanceUnits.RetrieveAndUpdateOrAddValue(group.Key, list);
                }

            }
            else if (isRemaining)
            {
                var skuRemainingBalanceDataUnits = from rowWithRawData in rowsWithRawData
                                                   let unit = new SKURemainingBalanceDataUnit(rowWithRawData)
                                                   group unit by unit.SKUNumber 
                                                   into unitGroup  
                                                   select unitGroup;
                
                foreach (var group in skuRemainingBalanceDataUnits)
                {
                    var list = (((List<SKURemainingBalanceDataUnit>?)group.ToList())?.Cast<ParsingRowEntity>().ToList()) ?? 
                        throw new InvalidCastException("Невозможно привести List<SKURemainingBalanceDataUnit> к List<ParsingRowEntity>");
                    
                    SkuRemainingBalanceUnits.RetrieveAndUpdateOrAddValue(group.Key, list);
                }
            }

            return reportFileKey switch
            {
                SourceSKUReportFileKey.RansomsWB or SourceSKUReportFileKey.RansomsOZ => SkuRansomsBalanceUnits.Count(),
                SourceSKUReportFileKey.RemainingWB or SourceSKUReportFileKey.RemainingOZ => SkuRemainingBalanceUnits.Count(),
                _ => 0
            };
        }

        /// <summary>
        /// Creates a collection of SKUs objects from an array of "raw data".
        /// </summary>
        /// <exception cref="ArgumentException">Manager does not contain an array of "raw data".</exception>
        /// <remarks><i>New version, tested</i></remarks>
        public async Task UpdateSKUsFromBalanceUnits(SourceSKUReportFileKey skuReportFileKey)
        {
            bool isWB = skuReportFileKey == SourceSKUReportFileKey.RansomsWB || skuReportFileKey == SourceSKUReportFileKey.RemainingWB;
            bool isOZ = skuReportFileKey == SourceSKUReportFileKey.RansomsOZ || skuReportFileKey == SourceSKUReportFileKey.RemainingOZ;
            bool isRansoms = skuReportFileKey == SourceSKUReportFileKey.RansomsWB || skuReportFileKey == SourceSKUReportFileKey.RansomsOZ;
            bool isRemaining = skuReportFileKey == SourceSKUReportFileKey.RemainingWB || skuReportFileKey == SourceSKUReportFileKey.RemainingOZ;

            // Definition of a marketplace and collection
            Marketplace marketplace = isWB ? Marketplace.WB : Marketplace.OZ;
            IRepository<int, List<ParsingRowEntity>>? parsingEntityCollection = isRansoms ? SkuRansomsBalanceUnits : SkuRemainingBalanceUnits;

            if (parsingEntityCollection == null || parsingEntityCollection.Count() == 0)
            {
                await Task.Delay(100);
                OnManagerReached(new("Невозможно сформировать словарь артикулов из пустого словаря 'сырых' данных.", 2000, true));
                return;
            }

            List<(int key, List<ParsingRowEntity> value)>? listCollection = parsingEntityCollection.GetValuesList().ToList();
            if (listCollection is null || listCollection.Count == 0)
                throw new Exception("Невозможно обработать пустую коллекцию типа List<(int key, List<ParsingRowEntity> value)>?.");

            foreach (var (skuNumber, listEntities) in listCollection)
            {
                if (isRansoms)
                {
                    var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKURansomsBalanceDataUnit>().ToList()) ??
                        throw new InvalidCastException("Невозможно привести коллекцию к IList<SKURansomsBalanceDataUnit>");

                    SKURansomsBalance ransomsBalance = new(units.ToList());

                    var skuMaxRansomDate = units
                        .Select(u => u.SKURansomDate)
                        .Max();

                    var averagePrice = units
                        .Average(u => u.SKUCost);

                    SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(marketplace, skuNumber, ransomsBalance, averagePrice, skuMaxRansomDate));
                }
                else if (isRemaining)
                {
                    var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKURemainingBalanceDataUnit>().ToList()) ??
                        throw new InvalidCastException("Невозможно привести коллекцию к IList<SKURemainingBalanceDataUnit>");

                    SKURemainingBalance remainingBalance = new(units.ToList());

                    SKUs.RetrieveAndUpdateOrAddValue(skuNumber, new(marketplace, skuNumber, remainingBalance));
                }
            }
        }

        // /// <summary>
        // /// Returns saved article by its number or null.
        // /// </summary>
        // /// <param name="articleNumber">Article number to look up in  dict.</param>
        // /// <param name="article">Link to a copy of article.</param>
        // /// <returns>Logical flag for performing an operation.</returns>
        // /// <remarks><i>New version, tested</i></remarks>
        // public bool GetSKUByNumber(int articleNumber, out SKU? article)
        // {
        //     article = _skus.TryGetValue(articleNumber, out SKU? value) ? value : null;
        //     return article != null;
        // }

        // /// <summary>
        // /// Saves ArticleRowInReport in corresponding article. 
        // /// </summary>
        // /// <param name="skuRowsInReport">Link to ArticleRowInReport.</param>
        // /// <param name="marketplace">Marketplace products.</param>
        // /// <returns>Time it takes to complete operation in seconds.</returns>
        // /// <remarks><i>New version, tested</i></remarks>
        // public double SaveSKURowsInReport(List<SKURowInReport> skuRowsInReport, Marketplace marketplace)
        // {
        //     var stopwatch = new Stopwatch();
        //     stopwatch.Start();

        //     foreach (var skuRowInReport in skuRowsInReport)
        //     {
        //         var skuNumber = skuRowInReport.SKUNumber;

        //         if (GetSKUByNumber(skuNumber, out SKU? sku) && sku != null)
        //         {
        //             sku.SetArticleRowInReport(skuRowInReport);
        //         }
        //         else
        //         {
        //             _skus.Add(skuNumber, new SKU(marketplace, skuNumber, skuRowInReport));
        //         }
        //     }

        //     stopwatch.Stop();

        //     return stopwatch.ElapsedMilliseconds / 1000;
        // }

        // /// <summary>
        // /// Performs analysis of elements in article collection.
        // /// </summary>
        // /// <exception cref="Exception">An exception occurred when trying to parse an empty collection.</exception>
        // /// <remarks><i>New version, tested</i></remarks>
        // public void AnalyzeSKUs()
        // {
        //     if (Equals(_skus.Count, 0))
        //         throw new Exception("An attempt to parse a dict with a number of elements - 0");

        //     foreach (var (key, value) in _skus)
        //         value.AnalyzeSKU(DeadlineDays);
        // }

        #endregion
    }
}