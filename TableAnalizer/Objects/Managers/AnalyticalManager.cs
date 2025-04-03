using Microsoft.Extensions.Configuration;
using TableAnalizer.Objects.SKUItems;

namespace TableAnalizer.Objects.Managers
{
    public class AnalyticalManager(IConfiguration config) : BaseManager(config)
    {
        #region Fields
        
        private static Func<SKUForTargetReports, bool> OverPredicateConsumptionByOutgoingProducts => sku => !sku.IsSkuNormalize && sku.MoreMutualSettlementThanWaiting > 0 && int.IsPositive(sku.SKUOutgoingProducts - sku.MoreMutualSettlementThanWaiting);

        private static Func<SKUForTargetReports, bool> OverPredicateConsumptionByTransfer => sku => !sku.IsSkuNormalize && sku.MoreMutualSettlementThanWaiting > 0 && int.IsPositive(sku.SKUTransfered - sku.MoreMutualSettlementThanWaiting);

        private static Func<SKUForTargetReports, bool> UnderPredicateByConsumption => sku => !sku.IsSkuNormalize && sku.LessMutualSettlementThanWaiting > 0;

        private static Func<SKUForTargetReports, bool> OverPredicateByReceipt=> sku => !sku.IsSkuNormalize && sku.MoreReceiptThanWaiting > 0 && int.IsPositive(sku.SKUReceiptGoods - sku.MoreReceiptThanWaiting);

        private static Func<SKUForTargetReports, bool> UnderPredicateByReceipt => sku => !sku.IsSkuNormalize && sku.LessReceiptThanWaiting > 0;

        /// <summary>
        /// Артикул не номализован, взаиморасчет + остаток = ожидание и общий приход больше ожидания
        /// </summary>
        private static Func<SKUForTargetReports, bool> ExtraPredicateByReceipt => sku => 
            !sku.IsSkuNormalize && 
            (sku.SKUSummarySettlement + sku.SKURemaining) == sku.SKURansomsWaiting && 
            sku.SKUReceiptTotal > sku.SKURansomsWaiting;

         private static Func<SKUForTargetReports, bool> ExtraPredicateByReceiptFinal => 
            sku => !sku.IsSkuNormalize && 
            sku.SKUReceiptTotal > sku.SKURansomsWaiting;

        /// <summary>
        /// Артикулы с перерасходами (по расходу)
        /// </summary>
        private static LinkedList<SKUForTargetReports>? overspendingSkusByConsumption;

        /// <summary>
        ///  Артикулы с недорасходами (по расходу)
        /// </summary>
        private static LinkedList<SKUForTargetReports>? underspendingSkusByConsumption;

        /// <summary>
        /// Артикулы с перерасходами (по приходу)
        /// </summary>
        private static LinkedList<SKUForTargetReports>? overspendingSkusByReceipt;

        /// <summary>
        ///  Артикулы с недорасходами (по приходу)
        /// </summary>
        private static LinkedList<SKUForTargetReports>? underspendingSkusByReceipt;

        /// <summary>
        ///  Артикулы с экстра приходом=
        /// </summary>
        private static List<SKUForTargetReports>? extraSkusByReceipt;
        private static bool isNeedUnderspendingByConsumption;
        private static bool isPossibilityOverspendingByConsumption;
        private static bool isNeedUnderspendingByReceipt;
        private static bool isPossibilityOverspendingByReceipt;
        /// <summary>
        /// Есть артикулы с экстра-приходом
        /// </summary>
        private static bool isThereAreSkusWithExtraReceipt;

        #endregion

        #region Functionality

        private static Task UpdateDataForResorts(ref List<SKUForTargetReports> skuForTargetReports)
        {
            var ossByConsumption = GetSkus(ref skuForTargetReports, [OverPredicateConsumptionByOutgoingProducts, OverPredicateConsumptionByTransfer]);
            overspendingSkusByConsumption = ossByConsumption is not null ? new LinkedList<SKUForTargetReports>(ossByConsumption.OrderByDescending(el => el.SKUNumber)) : null;
            isPossibilityOverspendingByConsumption = overspendingSkusByConsumption is not null && overspendingSkusByConsumption.Count != 0;

            var ussByConsumption = GetSkus(ref skuForTargetReports, [UnderPredicateByConsumption]);
            underspendingSkusByConsumption = ussByConsumption is not null ? new LinkedList<SKUForTargetReports>(ussByConsumption.OrderByDescending(el => el.SKUNumber)) : null;
            isNeedUnderspendingByConsumption = underspendingSkusByConsumption is not null && underspendingSkusByConsumption.Count != 0;

            var ossByReceipt = GetSkus(ref skuForTargetReports, [OverPredicateByReceipt]);
            overspendingSkusByReceipt = ossByReceipt is not null ? new LinkedList<SKUForTargetReports>(ossByReceipt.OrderByDescending(el => el.SKUNumber)) : null;
            isPossibilityOverspendingByReceipt = overspendingSkusByReceipt is not null && overspendingSkusByReceipt.Count != 0;

            var ussByReceipt = GetSkus(ref skuForTargetReports, [UnderPredicateByReceipt]);
            underspendingSkusByReceipt = ussByReceipt is not null ? new LinkedList<SKUForTargetReports>(ussByReceipt.OrderByDescending(el => el.SKUNumber)) : null;
            isNeedUnderspendingByReceipt = underspendingSkusByReceipt is not null && underspendingSkusByReceipt.Count != 0;

            var extraByReceipt = GetSkus(ref skuForTargetReports, [ExtraPredicateByReceipt]);
            extraSkusByReceipt = extraByReceipt?.OrderByDescending(el => el.SKUNumber).ToList();
            isThereAreSkusWithExtraReceipt = extraSkusByReceipt is not null && extraSkusByReceipt.Count != 0;

            return Task.CompletedTask;
        }

        public static Task SetAutoResorts(ref List<SKUForTargetReports> skuForTargetReports, SKUResortTarget resortTarget)
        {
            // Initial set collections
            var updateDataTask = UpdateDataForResorts(ref skuForTargetReports);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            bool? workFlag = resortTarget switch 
            {
                SKUResortTarget.ResortingConsumptionByOutgoing or SKUResortTarget.ResortingConsumptionByTransfer => 
                    isNeedUnderspendingByConsumption && isPossibilityOverspendingByConsumption,
                SKUResortTarget.ResortingReceipt => isNeedUnderspendingByReceipt && isPossibilityOverspendingByReceipt,
                _ => null
            };

            LinkedList<SKUForTargetReports>? ossWorkList = resortTarget switch  
            {
                SKUResortTarget.ResortingConsumptionByOutgoing or SKUResortTarget.ResortingConsumptionByTransfer => overspendingSkusByConsumption,
                SKUResortTarget.ResortingReceipt => overspendingSkusByReceipt,
                _ => null
            };

             LinkedList<SKUForTargetReports>? ussWorkList = resortTarget switch  
            {
                SKUResortTarget.ResortingConsumptionByOutgoing or SKUResortTarget.ResortingConsumptionByTransfer => underspendingSkusByConsumption,
                SKUResortTarget.ResortingReceipt => underspendingSkusByReceipt,
                _ => null

            };

            if (workFlag != null && (bool)workFlag && ossWorkList != null && ussWorkList != null)
            {
                var overNode = ossWorkList?.First;
                var underNode = ussWorkList?.First;

                while (underNode != null && overNode != null)
                {
                    var overSku = overNode.Value;
                    var underSku = underNode.Value;

                    bool zeroСondition = resortTarget switch 
                    {
                        SKUResortTarget.ResortingConsumptionByOutgoing or SKUResortTarget.ResortingConsumptionByTransfer => overSku.MoreMutualSettlementThanWaiting == 0,
                        SKUResortTarget.ResortingReceipt => overSku.MoreReceiptThanWaiting == 0,
                        _ => true
                    };

                    if (zeroСondition)
                    {
                        overNode = overNode.Next;
                        continue;
                    }

                    int result = 0;
                    switch(resortTarget) 
                    {
                        case SKUResortTarget.ResortingConsumptionByOutgoing or SKUResortTarget.ResortingConsumptionByTransfer:
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

                            SKUResortTarget? skuConsuptionResortTarget = (OverPredicateConsumptionByOutgoingProducts(overSku), OverPredicateConsumptionByTransfer(overSku)) switch 
                            {
                                (true, true) or (true, false) => SKUResortTarget.ResortingConsumptionByOutgoing,
                                (false, true) => SKUResortTarget.ResortingConsumptionByTransfer,
                                (false, false) => null
                            };

                            if(skuConsuptionResortTarget is not null) 
                            {
                                var target = (SKUResortTarget)skuConsuptionResortTarget;
                                int underConsumtionSkuIndex = skuForTargetReports.IndexOf(underSku);
                                int overConsumtionSkuIndex = skuForTargetReports.IndexOf(overSku);
                            
                                MakeResort(ref skuForTargetReports, target, underConsumtionSkuIndex, overConsumtionSkuIndex, result);
                            }

                            break;

                        case SKUResortTarget.ResortingReceipt:
                            if (overSku.MoreReceiptThanWaiting >= underSku.LessReceiptThanWaiting)
                            {
                                result = underSku.LessReceiptThanWaiting;
                                underNode = underNode.Next;
                            }
                            else
                            {
                                result = overSku.MoreReceiptThanWaiting;
                                overNode = overNode.Next;
                            }

                            int underReceiptSkuIndex = skuForTargetReports.IndexOf(underSku);
                            int overReceiptSkuIndex = skuForTargetReports.IndexOf(overSku);
                            
                            MakeResort(ref skuForTargetReports, resortTarget, underReceiptSkuIndex, overReceiptSkuIndex, result);
                            break;
                        default:
                            break;
                    } 
                }
            }

            // Initial set collections
            updateDataTask = UpdateDataForResorts(ref skuForTargetReports);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            return Task.CompletedTask;
        }

        public static Task FixExtraReceipts(ref List<SKUForTargetReports> skuForTargetReports) 
        {
            // Initial set collections
            var updateDataTask = UpdateDataForResorts(ref skuForTargetReports);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            if (isThereAreSkusWithExtraReceipt)
            {
                foreach (var sku in extraSkusByReceipt!)
                {
                    int sum = sku.SKUReceiptTotal - sku.SKURansomsWaiting;
                    int skuIndex = skuForTargetReports.IndexOf(sku);

                    MakeResort(ref skuForTargetReports, SKUResortTarget.ResortingReceipt, skuIndex, skuIndex, sum);
                }
            }

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
            if (underSkuIndex != overSkuIndex && target == SKUResortTarget.ResortingReceipt) 
            {
                skuForTargetReports[underSkuIndex].MakeResort(target, SKUResortDirection.Incoming, skuForTargetReports[overSkuIndex].SKUNumber, sum);
            }

            // Списание экстра приходов
            if (underSkuIndex == overSkuIndex && target == SKUResortTarget.ResortingReceipt) 
            {
                skuForTargetReports[underSkuIndex].MakeResort(target, SKUResortDirection.Internal, skuForTargetReports[overSkuIndex].SKUNumber, sum);
            }
        }

        public static Task FinalAnalizeSkus(ref List<SKUForTargetReports> skuForTargetReports) 
        {
            foreach (var sku in skuForTargetReports)
            {
                sku.FinalAnalize();
            }

            return Task.CompletedTask;
        }

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