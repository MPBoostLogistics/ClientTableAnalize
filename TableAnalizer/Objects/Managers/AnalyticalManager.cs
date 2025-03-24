using System.Collections;
using iText.Layout.Element;
using Microsoft.Extensions.Configuration;
using TableAnalizer.Objects.SKUItems;

namespace TableAnalizer.Objects.Managers
{
    public class AnalyticalManager(IConfiguration config) : BaseManager(config)
    {
        #region Fields
        /// <summary>
        /// <b>Предикат для артикулов с перерасходами (по приходу)</b>
        /// </summary>
        /// <remarks>Артикул не нормализован, приход больше ожидания и приход - приход больше ожидания > 0</remarks>
        //private static Func<SKUForTargetReports, bool> OverPredicateByReceipt => sku => !sku.IsSKUNormalize && sku.MoreReceiptThanWaiting > 0 && int.IsPositive(sku.SKUReceiptGoods - sku.MoreReceiptThanWaiting);
        
        private static Func<SKUForTargetReports, bool> OverPredicateConsumptionByOutgoingProducts => sku => !sku.IsSKUNormalize && sku.MoreMutualSettlementThanWaiting > 0 && int.IsPositive(sku.SKUOutgoingProducts - sku.MoreMutualSettlementThanWaiting);

         private static Func<SKUForTargetReports, bool> OverPredicateConsumptionByTransfer => sku => !sku.IsSKUNormalize && sku.MoreMutualSettlementThanWaiting > 0 && int.IsPositive(sku.SKUTransfered - sku.MoreMutualSettlementThanWaiting);

        //private static Func<SKUForTargetReports, bool> OverPredicateByReceiptAndConsumption => 
          //  sku => OverPredicateByReceipt(sku) && sku.MoreMutualSettlementThanWaiting > 0;

        private static Func<SKUForTargetReports, bool> UnderPredicateByConsumption => sku => !sku.IsSKUNormalize && sku.LessMutualSettlementThanWaiting > 0;

        /// <summary>
        /// Артикулы с перерасходами (по расходу)
        /// </summary>
        private static LinkedList<SKUForTargetReports>? overspendingSkusByConsumption;

        /// <summary>
        /// Артикулы с перерасходами (по приходу и расходу)
        /// </summary>
        //private static List<SKUForTargetReports>? overspendingSkusByReceiptAndConsumption;

        /// <summary>
        ///  Артикулы с недорасходами (по расходу)
        /// </summary>
        private static LinkedList<SKUForTargetReports>? underspendingSkusByConsumption;

        private static bool isNeedUnderspendingByConsumption;
        private static bool isPossibilityOverspendingByConsumption;
       // private static bool isNeedsAndPossibilityOverspendingByReceiptAndConsumption;

        #endregion

        #region Functionality

        private static Task UpdateDataForResorts(ref List<SKUForTargetReports> skuForTargetReports)
        {
            // var ossByReceiptAndConsumption = GetSkus(ref skuForTargetReports, OverPredicateByReceiptAndConsumption);
            // overspendingSkusByReceiptAndConsumption = ossByReceiptAndConsumption is not null ? [.. ossByReceiptAndConsumption] : null;
            // isNeedsAndPossibilityOverspendingByReceiptAndConsumption = overspendingSkusByReceiptAndConsumption is not null && overspendingSkusByReceiptAndConsumption.Count != 0;

            var ossByConsumption = GetSkus(ref skuForTargetReports, [OverPredicateConsumptionByOutgoingProducts, OverPredicateConsumptionByTransfer]);
            overspendingSkusByConsumption = ossByConsumption is not null ? new LinkedList<SKUForTargetReports>(ossByConsumption.OrderByDescending(el => el.SKUNumber)) : null;
            isPossibilityOverspendingByConsumption = overspendingSkusByConsumption is not null && overspendingSkusByConsumption.Count != 0;

            var ussByConsumption = GetSkus(ref skuForTargetReports, [UnderPredicateByConsumption]);
            underspendingSkusByConsumption = ussByConsumption is not null ? new LinkedList<SKUForTargetReports>(ussByConsumption.OrderByDescending(el => el.SKUNumber)) : null;
            isNeedUnderspendingByConsumption = underspendingSkusByConsumption is not null && underspendingSkusByConsumption.Count != 0;

            return Task.CompletedTask;
        }

        public static Task SetConsumptionResorts(ref List<SKUForTargetReports> skuForTargetReports)
        {
            // Initial set collections
            var updateDataTask = UpdateDataForResorts(ref skuForTargetReports);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            if (isNeedUnderspendingByConsumption && isPossibilityOverspendingByConsumption)
            {
                var overNode = overspendingSkusByConsumption?.First;
                var underNode = underspendingSkusByConsumption?.First;

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

                    SKUResortTarget? skuResortTarget = (OverPredicateConsumptionByOutgoingProducts(overSku), OverPredicateConsumptionByTransfer(overSku)) switch 
                    {
                        (true, true) or (true, false) => SKUResortTarget.ResortingConsumptionByOutgoing,
                        (false, true) => SKUResortTarget.ResortingConsumptionByTransfer,
                        (false, false) => null
                    };

                    if(skuResortTarget is not null) 
                    {
                        var target = (SKUResortTarget)skuResortTarget;
                        int underSkuIndex = skuForTargetReports.IndexOf(underSku);
                        int overSkuIndex = skuForTargetReports.IndexOf(overSku);
                    
                        MakeResort(ref skuForTargetReports, target, underSkuIndex, overSkuIndex, result);
                    }
                }
            }

            // Initial set collections
            updateDataTask = UpdateDataForResorts(ref skuForTargetReports);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            return Task.CompletedTask;
            
        }

        private static void MakeResort(ref List<SKUForTargetReports> skuForTargetReports, SKUResortTarget target, int underSkuIndex, int overSkuIndex, int sum)
        {
            if(int.IsNegative(underSkuIndex) || int.IsNegative(overSkuIndex)) 
            {
                return;
            } 

            // Пересорты расходов
            if(underSkuIndex != overSkuIndex && (target == SKUResortTarget.ResortingConsumptionByOutgoing || target == SKUResortTarget.ResortingConsumptionByTransfer)) 
            {
                skuForTargetReports[underSkuIndex].MakeResort(target, SKUResortDirection.Incoming, skuForTargetReports[overSkuIndex].SKUNumber, sum);
                skuForTargetReports[overSkuIndex].MakeResort(target, SKUResortDirection.Outgoing, skuForTargetReports[underSkuIndex].SKUNumber, sum);
            }

            // Пересорты приходов
            if (underSkuIndex == overSkuIndex && target == SKUResortTarget.ResortingReceipt) 
            {
                skuForTargetReports[underSkuIndex].MakeResort(target, SKUResortDirection.Incoming, skuForTargetReports[overSkuIndex].SKUNumber, sum);
            }
        }

        /// <summary>
        /// Компесация возможных исходящих пересортов расхода если им нет пары
        /// Списывается излишний приход. 
        /// </summary>
        // public static Task SetReceiptResorts(ref List<SKUForTargetReports> skuForTargetReports) 
        // {
        //     // Initial set collections
        //     var updateDataTask = UpdateDataForResorts(ref skuForTargetReports);
        //     Task.Run(() => updateDataTask);
        //     updateDataTask.Wait();

        //     if (isNeedsAndPossibilityOverspendingByReceiptAndConsumption) 
        //     {
        //         foreach (var sku in overspendingSkusByReceiptAndConsumption!)
        //         {
        //             int sum = sku.MoreReceiptThanWaiting >= sku.MoreMutualSettlementThanWaiting ? 
        //                 sku.MoreMutualSettlementThanWaiting :
        //                 sku.MoreReceiptThanWaiting;

        //             int skuIndex = skuForTargetReports.IndexOf(sku);

        //             MakeResort(ref skuForTargetReports, SKUResortTarget.ResortingReceipt, skuIndex, skuIndex, sum);
        //         }
        //     }

        //     return Task.CompletedTask;
        // }

        private static IEnumerable<SKUForTargetReports>? GetSkus(ref List<SKUForTargetReports> skuForTargetReports, List<Func<SKUForTargetReports, bool>> skuPredicates)
        {
            return from skuForTarget in skuForTargetReports
                   from predicate in skuPredicates
                   where predicate(skuForTarget)
                   select skuForTarget;
        }

        #endregion
    }
}