using Microsoft.Extensions.Configuration;
using TableAnalizer.Objects.SKUItems;
using TableAnalizer.Objects.SKUItems.SKUForTargetReports;

namespace TableAnalizer.Objects.Managers
{
    public class AnalyticalManager(IConfiguration config) : BaseManager(config)
    {
        #region Fields
        
        /// <summary>
        /// Предикат: артикул параллельных клинтов и ожидание меньше общего прихода
        /// </summary>
        private static Func<SKUForTargetReport, bool> ParallelClientSkuWithExtraReceiptPredicate => sku => sku.IsParallelClientsSKU && sku.SKURansomsWaiting < sku.SKUReceiptTotal;

        private static Func<SKUForTargetReport, bool> OverPredicateConsumptionByOutgoingProducts => sku => !sku.IsSkuNormalize && sku.OverMutualSettlement > 0 && int.IsPositive(sku.SKUOutgoingProducts - sku.OverMutualSettlement);

        private static Func<SKUForTargetReport, bool> OverPredicateConsumptionByTransfer => sku => !sku.IsSkuNormalize && sku.OverMutualSettlement > 0 && int.IsPositive(sku.SKUTransfered - sku.OverMutualSettlement);

        private static Func<SKUForTargetReport, bool> UnderPredicateByConsumption => sku => !sku.IsSkuNormalize && sku.UnderMutualSettlement > 0;

        private static Func<SKUForTargetReport, bool> OverPredicateByReceipt=> sku => !sku.IsSkuNormalize && sku.OverReceipt > 0 && int.IsPositive(sku.SKUReceiptGoods - sku.OverReceipt);

        private static Func<SKUForTargetReport, bool> UnderPredicateByReceipt => sku => !sku.IsSkuNormalize && sku.UnderReceipt > 0;

        /// <summary>
        /// Артикул не номализован, взаиморасчет + остаток = ожидание и общий приход больше ожидания
        /// </summary>
        private static Func<SKUForTargetReport, bool> ExtraPredicateByReceipt => sku => 
            !sku.IsSkuNormalize && 
            (sku.SKUSummarySettlement + sku.SKURemaining) == sku.SKURansomsWaiting && 
            sku.SKUReceiptTotal > sku.SKURansomsWaiting;

        private static Func<SKUForTargetReport, bool> ExtraPredicateByReceiptFinal => 
            sku => !sku.IsSkuNormalize && 
            sku.SKUReceiptTotal > sku.SKURansomsWaiting;


        /// <summary>
        /// Были списания недостач точек через склад
        /// </summary>
        private static Func<SKUForTargetReport, bool> WrittenOffFromPointsViaWarehouseByShortage => 
            sku => sku.SKUWriteOffFromPointsViaWarehouseShortage > 0;

        /// <summary>
        /// Были списания корректировки от точек через склад
        /// </summary>
        private static Func<SKUForTargetReport, bool> WrittenOffFromPointsViaWarehouseByAdjustment => 
            sku => sku.SKUWrittenOffFromPointsViaWarehouseAdjustment > 0;

        /// <summary>
        /// Артикулы с перерасходами (по расходу)
        /// </summary>
        private static LinkedList<SKUForTargetReport>? overspendingSkusByConsumption;

        /// <summary>
        ///  Артикулы с недорасходами (по расходу)
        /// </summary>
        private static LinkedList<SKUForTargetReport>? underspendingSkusByConsumption;

        /// <summary>
        /// Артикулы с перерасходами (по приходу)
        /// </summary>
        private static LinkedList<SKUForTargetReport>? overspendingSkusByReceipt;

        /// <summary>
        ///  Артикулы с недорасходами (по приходу)
        /// </summary>
        private static LinkedList<SKUForTargetReport>? underspendingSkusByReceipt;

        /// <summary>
        ///  Артикулы со списанием недостач с точек через склад
        /// </summary>
        private static List<SKUForTargetReport>? writeOffPointsByShortage;

        /// <summary>
        ///  Артикулы со списанием корректировок с точек через склад
        /// </summary>
        private static List<SKUForTargetReport>? writeOffPointsByAdjustment;

        private static List<SKUForTargetReport>? parallelClientSkuWithExtraReceipt;


        /// <summary>
        ///  Артикулы с недорасходами (по приходу)
        /// </summary>
        //private static LinkedList<SKUForTargetReports>? underspendingSkusByReceipt; // WrittenOffFromPointsViaWarehouseByReceipt
 
        /// <summary>
        ///  Артикулы с экстра приходом=
        /// </summary>
        private static List<SKUForTargetReport>? extraSkusByReceipt;
        private static bool isNeedUnderspendingByConsumption;
        private static bool isPossibilityOverspendingByConsumption;

        /// <summary>
        /// Есть артикулы с недорасходами (по приходу)
        /// </summary>
        private static bool isNeedUnderspendingByReceipt;
        /// <summary>
        /// Есть артикулы с перерасходами (по приходу)
        /// </summary>
        private static bool isPossibilityOverspendingByReceipt;
        /// <summary>
        /// Есть артикулы с экстра-приходом
        /// </summary>
        private static bool isThereAreSkusWithExtraReceipt;

        /// <summary>
        /// Есть списания недостач от точек через склад (прошлые периоды)
        /// </summary>
        private static bool isWriteOffPointsThroughWarehouseShortages;
        /// <summary>
        /// Есть корректировка данных от точек через склад (прошлые периоды)
        /// </summary>
        private static bool isWriteOffPointsThroughWarehouseAdjustment;

        /// <summary>
        /// Флаг: Есть артикулы параллельных клиентов где ожидание меньше общего прихода.
        /// </summary>
        private static bool isParallelClientSkuWithExtraReceipt;

        #endregion

        #region Properties 
        private static int PaidWriteOffRemainder {get;set;} = 0; 
        #endregion

        #region Functionality

        private static Task UpdateDataForResorts(ref List<SKUForTargetReport> skuForTargetReports)
        {
            parallelClientSkuWithExtraReceipt = GetSkus(ref skuForTargetReports, [ParallelClientSkuWithExtraReceiptPredicate])?.OrderByDescending(el => el.SKUNumber).ToList();;
            isParallelClientSkuWithExtraReceipt = parallelClientSkuWithExtraReceipt is not null && parallelClientSkuWithExtraReceipt.Count > 0;

            writeOffPointsByShortage = GetSkus(ref skuForTargetReports, [WrittenOffFromPointsViaWarehouseByShortage])?.OrderByDescending(el => el.SKUNumber).ToList();;
            isWriteOffPointsThroughWarehouseShortages = writeOffPointsByShortage is not null && writeOffPointsByShortage.Count > 0;

            writeOffPointsByAdjustment = GetSkus(ref skuForTargetReports, [WrittenOffFromPointsViaWarehouseByAdjustment])?.OrderByDescending(el => el.SKUNumber).ToList();;
            isWriteOffPointsThroughWarehouseAdjustment = writeOffPointsByAdjustment is not null && writeOffPointsByAdjustment.Count > 0;

            var ossByConsumption = GetSkus(ref skuForTargetReports, [OverPredicateConsumptionByOutgoingProducts, OverPredicateConsumptionByTransfer]);
            overspendingSkusByConsumption = ossByConsumption is not null ? new LinkedList<SKUForTargetReport>(ossByConsumption.OrderByDescending(el => el.SKUNumber)) : null;
            isPossibilityOverspendingByConsumption = overspendingSkusByConsumption is not null && overspendingSkusByConsumption.Count != 0;

            var ussByConsumption = GetSkus(ref skuForTargetReports, [UnderPredicateByConsumption]);
            underspendingSkusByConsumption = ussByConsumption is not null ? new LinkedList<SKUForTargetReport>(ussByConsumption.OrderByDescending(el => el.SKUNumber)) : null;
            isNeedUnderspendingByConsumption = underspendingSkusByConsumption is not null && underspendingSkusByConsumption.Count != 0;

            var ossByReceipt = GetSkus(ref skuForTargetReports, [OverPredicateByReceipt]);
            overspendingSkusByReceipt = ossByReceipt is not null ? new LinkedList<SKUForTargetReport>(ossByReceipt.OrderByDescending(el => el.SKUNumber)) : null;
            isPossibilityOverspendingByReceipt = overspendingSkusByReceipt is not null && overspendingSkusByReceipt.Count != 0;

            var ussByReceipt = GetSkus(ref skuForTargetReports, [UnderPredicateByReceipt]);
            underspendingSkusByReceipt = ussByReceipt is not null ? new LinkedList<SKUForTargetReport>(ussByReceipt.OrderByDescending(el => el.SKUNumber)) : null;
            isNeedUnderspendingByReceipt = underspendingSkusByReceipt is not null && underspendingSkusByReceipt.Count != 0;

            var extraByReceipt = GetSkus(ref skuForTargetReports, [ExtraPredicateByReceipt])?.OrderByDescending(el => el.SKUNumber).ToList();
            isThereAreSkusWithExtraReceipt = extraSkusByReceipt is not null && extraSkusByReceipt.Count != 0;

            return Task.CompletedTask;
        }

        public static Task SetAutoResorts(ref List<SKUForTargetReport> skuForTargetReports, SKUResortTarget resortTarget)
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

            LinkedList<SKUForTargetReport>? ossWorkList = resortTarget switch  
            {
                SKUResortTarget.ResortingConsumptionByOutgoing or SKUResortTarget.ResortingConsumptionByTransfer => overspendingSkusByConsumption,
                SKUResortTarget.ResortingReceipt => overspendingSkusByReceipt,
                _ => null
            };

            LinkedList<SKUForTargetReport>? ussWorkList = resortTarget switch  
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
                        SKUResortTarget.ResortingConsumptionByOutgoing or SKUResortTarget.ResortingConsumptionByTransfer => overSku.OverMutualSettlement == 0,
                        SKUResortTarget.ResortingReceipt => overSku.OverReceipt == 0,
                        _ => true
                    };

                    if (zeroСondition)
                    {
                        overNode = overNode.Next;
                        continue;
                    }

                    int resortSum = 0;
                    switch(resortTarget) 
                    {
                        case SKUResortTarget.ResortingConsumptionByOutgoing or SKUResortTarget.ResortingConsumptionByTransfer:
                            if (overSku.OverMutualSettlement >= underSku.UnderMutualSettlement)
                            {
                                resortSum = underSku.UnderMutualSettlement;
                                underNode = underNode.Next;
                            }
                            else
                            {
                                resortSum = overSku.OverMutualSettlement;
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
                            
                                MakeResort(ref skuForTargetReports, target, underConsumtionSkuIndex, overConsumtionSkuIndex, resortSum);
                            }

                            break;

                        case SKUResortTarget.ResortingReceipt:
                            // Экстра-приход больше недоприхода
                            if (overSku.OverReceipt >= underSku.UnderReceipt)
                            {
                                resortSum = underSku.UnderReceipt;
                                underNode = underNode.Next;
                            }
                            // Экстра-приход меньше недоприхода
                            else
                            {
                                resortSum = overSku.OverReceipt;
                                overNode = overNode.Next;
                            }

                            int underReceiptSkuIndex = skuForTargetReports.IndexOf(underSku);
                            int overReceiptSkuIndex = skuForTargetReports.IndexOf(overSku);
                            
                            MakeResort(ref skuForTargetReports, resortTarget, underReceiptSkuIndex, overReceiptSkuIndex, resortSum);
                            
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

        public static Task FixWriteOffByPoints(ref List<SKUForTargetReport> skuForTargetReports) 
        {
            // Initial set collections
            var updateDataTask = UpdateDataForResorts(ref skuForTargetReports);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            if (isWriteOffPointsThroughWarehouseShortages)
            {
                foreach (var sku in writeOffPointsByShortage!)
                {
                    MakeCorrectionWriteOffs(ref skuForTargetReports, WriteOffSource.PointsViaWarehouseShortage, skuForTargetReports.IndexOf(sku));
                }
            }

            if (isWriteOffPointsThroughWarehouseAdjustment)
            {
                foreach (var sku in writeOffPointsByAdjustment!)
                {
                    MakeCorrectionWriteOffs(ref skuForTargetReports, WriteOffSource.PointsViaWarehouseAdjustment, skuForTargetReports.IndexOf(sku));
                }
            }

            return Task.CompletedTask;
        }

        public static Task FixExtraReceipts(ref List<SKUForTargetReport> skuForTargetReports) 
        {
            // Initial set collections
            var updateDataTask = UpdateDataForResorts(ref skuForTargetReports);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            if (isWriteOffPointsThroughWarehouseShortages)
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

        public static void MakeCorrectionWriteOffs(ref List<SKUForTargetReport> skuForTargetReports, WriteOffSource writeOffSource, int skuIndex) 
        {
            if(int.IsNegative(skuIndex)) return;

            var paidWriteOffRemainder = skuForTargetReports[skuIndex].MakeCorrectionOnWriteOffShortages(writeOffSource);
            if(paidWriteOffRemainder > 0) 
            {
                PaidWriteOffRemainder += paidWriteOffRemainder;
            }

            // if(writeOffSource == WriteOffSource.PointsViaWarehouseShortage && values.Item2 > 0) 
            // {
            //     throw new NotImplementedException($"Use it: {values.Item2}!");
            // }
        }

        private static void MakeResort(ref List<SKUForTargetReport> skuForTargetReports, SKUResortTarget target, int underSkuIndex, int overSkuIndex, int sum)
        {
            if(int.IsNegative(underSkuIndex) || int.IsNegative(overSkuIndex)) 
            {
                return;
            } 

            bool isIndicesEqual = underSkuIndex == overSkuIndex;
            bool isIndicesNotEqual = underSkuIndex != overSkuIndex;

             // Пересорты приходов (артикулы параллельных клиентов)
            if (skuForTargetReports[underSkuIndex].IsParallelClientsSKU && 
                isIndicesEqual && target == SKUResortTarget.ResortingReceipt) 
            {
                skuForTargetReports[underSkuIndex].MakeResort(target, SKUResortDirection.InternalParallel, skuForTargetReports[overSkuIndex].SKUNumber, sum);
            }


            // Пересорты расходов
            if(isIndicesNotEqual && 
                (target == SKUResortTarget.ResortingConsumptionByOutgoing || target == SKUResortTarget.ResortingConsumptionByTransfer)) 
            {
                skuForTargetReports[underSkuIndex].MakeResort(target, SKUResortDirection.Incoming, skuForTargetReports[overSkuIndex].SKUNumber, sum);
                skuForTargetReports[overSkuIndex].MakeResort(target, SKUResortDirection.Outgoing, skuForTargetReports[underSkuIndex].SKUNumber, sum);
            }

            // Пересорты приходов
            if (isIndicesNotEqual 
                && target == SKUResortTarget.ResortingReceipt) 
            {
                skuForTargetReports[underSkuIndex].MakeResort(target, SKUResortDirection.Incoming, skuForTargetReports[overSkuIndex].SKUNumber, sum);
                skuForTargetReports[overSkuIndex].MakeResort(target, SKUResortDirection.Outgoing, skuForTargetReports[underSkuIndex].SKUNumber, sum);
            }

            // Списание экстра приходов
            if (isIndicesEqual && target == SKUResortTarget.ResortingReceipt) 
            {
                skuForTargetReports[underSkuIndex].MakeResort(target, SKUResortDirection.Internal, skuForTargetReports[overSkuIndex].SKUNumber, sum);
            }
        }

        public static Task FinalAnalizeSkus(ref List<SKUForTargetReport> skuForTargetReports) 
        {
            foreach (var sku in skuForTargetReports)
            {
                if(!sku.IsSkuNormalize && PaidWriteOffRemainder > 0) 
                {
                    var reminder = sku.FinalAnalize(PaidWriteOffRemainder);
                    PaidWriteOffRemainder = reminder;
                } 
                else 
                {
                    sku.FinalAnalize(0);
                }
            }

            return Task.CompletedTask;
        }

        private static IEnumerable<SKUForTargetReport>? GetSkus(ref List<SKUForTargetReport> skuForTargetReports, List<Func<SKUForTargetReport, bool>> skuPredicates)
        {
            return from skuForTarget in skuForTargetReports
                   from predicate in skuPredicates
                   where predicate(skuForTarget)
                   select skuForTarget;
        }

        public static Task FixReceiptBySKUsOfParallelClients(ref List<SKUForTargetReport> skuForTargetReports)
        {
            // Initial set collections
            var updateDataTask = UpdateDataForResorts(ref skuForTargetReports);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            if (isParallelClientSkuWithExtraReceipt)
            {
                foreach (var sku in parallelClientSkuWithExtraReceipt!)
                {
                    int sum = sku.SKUReceiptTotal - sku.SKURansomsWaiting;
                    int skuIndex = skuForTargetReports.IndexOf(sku);

                    MakeResort(ref skuForTargetReports, SKUResortTarget.ResortingReceipt, skuIndex, skuIndex, sum);
                }
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}