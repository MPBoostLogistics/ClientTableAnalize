using Microsoft.Extensions.Configuration;
using TableAnalizer.Objects.SKUItems;

namespace TableAnalizer.Objects.Managers
{
    public class AnalyticalManager(IConfiguration config) : BaseManager(config)
    {      
        #region Fields
        private static Func<SKUForTargetReports, bool> OverPredicate => sku => sku.IsSKUNormalize == false && sku.MoreMutualSettlementThanWaiting > 0;
        private static Func<SKUForTargetReports, bool> UnderPredicate => sku => sku.IsSKUNormalize == false && sku.LessMutualSettlementThanWaiting > 0;

        private static readonly Func<SKUForTargetReports, SKUForTargetReports, bool> MatchBySumAndPrice = (ob, ub) => 
            ob.MoreMutualSettlementThanWaiting >= ub.LessMutualSettlementThanWaiting &&
            ob.SKUPrice >= ub.SKUPrice;

         private static readonly Func<SKUForTargetReports, SKUForTargetReports, bool> MatchBySum = (ob, ub) => 
            ob.MoreMutualSettlementThanWaiting >= ub.LessMutualSettlementThanWaiting;
            
        /// <summary>
        /// Балансы с перерасходами
        /// </summary>
        private static List<SKUForTargetReports>? overspendingSkus;
        /// <summary>
        /// Балансы с перерасходами
        /// </summary>
        private static List<SKUForTargetReports>? underspendingSkus;
            
        #endregion

        #region Functionality

        private static void UpdateSkus(ref List<SKUForTargetReports> skuForTargetReports) 
        {
            var obs = GetSkus(ref skuForTargetReports, OverPredicate);
            if(obs is not null) 
            {
                overspendingSkus = [..obs];
            }

            var ubs = GetSkus(ref skuForTargetReports, UnderPredicate);
            if(ubs is not null) 
            {
                underspendingSkus = [.. ubs];
            }
        }

        public static Task AnalysisTargetSKUs(ref List<SKUForTargetReports> skuForTargetReports) 
        {
            UpdateSkus(ref skuForTargetReports);

            // Проход по артикулам с недорасходом
            if(underspendingSkus is not null) 
            {
                for (int i = 0; i < underspendingSkus.Count; i++)
                {
                    if(overspendingSkus is not null) 
                    {
                        SKUForTargetReports? obPretender;

                        // Артикул-претендент перерасхода по совпадению количества и цены товаров
                        obPretender = overspendingSkus.FirstOrDefault(el => MatchBySumAndPrice(el, underspendingSkus[i]));
                        // Артикул-претендент перерасхода по совпадению количества
                        obPretender ??= overspendingSkus.FirstOrDefault(el => MatchBySum(el, underspendingSkus[i]));

                        if(obPretender is not null)
                        {
                            var resortSum = underspendingSkus[i].LessMutualSettlementThanWaiting;
                            underspendingSkus[i].MakeResort(SKUResortTarget.ResortingConsumption, SKUResortDirection.Incoming, obPretender.SKUNumber, resortSum);
                            obPretender.MakeResort(SKUResortTarget.ResortingConsumption, SKUResortDirection.Outgoing, underspendingSkus[i].SKUNumber, resortSum);
                        } 

                    }
                
                    UpdateSkus(ref skuForTargetReports);
                }
            }
        
            return Task.CompletedTask;
        } 
        
        private static IEnumerable<SKUForTargetReports>? GetSkus(ref List<SKUForTargetReports> skuForTargetReports, Func<SKUForTargetReports, bool> skuPredicate) 
        {
            return from skuForTarget in skuForTargetReports
                    where skuPredicate(skuForTarget)
                    select skuForTarget;
        }

        #endregion
    }
}