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
            var updateDataTask = UpdateDataSkus(ref skuForTargetReports);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            if (!isNeedUnderspending || !isPossibilityOverspending) return Task.CompletedTask;

            LinkedListNode<SKUForTargetReports>? usc;

            // Пока есть возможность пересортов расхода
            while (isPossibilityOverspending)
            {
                usc = underspendingSkus!.First;
                StringBuilder stringBuilder = new($"------------\nus candidate: {usc!.Value.SKUNumber}, need sum: {usc!.Value.LessMutualSettlementThanWaiting}\n");
                Dictionary<int, int> resortResult = SetAutoMoreMutualSettlementResort(ref stringBuilder, usc!);
  
                int usi = skuForTargetReports.IndexOf(usc!.Value);

                foreach (var item in resortResult)
                {
                    SKUForTargetReports? osiItem = skuForTargetReports.SingleOrDefault(el => el.SKUNumber == item.Key);
                    
                    if (osiItem != null)
                    {
                        int osi = skuForTargetReports.IndexOf(osiItem);
                        if (int.IsPositive(usi) && int.IsPositive(osi))
                        {
                            skuForTargetReports[usi].MakeResort(SKUResortTarget.ResortingConsumption, SKUResortDirection.Incoming, item.Key, item.Value);
                            skuForTargetReports[osi].MakeResort(SKUResortTarget.ResortingConsumption, SKUResortDirection.Outgoing, usc.Value.SKUNumber, item.Value);

                            Trace.WriteLine("Resort ok!");
                        }
                    }
                }
            
                // Update collections
                updateDataTask = UpdateDataSkus(ref skuForTargetReports);
                Task.Run(() => updateDataTask);
                updateDataTask.Wait();

                resortResult.Clear();
                stringBuilder = new();
            }

            return Task.CompletedTask;
        }

        private static Dictionary<int, int> SetAutoMoreMutualSettlementResort(ref StringBuilder stringBuilder, LinkedListNode<SKUForTargetReports> usc)
        {
            Dictionary<int, int> resortResult = [];
            static bool isResortComplete(Dictionary<int, int> d, int n) => d.Aggregate(0, (acc, i) => acc + i.Value) == n;

            var osc = overspendingSkus!.First;

            // Пока необходимая сумма пререрасхода не набрана
            while (!isResortComplete(resortResult, usc.Value.LessMutualSettlementThanWaiting))
            {
                stringBuilder.Append($"os candidate: {osc!.Value.SKUNumber}, ");

                if (osc.Value.MoreMutualSettlementThanWaiting == 0 && osc.Next != null)
                {
                    osc = osc.Next;
                    continue;
                }

                int takeSum = osc.Value.MoreMutualSettlementThanWaiting >= usc.Value.LessMutualSettlementThanWaiting ?
                    usc.Value.LessMutualSettlementThanWaiting :
                    osc.Value.MoreMutualSettlementThanWaiting;

                stringBuilder.Append($"take sum: {takeSum}\n");

                resortResult[osc.Value.SKUNumber] = takeSum;

                if (!isResortComplete(resortResult, usc.Value.LessMutualSettlementThanWaiting) && osc.Next != null) 
                {
                    osc = osc.Next;
                    continue;
                }
                else 
                {
                    Trace.WriteLine(stringBuilder.ToString());
                    break;
                } 
            }

            return resortResult;
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