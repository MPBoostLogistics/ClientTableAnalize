using System.Diagnostics;
using System.Text;
using TableAnalizer.Objects.Enums;

namespace TableAnalizer.Objects.SKUItems.SKUBalances.SKUResortBalance
{
    public class SKUResortBalance
    {
        #region Variables and constants
        public const string RESORT_1C_NAME = "Создан пересорт в 1С";  
        public const string MAYBE_RESORT_1C_NAME = "Возможен пересорт в 1С";  
        public const string MAYBE_WRITEOFF_POINTS_NAME = "Возможно списание с точек";  
        public const string NO_SOLUTION_NAME = "Нет решениия"; 
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
        public string ResortComment { get; private set; } = string.Empty;
        
        #endregion Properties

        public delegate bool FindSkusInBalanceDelegate((SKUResortBalanceTarget target, int count) item); 

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
                Trace.WriteLine("Entry for re-sort must contain a comment. Skipped.");
            }

            foreach (var skuResortDataUnit in skuResortDataUnits)
            {
                // If there is no comment, entry is skipped
                if (skuResortDataUnit.ResortComment == string.Empty)
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
                    case RESORT_1C_NAME:
                        skuResortBalanceTarget = SKUResortBalanceTarget.ResortIn1C;
                        break;
                    case MAYBE_RESORT_1C_NAME:
                        skuResortBalanceTarget = SKUResortBalanceTarget.MaybeResortIn1C;
                        break;
                    case MAYBE_WRITEOFF_POINTS_NAME:
                        skuResortBalanceTarget = SKUResortBalanceTarget.MaybeWriteOffFromPoints;
                        break;
                    case NO_SOLUTION_NAME:
                        skuResortBalanceTarget = SKUResortBalanceTarget.NoSolution;
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

        public (int sum, string comment)? FindSkusInBalanceByPredicate(FindSkusInBalanceDelegate findSkusInBalanceDelegate) 
        {
            var items = GetSkusResortToSum(findSkusInBalanceDelegate);

            if(!items.Any()) return null;

            StringBuilder stringBuilder = new("Пересортица на артикулы:\n");
            foreach (var item in items)
            {
                stringBuilder.Append($"{item.skuResortTo}, шт - {item.skuResortToSum}");
                if(items.Last() != item) stringBuilder.Append(", ");
            }

            var sum = items.Aggregate(0, (acc, i) => acc + i.skuResortToSum);
            return (sum, stringBuilder.ToString());
        }

        public IEnumerable<(int skuResortTo, int skuResortToSum)> GetSkusResortToSum(FindSkusInBalanceDelegate findSkusInBalanceDelegate) 
        {
            return from skuToResort in SKUsToResort
                   from item in skuToResort.Value
                   where findSkusInBalanceDelegate((item.Key ,item.Value))
                   let data = (skuToResort.Key, item.Value)
                   select data;
        }
        
        public override string ToString()
        {
            StringBuilder builder = new($"----------\n{SKUFromResortNumber} --> SKUResortBalance:\n"); 

            foreach (var skuToResort in SKUsToResort)
            {
                builder.Append($"- Sku to resort number: {skuToResort.Key}\n");

                foreach (var skuToResortValues in skuToResort.Value)
                {
                    builder.Append($"-- Sku to resort target/count: {skuToResortValues.Key} : {skuToResortValues.Value}\n");
                }
            }

            builder.Append($"----------\n");
            builder.Append(String.Empty);

            return builder.ToString();
        }

        #endregion
    }
}