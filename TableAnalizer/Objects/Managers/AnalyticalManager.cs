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

        private static Func<SKUForTargetReport, bool> OverPredicateConsumptionByOutgoingProducts => sku =>
            !sku.IsSkuNormalize
            && sku.OverMutualSettlement > 0 &&
            int.IsPositive(sku.SKUShipmentGoods - sku.OverMutualSettlement);

        private static Func<SKUForTargetReport, bool> OverPredicateConsumptionByOutgoingProductsWithoutProgress => sku =>
            sku.isDeadline &&
            !sku.IsSkuNormalize
            && sku.OverMutualSettlement > 0 &&
            int.IsPositive(sku.SKUShipmentGoods - sku.OverMutualSettlement);

        private static Func<SKUForTargetReport, bool> OverPredicateConsumptionByTransfer => sku =>
            !sku.IsSkuNormalize &&
            sku.OverMutualSettlement > 0 &&
            int.IsPositive(sku.SKUTransfered - sku.OverMutualSettlement);

        private static Func<SKUForTargetReport, bool> OverPredicateConsumptionByTransferWithoutProgress => sku =>
            sku.isDeadline &&
            !sku.IsSkuNormalize &&
            sku.OverMutualSettlement > 0 &&
            int.IsPositive(sku.SKUTransfered - sku.OverMutualSettlement);

        private static Func<SKUForTargetReport, bool> UnderPredicateByConsumption => sku =>
            !sku.IsSkuNormalize && sku.UnderMutualSettlement > 0;

        private static Func<SKUForTargetReport, bool> UnderPredicateByConsumptionWithoutProgress => sku =>
            sku.isDeadline &&
            !sku.IsSkuNormalize &&
            sku.UnderMutualSettlement > 0;

        private static Func<SKUForTargetReport, bool> OverPredicateByReceipt => sku =>
            !sku.IsSkuNormalize &&
            sku.OverReceipt > 0 &&
            int.IsPositive(sku.SKUReceiptGoods - sku.OverReceipt);

        private static Func<SKUForTargetReport, bool> OverPredicateByReceiptWithotProgress => sku =>
            sku.isDeadline &&
            !sku.IsSkuNormalize &&
            sku.OverReceipt > 0 &&
            int.IsPositive(sku.SKUReceiptGoods - sku.OverReceipt);

        private static Func<SKUForTargetReport, bool> UnderPredicateByReceipt => sku =>
            !sku.IsSkuNormalize &&
            sku.UnderReceipt > 0;

        private static Func<SKUForTargetReport, bool> UnderPredicateByReceiptWithoutProgress => sku =>
            sku.isDeadline &&
            !sku.IsSkuNormalize &&
            sku.UnderReceipt > 0;

        /// <summary>
        /// Артикул не нормализован, взаиморасчет + остаток = ожидание и общий приход больше ожидания
        /// </summary>
        private static Func<SKUForTargetReport, bool> ExtraPredicateByReceipt => sku =>
            !sku.IsSkuNormalize &&
            (sku.SKUSummarySettlement + sku.SKURemaining) == sku.SKURansomsWaiting &&
            sku.SKUReceiptTotal > sku.SKURansomsWaiting;

        private static Func<SKUForTargetReport, bool> ExtraPredicateByReceiptWithoutProgress => sku =>
            sku.isDeadline &&
            !sku.IsSkuNormalize &&
            (sku.SKUSummarySettlement + sku.SKURemaining) == sku.SKURansomsWaiting &&
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

        /// <summary>
        /// Список артикулов параллельных клиентов с экстра-приходами.
        /// </summary>
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

        /// <summary>
        /// Предикат для получения артикулов, дедлайн по которым не прошел.
        /// </summary>
        private static Func<SKUForTargetReport, bool> IsDontConsiderInProgress => sku => !sku.isDeadline;

        #endregion

        #region Properties 
        private static int PaidWriteOffRemainder { get; set; } = 0;
        #endregion

        #region Functionality

        /// <summary>
        /// Обновляет внутренние коллекции данных. 
        /// </summary>
        /// <param name="skuForTargetReports">Ссылка на рабочую коллекцию.</param>
        /// <param name="isDontConsiderSKUsInProgress">Флаг - не учитывать артикулы с неистекшим дедлайном."</param>
        /// <returns>Флаг выполнения задачи.</returns>
        private static Task UpdateDataForResorts(ref List<SKUForTargetReport> skuForTargetReports, bool isDontConsiderSKUsInProgress)
        {
            parallelClientSkuWithExtraReceipt = GetSkus(ref skuForTargetReports, [ParallelClientSkuWithExtraReceiptPredicate])?.OrderByDescending(el => el.SKUNumber).ToList(); ;
            isParallelClientSkuWithExtraReceipt = parallelClientSkuWithExtraReceipt is not null && parallelClientSkuWithExtraReceipt.Count > 0;

            writeOffPointsByShortage = GetSkus(ref skuForTargetReports, [WrittenOffFromPointsViaWarehouseByShortage])?.OrderByDescending(el => el.SKUNumber).ToList(); ;
            isWriteOffPointsThroughWarehouseShortages = writeOffPointsByShortage is not null && writeOffPointsByShortage.Count > 0;

            writeOffPointsByAdjustment = GetSkus(ref skuForTargetReports, [WrittenOffFromPointsViaWarehouseByAdjustment])?.OrderByDescending(el => el.SKUNumber).ToList(); ;
            isWriteOffPointsThroughWarehouseAdjustment = writeOffPointsByAdjustment is not null && writeOffPointsByAdjustment.Count > 0;

            var overspendingByConsumption = isDontConsiderSKUsInProgress ?
                GetSkus(ref skuForTargetReports, [OverPredicateConsumptionByOutgoingProductsWithoutProgress, OverPredicateConsumptionByTransferWithoutProgress]) :
                GetSkus(ref skuForTargetReports, [OverPredicateConsumptionByOutgoingProducts, OverPredicateConsumptionByTransfer]);
            overspendingSkusByConsumption = overspendingByConsumption is not null ? new LinkedList<SKUForTargetReport>(overspendingByConsumption.OrderByDescending(el => el.SKUNumber)) : null;
            isPossibilityOverspendingByConsumption = overspendingSkusByConsumption is not null && overspendingSkusByConsumption.Count != 0;

            var underspendingByConsumption = isDontConsiderSKUsInProgress ?
                GetSkus(ref skuForTargetReports, [UnderPredicateByConsumptionWithoutProgress]) :
                GetSkus(ref skuForTargetReports, [UnderPredicateByConsumption]);
            underspendingSkusByConsumption = underspendingByConsumption is not null ? new LinkedList<SKUForTargetReport>(underspendingByConsumption.OrderByDescending(el => el.SKUNumber)) : null;
            isNeedUnderspendingByConsumption = underspendingSkusByConsumption is not null && underspendingSkusByConsumption.Count != 0;

            var overspendingByReceipt = isDontConsiderSKUsInProgress ?
                GetSkus(ref skuForTargetReports, [OverPredicateByReceiptWithotProgress]) :
                GetSkus(ref skuForTargetReports, [OverPredicateByReceipt]);
            overspendingSkusByReceipt = overspendingByReceipt is not null ? new LinkedList<SKUForTargetReport>(overspendingByReceipt.OrderByDescending(el => el.SKUNumber)) : null;
            isPossibilityOverspendingByReceipt = overspendingSkusByReceipt is not null && overspendingSkusByReceipt.Count != 0;

            var underspendingByReceipt = isDontConsiderSKUsInProgress ?
                GetSkus(ref skuForTargetReports, [UnderPredicateByReceiptWithoutProgress]) :
                GetSkus(ref skuForTargetReports, [UnderPredicateByReceipt]);
            underspendingSkusByReceipt = underspendingByReceipt is not null ? new LinkedList<SKUForTargetReport>(underspendingByReceipt.OrderByDescending(el => el.SKUNumber)) : null;
            isNeedUnderspendingByReceipt = underspendingSkusByReceipt is not null && underspendingSkusByReceipt.Count != 0;

            extraSkusByReceipt = isDontConsiderSKUsInProgress ?
                GetSkus(ref skuForTargetReports, [ExtraPredicateByReceiptWithoutProgress])?.OrderByDescending(el => el.SKUNumber).ToList() :
                GetSkus(ref skuForTargetReports, [ExtraPredicateByReceipt])?.OrderByDescending(el => el.SKUNumber).ToList();
            isThereAreSkusWithExtraReceipt = extraSkusByReceipt is not null && extraSkusByReceipt.Count != 0;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Устанавливает автоматические пересорты.
        /// </summary>
        /// <param name="skuForTargetReports">Рабочая коллекция SKUForTargetReport.</param>
        /// <param name="resortTarget">Напрпвление пересорта.</param>
        /// <param name="isDontConsiderInProgress">Не учитывать артикулы, дедлайн по которым еще не прошел ("в работе").</param>
        /// <returns>Флаг выполнения задачи.</returns>
        public static Task SetAutoResorts(ref List<SKUForTargetReport> skuForTargetReports, ResortConsumptionTarget resortTarget, bool isDontConsiderInProgress)
        {
            // Initial set collections
            var updateDataTask = UpdateDataForResorts(ref skuForTargetReports, isDontConsiderInProgress);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            bool? workFlag = resortTarget switch
            {
                ResortConsumptionTarget.ResortingConsumptionByOutgoing or ResortConsumptionTarget.ResortingConsumptionByTransfer =>
                    isNeedUnderspendingByConsumption && isPossibilityOverspendingByConsumption,
                ResortConsumptionTarget.ResortingReceipt => isNeedUnderspendingByReceipt && isPossibilityOverspendingByReceipt,
                _ => null
            };

            LinkedList<SKUForTargetReport>? overspendingList = DefinesListWithOverspending(resortTarget);
            LinkedList<SKUForTargetReport>? underspandingList = DefinesListWithUnderspanding(resortTarget);

            if (workFlag != null && (bool)workFlag && overspendingList != null && underspandingList != null)
            {
                var overNode = overspendingList?.First;
                var underNode = underspandingList?.First;

                while (underNode != null && overNode != null)
                {
                    var overSku = overNode.Value;
                    var underSku = underNode.Value;

                    bool zeroСondition = resortTarget switch
                    {
                        ResortConsumptionTarget.ResortingConsumptionByOutgoing or ResortConsumptionTarget.ResortingConsumptionByTransfer => overSku.OverMutualSettlement == 0,
                        ResortConsumptionTarget.ResortingReceipt => overSku.OverReceipt == 0,
                        _ => true
                    };

                    if (zeroСondition)
                    {
                        overNode = overNode.Next;
                        continue;
                    }

                    int resortSum = 0;
                    switch (resortTarget)
                    {
                        case ResortConsumptionTarget.ResortingConsumptionByOutgoing or ResortConsumptionTarget.ResortingConsumptionByTransfer:
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

                            ResortConsumptionTarget? skuConsuptionResortTarget = (OverPredicateConsumptionByOutgoingProducts(overSku), OverPredicateConsumptionByTransfer(overSku)) switch
                            {
                                (true, true) or (true, false) => ResortConsumptionTarget.ResortingConsumptionByOutgoing,
                                (false, true) => ResortConsumptionTarget.ResortingConsumptionByTransfer,
                                (false, false) => null
                            };

                            if (skuConsuptionResortTarget is not null)
                            {
                                var target = (ResortConsumptionTarget)skuConsuptionResortTarget;
                                int underConsumtionSkuIndex = skuForTargetReports.IndexOf(underSku);
                                int overConsumtionSkuIndex = skuForTargetReports.IndexOf(overSku);

                                MakeResort(ref skuForTargetReports, target, underConsumtionSkuIndex, overConsumtionSkuIndex, resortSum);
                            }

                            break;

                        case ResortConsumptionTarget.ResortingReceipt:
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
            updateDataTask = UpdateDataForResorts(ref skuForTargetReports, isDontConsiderInProgress);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            if (workFlag != null && (bool)workFlag && resortTarget != ResortConsumptionTarget.ResortingReceipt)
            {
                return SetAutoResorts(ref skuForTargetReports, resortTarget, false);
            }

            return Task.CompletedTask;
        }

        private static LinkedList<SKUForTargetReport>? DefinesListWithUnderspanding(ResortConsumptionTarget resortTarget)
        {
            return resortTarget switch
            {
                ResortConsumptionTarget.ResortingConsumptionByOutgoing or ResortConsumptionTarget.ResortingConsumptionByTransfer => underspendingSkusByConsumption,
                ResortConsumptionTarget.ResortingReceipt => underspendingSkusByReceipt,
                _ => null
            };
        }

        /// <summary>   
        /// Defines the list with overspending.
        /// </summary>
        /// <param name="resortTarget">Resort target case.</param>
        /// <returns>List with overspending (optional).</returns>
        private static LinkedList<SKUForTargetReport>? DefinesListWithOverspending(ResortConsumptionTarget resortTarget)
        {
            return resortTarget switch
            {
                ResortConsumptionTarget.ResortingConsumptionByOutgoing or ResortConsumptionTarget.ResortingConsumptionByTransfer => overspendingSkusByConsumption,
                ResortConsumptionTarget.ResortingReceipt => overspendingSkusByReceipt,
                _ => null
            };
        }

        /// <summary>
        /// Фиксация недовозов, списанных с точек через склад.
        /// </summary>
        /// <param name="skuForTargetReports">Коллекция SKUForTargetReport (ссылка).</param>
        /// <returns></returns>
        public static Task FixWriteOffByPointsThroughWarehouse(ref List<SKUForTargetReport> skuForTargetReports)
        {
            // Initial set collections
            var updateDataTask = UpdateDataForResorts(ref skuForTargetReports, false);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            if (isWriteOffPointsThroughWarehouseShortages)
            {
                foreach (var sku in writeOffPointsByShortage!)
                {
                    MakeCorrectionWriteOffs(ref skuForTargetReports, SourceReportItemKey.WriteOffPointsViaWarehouseShortage, skuForTargetReports.IndexOf(sku));
                }
            }

            if (isWriteOffPointsThroughWarehouseAdjustment)
            {
                foreach (var sku in writeOffPointsByAdjustment!)
                {
                    MakeCorrectionWriteOffs(ref skuForTargetReports, SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment, skuForTargetReports.IndexOf(sku));
                }
            }

            return Task.CompletedTask;
        }

        public static Task FixExtraReceipts(ref List<SKUForTargetReport> skuForTargetReports, bool isDontConsiderInProgress)
        {
            // Initial set collections
            var updateDataTask = UpdateDataForResorts(ref skuForTargetReports, isDontConsiderInProgress);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            if (isThereAreSkusWithExtraReceipt)
            {
                foreach (var sku in extraSkusByReceipt!)
                {
                    int sum = sku.SKUReceiptTotal - sku.SKURansomsWaiting;
                    int skuIndex = skuForTargetReports.IndexOf(sku);

                    MakeResort(ref skuForTargetReports, ResortConsumptionTarget.ResortingReceipt, skuIndex, skuIndex, sum);
                }
            }

            return isThereAreSkusWithExtraReceipt ?
                FixExtraReceipts(ref skuForTargetReports, false) :
                Task.CompletedTask;
        }

        public static void MakeCorrectionWriteOffs(ref List<SKUForTargetReport> skuForTargetReports, SourceReportItemKey writeOffSource, int skuIndex)
        {
            if (int.IsNegative(skuIndex)) return;

            var paidWriteOffRemainder = skuForTargetReports[skuIndex].MakeCorrectionOnWriteOffShortages(writeOffSource);
            if (paidWriteOffRemainder > 0)
            {
                PaidWriteOffRemainder += paidWriteOffRemainder;
            }

            // if(writeOffSource == WriteOffSource.PointsViaWarehouseShortage && values.Item2 > 0) 
            // {
            //     throw new NotImplementedException($"Use it: {values.Item2}!");
            // }
        }

        private static void MakeResort(ref List<SKUForTargetReport> skuForTargetReports, ResortConsumptionTarget target, int underSkuIndex, int overSkuIndex, int sum)
        {
            if (int.IsNegative(underSkuIndex) || int.IsNegative(overSkuIndex))
            {
                return;
            }

            bool isIndicesEqual = underSkuIndex == overSkuIndex;
            bool isIndicesNotEqual = underSkuIndex != overSkuIndex;

            // Пересорты приходов (артикулы параллельных клиентов)
            if (skuForTargetReports[underSkuIndex].IsParallelClientsSKU &&
                isIndicesEqual && target == ResortConsumptionTarget.ResortingReceipt)
            {
                skuForTargetReports[underSkuIndex].MakeResort(target, SKUResortDirection.InternalParallel, skuForTargetReports[overSkuIndex].SKUNumber, sum);
            }


            // Пересорты расходов
            if (isIndicesNotEqual &&
                (target == ResortConsumptionTarget.ResortingConsumptionByOutgoing || target == ResortConsumptionTarget.ResortingConsumptionByTransfer))
            {
                skuForTargetReports[underSkuIndex].MakeResort(target, SKUResortDirection.Incoming, skuForTargetReports[overSkuIndex].SKUNumber, sum);
                skuForTargetReports[overSkuIndex].MakeResort(target, SKUResortDirection.Outgoing, skuForTargetReports[underSkuIndex].SKUNumber, sum);
            }

            // Пересорты приходов
            if (isIndicesNotEqual
                && target == ResortConsumptionTarget.ResortingReceipt)
            {
                skuForTargetReports[underSkuIndex].MakeResort(target, SKUResortDirection.Incoming, skuForTargetReports[overSkuIndex].SKUNumber, sum);
                skuForTargetReports[overSkuIndex].MakeResort(target, SKUResortDirection.Outgoing, skuForTargetReports[underSkuIndex].SKUNumber, sum);
            }

            // Списание экстра приходов
            if (isIndicesEqual && target == ResortConsumptionTarget.ResortingReceipt)
            {
                skuForTargetReports[underSkuIndex].MakeResort(target, SKUResortDirection.Internal, skuForTargetReports[overSkuIndex].SKUNumber, sum);
            }
        }

        public static Task FinalAnalizeSkus(ref List<SKUForTargetReport> skuForTargetReports)
        {
            foreach (var sku in skuForTargetReports)
            {
                if (!sku.IsSkuNormalize && PaidWriteOffRemainder > 0)
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
            return from sku in skuForTargetReports
                   from predicate in skuPredicates
                   where predicate(sku)
                   select sku;
        }

        public static Task FixReceiptBySKUsOfParallelClients(ref List<SKUForTargetReport> skuForTargetReports)
        {
            // Initial set collections
            var updateDataTask = UpdateDataForResorts(ref skuForTargetReports, false);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            if (isParallelClientSkuWithExtraReceipt)
            {
                foreach (var sku in parallelClientSkuWithExtraReceipt!)
                {
                    int sum = sku.SKUReceiptTotal - sku.SKURansomsWaiting;
                    int skuIndex = skuForTargetReports.IndexOf(sku);

                    MakeResort(ref skuForTargetReports, ResortConsumptionTarget.ResortingReceipt, skuIndex, skuIndex, sum);
                }
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}