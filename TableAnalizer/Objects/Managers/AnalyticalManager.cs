using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Configuration;
using TableAnalizer.Objects.SKUItems;

namespace TableAnalizer.Objects.Managers
{
    public class AnalyticalManager(IConfiguration config) : BaseManager(config)
    {
        #region Fields
        private static Func<SKUForTargetReports, bool> OverPredicate => sku => !sku.IsSKUNormalize && sku.MoreMutualSettlementThanWaiting > 0 && int.IsPositive(sku.SKUOutgoingProducts - sku.MoreMutualSettlementThanWaiting);
        private static Func<SKUForTargetReports, bool> UnderPredicate => sku => !sku.IsSKUNormalize && sku.LessMutualSettlementThanWaiting > 0;

        private static readonly Func<SKUForTargetReports, SKUForTargetReports, bool> MatchByPrice = (ov, us) =>
            ov.SKUPrice >= us.SKUPrice;

        /// <summary>
        /// Артикулы с перерасходами (по расходу)
        /// </summary>
        private static LinkedList<SKUForTargetReports>? overspendingSkus;
        /// <summary>
        ///  Артикулы с недорасходами (по расходу)
        /// </summary>
        private static LinkedList<SKUForTargetReports>? underspendingSkus;

        private static bool isNeedUnderspending;
        private static bool isPossibilityOverspending;

        #endregion

        #region Functionality

        private static Task UpdateDataSkus(ref List<SKUForTargetReports> skuForTargetReports)
        {
            var oss = GetSkus(ref skuForTargetReports, OverPredicate);
            overspendingSkus = oss is not null ? new LinkedList<SKUForTargetReports>(oss) : null;
            isPossibilityOverspending = overspendingSkus is not null && overspendingSkus.Count != 0;

            var uss = GetSkus(ref skuForTargetReports, UnderPredicate);
            underspendingSkus = uss is not null ? new LinkedList<SKUForTargetReports>(uss) : null;
            isNeedUnderspending = underspendingSkus is not null && underspendingSkus.Count != 0;

            return Task.CompletedTask;
        }

        public static Task SetAutoMoreMutualSettlementResorts(ref List<SKUForTargetReports> skuForTargetReports)
        {
            // Initial set collections
            var updateDataTask = UpdateDataSkus(ref skuForTargetReports);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            if (isNeedUnderspending && isPossibilityOverspending)
            {
                var overNode = overspendingSkus?.First;
                var underNode = underspendingSkus?.First;

                while (underNode != null && overNode != null)
                {
                    var overSku = overNode.Value;
                    var underSku = underNode.Value;

                    if (overSku.MoreMutualSettlementThanWaiting == 0)
                    {
                        overNode = overNode.Next;
                        continue;
                    }

                    int result = 0;
                    if (overSku.MoreMutualSettlementThanWaiting >= underSku.LessMutualSettlementThanWaiting)
                    {
                        result = underSku.LessMutualSettlementThanWaiting;
                        underNode = underNode.Next;
                    }
                    else
                    {
                        result = overSku.MoreMutualSettlementThanWaiting;
                        overNode = overNode.Next;
                    }

                    int underSkuIndex = skuForTargetReports.IndexOf(underSku);
                    int overSkuIndex = skuForTargetReports.IndexOf(overSku);
                    
                    MakeResort(ref skuForTargetReports, underSkuIndex, overSkuIndex, result);
                }
            }

            // Initial set collections
            updateDataTask = UpdateDataSkus(ref skuForTargetReports);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            if(isNeedUnderspending && isPossibilityOverspending) 
            {
                return SetAutoMoreMutualSettlementResorts(ref  skuForTargetReports);
            } 
            else 
            {
                return Task.CompletedTask;
            }
        }

        private static void MakeResort(ref List<SKUForTargetReports> skuForTargetReports, int underSkuIndex, int overSkuIndex, int sum)
        {
            if(int.IsNegative(underSkuIndex) || int.IsNegative(overSkuIndex)) 
            {
                return;
            }

            skuForTargetReports[underSkuIndex].MakeResort(SKUResortTarget.ResortingConsumption, SKUResortDirection.Incoming, skuForTargetReports[overSkuIndex].SKUNumber, sum);
            skuForTargetReports[overSkuIndex].MakeResort(SKUResortTarget.ResortingConsumption, SKUResortDirection.Outgoing, skuForTargetReports[underSkuIndex].SKUNumber, sum);
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