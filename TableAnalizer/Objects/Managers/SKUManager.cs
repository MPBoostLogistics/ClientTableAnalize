using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.SKUItems;
using ClientTableAnalizer.Objects.SKUItems.SKUBalances;

namespace ClientTableAnalizer.Objects.Managers
{
    public class SKUManager : BaseManager
    {
        #region Properties 
        public int DeadlineDays { get; private set; }
        public Dictionary<int, SKU> SKUs {get;} = new();
        public static Dictionary<int, List<SKURansomsBalanceDataUnit>> SkuRansomsBalanceUnits { get; set; } = new();

        /// <summary>
        /// Returns SKU by SKU number.
        /// </summary>
        /// <param name="skuNumber">SKU number.</param>
        /// <returns>SKU object or null.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public SKU? this[int skuNumber] 
        {   
            get => (!SKUs.TryGetValue(skuNumber, out SKU? sku) && sku == null) ? sku : null; 
            set => SKUs[skuNumber] = value!;  
        } 

        /// <summary>
        /// Returns number of SKUs in collection.
        /// </summary>
        /// <returns>Number of SKUs in collection.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public int SKUsCount() => SKUs.Count;

        #endregion

        #region Constructors and destructors
        public SKUManager(ExcelReportManager excelReportManager) { }
        #endregion

        #region Functionality

        /// <summary>
        /// Sets period before deadline when paying a client (in days).
        /// </summary>
        /// <param name="days">Period until deadline, days</param>
        /// <remarks><i>New version, tested</i></remarks>
        public void SetDeadlineDays(int days) => DeadlineDays = days;

        /// <summary>
        /// Creates and saves SKURansomsBalanceUnit collection from a list of raw data dictionaries.
        /// </summary>
        /// <param name="rawDataRowItems">List of raw data dictionaries.</param>
        /// <returns>Number of keys in final dictionary.</returns>
        /// <exception cref="ArgumentException">Exception when passing an empty dictionary.</exception>
        /// <remarks><i>New version, tested</i></remarks>
        public static int SaveSKURansomsBalanceUnits(List<Dictionary<string, (string value, ParsingTypeCode parsingTypeCode)>> rawDataRowItems)
        {
            foreach (var rawDataRowItem in rawDataRowItems)
            {
                if (rawDataRowItem is null)
                    throw new ArgumentException("It is not possible to create SKURansomsBalanceUnit from an empty value.");

                // Parsing && save data  
                SKURansomsBalanceDataUnit? unit = new(in rawDataRowItem);
                if (unit is not null)
                {
                    var key = unit.SKUNumber;
                    if (SkuRansomsBalanceUnits.TryGetValue(key, out List<SKURansomsBalanceDataUnit>? value))
                    {
                        value.Add(unit);
                    }
                    else
                    {
                        SkuRansomsBalanceUnits.TryAdd(key: unit.SKUNumber, value: new List<SKURansomsBalanceDataUnit> { unit });
                    }
                }
            }

            return SkuRansomsBalanceUnits.Count;
        }

        /// <summary>
        /// Creates a collection of SKUs objects from an array of "raw data".
        /// </summary>
        /// <exception cref="ArgumentException">Manager does not contain an array of "raw data".</exception>
        /// <remarks><i>New version, tested</i></remarks>
        public void MakeSKUsFromRansomsBalanceUnits(Marketplace marketplace)
        {
            if (SkuRansomsBalanceUnits == null || SkuRansomsBalanceUnits.Count == 0) 
                throw new ArgumentException("It is not possible to create SKUs objects from an empty raw data dictionary.");

            foreach (var skuRansomsBalanceUnit in SkuRansomsBalanceUnits)
            {
                var skuNumber = skuRansomsBalanceUnit.Key;
                
                if (!SKUs.TryGetValue(skuNumber, out SKU? sku) && sku == null)
                {
                    SKURansomsBalance ransomsBalance = new(skuRansomsBalanceUnit.Value);

                    var skuMaxRansomDate = skuRansomsBalanceUnit.Value
                        .Select(u => u.SKURansomDate)
                        .Max();

                    var averagePrice = skuRansomsBalanceUnit.Value
                        .Average(u => u.SKUCost);

                    this[skuNumber] = new (marketplace, skuNumber, ransomsBalance, averagePrice, skuMaxRansomDate);
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