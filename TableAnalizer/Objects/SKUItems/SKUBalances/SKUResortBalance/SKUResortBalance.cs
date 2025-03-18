using System.Diagnostics;
using System.Text;

namespace TableAnalizer.Objects.SKUItems.SKUBalances.SKUResortBalance
{
    public class ResortBalanceUnit (SKUResortBalanceTarget balanceTarget)
    {
        public SKUResortBalanceTarget BalanceTarget { get; } = balanceTarget;

        public Dictionary<int ,int> ResortTo { get; private set; } = []; 

        public void Update(int sku, int sum) 
        {
            if(ResortTo.TryGetValue(sku, out int value)) 
            {
                value += sum;
                ResortTo[sku] = value;
            }
            else 
            {
                ResortTo[sku] = sum;
            }
        }
    }

    public class SKUResortBalance
    {
        #region Variables and constants
        private const string RESORT_CONSUMPTION_KEY = "Пересортица расходов";
        private const string RESORT_RECEIPT_KEY = "Пересортица приходов";
        private const string RESORT_CONSUMPTION_FIRST_LINE = "Пересортица расхода на др.арт. (вычтен из \"Приход товаром\" и \"Расход товаром\"):\n";
        private const string RESORT_RECEIPT_FIRST_LINE = "Пересортица прихода на др.арт. (вычтен из \"Приход товаром\"):\n";
        #endregion

        #region Properties
        /// <summary>
        /// Sku FROM WHICH re-sorting is what was <b>not delivered</b> and is <b>written off</b> in 1C system
        /// </summary>
        public int SKUFromResortNumber { get; private set; }
        /// <summary>
        /// SKUs FOR WHICH re-sorting - what was <b>re-graded</b> and <b>entered</b> into 1C system: key - sku to resort: value - target of resorting, count.
        /// </summary>
        public readonly Dictionary<int, Dictionary<SKUResortBalanceTarget, int>> SKUsToResort = []; 
        
        public int MoreReceiptThanWaiting {get; private set;} = 0;
        
        public string ResortComment { get; private set; } = string.Empty;
        
        #endregion Properties

        //public delegate bool FindSkusInBalanceDelegate((SKUResortBalanceTarget target, int count) item); 

        #region Constructors
        public SKUResortBalance(int skuFromResortNumber, List<SKUResortDataUnit> skuToResortUnits)
        {
            SKUFromResortNumber = skuFromResortNumber;
            Update(skuToResortUnits);
        }
        #endregion

        #region Functionality
        public void Update(List<SKUResortDataUnit> skuResortDataUnits) 
        {
            static void SkippedProcedureMessage() 
            {
                Trace.WriteLine("Entry for re-sort must contain a correct comment.\nSkipped.");
            }

            foreach (var skuResortDataUnit in skuResortDataUnits)
            {
                // If there is no comment, entry is skipped
                if(string.IsNullOrEmpty(skuResortDataUnit.ResortComment)) 
                {
                    SkippedProcedureMessage();
                    continue;
                }

                if(!CheckEqualsSkus(skuResortDataUnit))
                {
                    continue;
                }

                // Get key and target
                var skuToResortKey = skuResortDataUnit.SKUToResortNumber;
                SKUResortBalanceTarget skuResortBalanceTarget;

                switch (skuResortDataUnit.ResortComment!.Trim())
                {
                    case RESORT_CONSUMPTION_KEY:
                        skuResortBalanceTarget = SKUResortBalanceTarget.ResortingConsumption;
                        break;
                    case RESORT_RECEIPT_KEY:
                        skuResortBalanceTarget = SKUResortBalanceTarget.ResortingReceipt;
                        break;
                    default:
                        SkippedProcedureMessage();
                        continue;
                }

                if (SKUsToResort.TryGetValue(skuToResortKey, out Dictionary<SKUResortBalanceTarget, int>? value))
                {
                    var resortDict = value;
                    if (resortDict.TryGetValue(skuResortBalanceTarget, out int count))
                    {
                        var resortCountByTarget = count + skuResortDataUnit.SKUToResortNumberCount;
                        resortDict[skuResortBalanceTarget] = resortCountByTarget;
                    }
                    else
                    {
                        resortDict[skuResortBalanceTarget] = skuResortDataUnit.SKUToResortNumberCount;
                    }

                    SKUsToResort[skuToResortKey] = resortDict;
                }
                else
                {
                    SKUsToResort[skuToResortKey] = new Dictionary<SKUResortBalanceTarget, int>()
                    {
                        {skuResortBalanceTarget, skuResortDataUnit.SKUToResortNumberCount}
                    };
                }
            }
        }

        private bool CheckEqualsSkus(SKUResortDataUnit skuResortDataUnit)
        {
            // If balance and resortFrom articles are not equal, entry is skipped.
            if (!int.Equals(SKUFromResortNumber, skuResortDataUnit.SKUFromResortNumber))
            {
                Trace.WriteLine($"Balance ({SKUFromResortNumber}) and resortFrom ({skuResortDataUnit.SKUFromResortNumber}) skus number are not equal. Skipped.");
                return false;
            }
            else 
            {
                return true;
            }
        }

        public void Update(Dictionary<int, Dictionary<SKUResortBalanceTarget, int>>? resortSKUsInNewValues) 
        {       
            if(resortSKUsInNewValues is null || resortSKUsInNewValues.Count == 0) return;

            foreach (var resortSKUsInNewValue in resortSKUsInNewValues)
            {
                var resortSKUsInNewValueKey = resortSKUsInNewValue.Key;
                if (SKUsToResort.TryGetValue(resortSKUsInNewValueKey, out Dictionary<SKUResortBalanceTarget, int>? value)) 
                {
                    var resortDict = value;

                    foreach (var item in resortSKUsInNewValue.Value)
                    {
                        var skuResortBalanceTarget = item.Key;
                        if (resortDict.TryGetValue(skuResortBalanceTarget, out int count))
                        {
                            var resortCountByTarget = count + item.Value;
                            resortDict[skuResortBalanceTarget] = resortCountByTarget;
                        }
                        else
                        {
                            resortDict[skuResortBalanceTarget] = item.Value;
                        }

                        SKUsToResort[resortSKUsInNewValueKey] = resortDict;
                    }
                } 
                else 
                {
                    SKUsToResort[resortSKUsInNewValueKey] = resortSKUsInNewValue.Value;
                }
            }
        }

        public (int sum, string? comment)? FindSkusInBalanceBy(SKUResortBalanceTarget target) 
        {
            var resortBalanceUnit = GetSkusResortToSum(target);
            if(resortBalanceUnit.ResortTo.Count == 0) return null;

            var sum = resortBalanceUnit.ResortTo.Aggregate(0, (acc, i) => acc + i.Value);
            var comment = string.Empty;
            StringBuilder stringBuilder = new();

            // Исходящие пересорты
            switch(target) 
            {
                case SKUResortBalanceTarget.ResortingConsumption:
                    stringBuilder.Append(RESORT_CONSUMPTION_FIRST_LINE);
                    break;
                case SKUResortBalanceTarget.ResortingReceipt:
                    stringBuilder.Append(RESORT_RECEIPT_FIRST_LINE);
                    break;
            }

            foreach (var item in resortBalanceUnit.ResortTo)
            {
                stringBuilder.Append($"{item.Key}, шт - {item.Value}");
                if(resortBalanceUnit.ResortTo.Keys.Last() != item.Key) 
                {
                    stringBuilder.Append(", ");
                }
            }

            // Result
            comment = stringBuilder.ToString();
            return (sum, comment);
            
        }

        public ResortBalanceUnit GetSkusResortToSum(SKUResortBalanceTarget target) 
        {
            var resort = new ResortBalanceUnit(target);
            foreach (var sku in SKUsToResort)
            {
                foreach ((SKUResortBalanceTarget t, int s) in sku.Value)
                {
                    if(t == target) 
                    {
                        resort.Update(sku.Key, s);
                    }
                }
            }

            return resort;
        }

        #endregion
    }
}