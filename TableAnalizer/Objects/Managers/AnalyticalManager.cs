using Microsoft.Extensions.Configuration;
using TableAnalizer.Objects.Parsing;
using TableAnalizer.Objects.SKUItems.SKUBalances.SKUResortBalance;
using TableAnalizer.Objects.SKUItems.SKUForTargetReports;

namespace TableAnalizer.Objects.Managers
{
    public class AnalyticalManager(IConfiguration config, SKUManager skuManager) : BaseManager(config)
    {
        private readonly SKUManager skuManager = skuManager;


        #region Fields

        /// <summary>
        /// Предикат: артикул параллельных клинтов и ожидание меньше общего прихода
        /// </summary>
        private static Func<SKUForFinalReport, bool> ParallelClientSkuWithExtraReceiptPredicate => (sku) =>
            sku.IsParallelClientsSKU &&
            sku.SKURansomsWaiting < sku.SKUReceiptPointsTotal;

        private static Func<SKUForFinalReport, bool> OverPredicateConsumptionByOutgoingProducts => (sku) =>
            !sku.IsSkuNormalize
            && sku.OverMutualSettlement > 0 &&
            int.IsPositive(sku.SKUShipmentGoods - sku.OverMutualSettlement);

        private static Func<SKUForFinalReport, bool> OverPredicateConsumptionByTransfer => (sku) =>
            !sku.IsSkuNormalize &&
            sku.OverMutualSettlement > 0 &&
            int.IsPositive(sku.SKUTransfered - sku.OverMutualSettlement);

        private static Func<SKUForFinalReport, bool> UnderPredicateByConsumption => (sku) =>
            !sku.IsSkuNormalize &&
            sku.UnderMutualSettlement > 0;

        private static Func<SKUForFinalReport, bool> OverPredicateByReceipt => (sku) =>
            !sku.IsSkuNormalize &&
            sku.OverReceipt > 0 &&
            int.IsPositive(sku.SKUReceiptPointsGoods - sku.OverReceipt);

        private static Func<SKUForFinalReport, bool> UnderPredicateByReceipt => (sku) =>
            !sku.IsSkuNormalize &&
            sku.UnderReceipt > 0;

        /// <summary>
        /// Артикул не нормализован, взаиморасчет + остаток = ожидание и общий приход больше ожидания
        /// </summary>
        private static Func<SKUForFinalReport, bool> ExtraPredicateByReceipt => (sku) =>
            !sku.IsSkuNormalize &&
            (sku.SKUSummarySettlement + sku.SKURemaining) == sku.SKURansomsWaiting &&
            sku.SKUReceiptPointsTotal > sku.SKURansomsWaiting;

        /// <summary>
        /// Были списания недостач точек через склад
        /// </summary>
        private static Func<SKUForFinalReport, bool> WrittenOffFromPointsViaWarehouseByShortage => (sku) =>
            sku.SKUWriteOffFromPointsViaWarehouseShortage > 0;

        /// <summary>
        /// Были списания корректировки от точек через склад
        /// </summary>
        private static Func<SKUForFinalReport, bool> WrittenOffFromPointsViaWarehouseByAdjustment => (sku) =>
            sku.SKUWrittenOffFromPointsViaWarehouseAdjustment > 0;

        /// <summary>
        /// Артикулы с перерасходами (по расходу)
        /// </summary>
        private static LinkedList<SKUForFinalReport>? overspendingSkusByConsumption;

        /// <summary>
        ///  Артикулы с недорасходами (по расходу)
        /// </summary>
        private static LinkedList<SKUForFinalReport>? underspendingSkusByConsumption;

        /// <summary>
        /// Артикулы с перерасходами (по приходу)
        /// </summary>
        private static LinkedList<SKUForFinalReport>? overspendingSkusByReceipt;

        /// <summary>
        ///  Артикулы с недорасходами (по приходу)
        /// </summary>
        private static LinkedList<SKUForFinalReport>? underspendingSkusByReceipt;

        /// <summary>
        ///  Артикулы со списанием недостач с точек через склад
        /// </summary>
        private static List<SKUForFinalReport>? writeOffPointsByShortage;

        /// <summary>
        ///  Артикулы со списанием корректировок с точек через склад
        /// </summary>
        private static List<SKUForFinalReport>? writeOffPointsByAdjustment;

        /// <summary>
        /// Список артикулов параллельных клиентов с экстра-приходами.
        /// </summary>
        private static List<SKUForFinalReport>? parallelClientSkuWithExtraReceipt;


        /// <summary>
        ///  Артикулы с недорасходами (по приходу)
        /// </summary>
        //private static LinkedList<SKUForTargetReports>? underspendingSkusByReceipt; // WrittenOffFromPointsViaWarehouseByReceipt

        /// <summary>
        ///  Артикулы с экстра приходом=
        /// </summary>
        private static List<SKUForFinalReport>? extraSkusByReceipt;
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
        private static Func<SKUForFinalReport, bool> IsDontConsiderInProgress => sku => !sku.isDeadline;

        #endregion

        #region Properties 
        private static int PaidWriteOffRemainder { get; set; } = 0;
        #endregion

        #region Functionality

        /// <summary>
        /// Обновляет внутренние коллекции данных. 
        /// </summary>
        /// <param name="skuForTargetReports">Ссылка на рабочую коллекцию.</param>
        /// <param name="isConsiderInProgressStatusInAutoResorting"Учитывать артикулы с неистешим дедлайном (Статус "в работе"</param>
        /// <returns>Флаг выполнения задачи.</returns>
        private static Task UpdateDataForResorts(ref List<SKUForFinalReport> skuForTargetReports, bool isConsiderInProgressStatusInAutoResorting)
        {
            parallelClientSkuWithExtraReceipt = GetSkus(ref skuForTargetReports, isConsiderInProgressStatusInAutoResorting, [ParallelClientSkuWithExtraReceiptPredicate])?.OrderByDescending(el => el.SKUNumber).ToList(); ;
            isParallelClientSkuWithExtraReceipt = parallelClientSkuWithExtraReceipt is not null && parallelClientSkuWithExtraReceipt.Count > 0;

            writeOffPointsByShortage = GetSkus(ref skuForTargetReports, isConsiderInProgressStatusInAutoResorting, [WrittenOffFromPointsViaWarehouseByShortage])?.OrderByDescending(el => el.SKUNumber).ToList(); ;
            isWriteOffPointsThroughWarehouseShortages = writeOffPointsByShortage is not null && writeOffPointsByShortage.Count > 0;

            writeOffPointsByAdjustment = GetSkus(ref skuForTargetReports, isConsiderInProgressStatusInAutoResorting, [WrittenOffFromPointsViaWarehouseByAdjustment])?.OrderByDescending(el => el.SKUNumber).ToList();
            isWriteOffPointsThroughWarehouseAdjustment = writeOffPointsByAdjustment is not null && writeOffPointsByAdjustment.Count > 0;

            var overspendingByConsumption = GetSkus(ref skuForTargetReports, isConsiderInProgressStatusInAutoResorting, [OverPredicateConsumptionByOutgoingProducts, OverPredicateConsumptionByTransfer]);

            overspendingSkusByConsumption = overspendingByConsumption is not null ? new LinkedList<SKUForFinalReport>(overspendingByConsumption.OrderByDescending(el => el.SKUNumber)) : null;
            isPossibilityOverspendingByConsumption = overspendingSkusByConsumption is not null && overspendingSkusByConsumption.Count != 0;

            var underspendingByConsumption = GetSkus(ref skuForTargetReports, isConsiderInProgressStatusInAutoResorting, [UnderPredicateByConsumption]);
            underspendingSkusByConsumption = underspendingByConsumption is not null ? new LinkedList<SKUForFinalReport>(underspendingByConsumption.OrderByDescending(el => el.SKUNumber)) : null;
            isNeedUnderspendingByConsumption = underspendingSkusByConsumption is not null && underspendingSkusByConsumption.Count != 0;

            var overspendingByReceipt = GetSkus(ref skuForTargetReports, isConsiderInProgressStatusInAutoResorting, [OverPredicateByReceipt]);

            overspendingSkusByReceipt = overspendingByReceipt is not null ? new LinkedList<SKUForFinalReport>(overspendingByReceipt.OrderByDescending(el => el.SKUNumber)) : null;
            isPossibilityOverspendingByReceipt = overspendingSkusByReceipt is not null && overspendingSkusByReceipt.Count != 0;

            var underspendingByReceipt = GetSkus(ref skuForTargetReports, isConsiderInProgressStatusInAutoResorting, [UnderPredicateByReceipt]);

            underspendingSkusByReceipt = underspendingByReceipt is not null ? new LinkedList<SKUForFinalReport>(underspendingByReceipt.OrderByDescending(el => el.SKUNumber)) : null;
            isNeedUnderspendingByReceipt = underspendingSkusByReceipt is not null && underspendingSkusByReceipt.Count != 0;

            extraSkusByReceipt = GetSkus(ref skuForTargetReports, isConsiderInProgressStatusInAutoResorting, [ExtraPredicateByReceipt])?.OrderByDescending(el => el.SKUNumber).ToList();

            isThereAreSkusWithExtraReceipt = extraSkusByReceipt is not null && extraSkusByReceipt.Count != 0;

            return Task.CompletedTask;
        }

        public Task<bool> SetManualResorts(ref List<SKUForFinalReport> skuForTargetReports, bool isConsiderInProgressStatusInAutoResorting)
        {
            if (skuManager.SkuResortUnits.Count() == 0)
                return Task.FromResult(false);

            // Initial set collections
            var updateDataTask = UpdateDataForResorts(ref skuForTargetReports, isConsiderInProgressStatusInAutoResorting);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            // Get manual resort utins 
            List<(long key, List<ParsingRowEntity> value)>? listCollection = [.. skuManager.SkuResortUnits!.GetValuesList()];
            if (listCollection is null || listCollection.Count == 0)
                return Task.FromResult(false);

            foreach (var (skuNumber, listEntities) in listCollection)
            {
                var units = (((List<ParsingRowEntity>?)listEntities)?.Cast<SKUResortUnit>().ToList()) ??
                    throw new InvalidCastException("Cannot cast collection to IList<SKUResortUnit>.");

                foreach (var skuResortUnit in units)
                {

                    SKUForFinalReport? underSKU = skuForTargetReports.FirstOrDefault(el => el.SKUNumber == skuResortUnit.SKUItemAddedNumber);
                    SKUForFinalReport? overSku = skuForTargetReports.FirstOrDefault(el => el.SKUNumber == skuResortUnit.SKUItemWrittenOffNumber);

                    if (underSKU != null && overSku != null)
                    {
                        // Если не учитывать артикулы в работе и не прошел дедлайн у одного из артикулов
                        if ((!underSKU!.isDeadline || !overSku!.isDeadline) && !isConsiderInProgressStatusInAutoResorting)
                        {
                            continue; // Пропускаем такую пару
                        }
                        else
                        {
                            int underReceiptSkuIndex = skuForTargetReports.IndexOf(underSKU);
                            int overReceiptSkuIndex = skuForTargetReports.IndexOf(overSku);


                            MakeResort(
                                true,
                                ref skuForTargetReports,
                                ResortConsumptionTarget.ResortingConsumptionByOutgoing,
                                underReceiptSkuIndex,
                                overReceiptSkuIndex,
                                skuResortUnit.SKUResortSum);
                        }
                    }
                    else
                        throw new ArgumentNullException($"Null values of underSKUIndex or overSKUIndex");
                }
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// Устанавливает автоматические пересорты.
        /// </summary>
        /// <param name="skuForTargetReports">Рабочая коллекция SKUForTargetReport.</param>
        /// <param name="resortTarget">Напрпвление пересорта.</param>
        /// <param name="isConsiderInProgressStatusInAutoResorting">Учитывать артикулы, дедлайн по которым еще не прошел ("в работе").</param>
        /// <returns>Флаг выполнения задачи.</returns>
        public static Task SetAutoResorts(ref List<SKUForFinalReport> skuForTargetReports, ResortConsumptionTarget resortTarget, bool isConsiderInProgressStatusInAutoResorting)
        {
            // Initial set collections
            var updateDataTask = UpdateDataForResorts(ref skuForTargetReports, isConsiderInProgressStatusInAutoResorting);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            bool? workFlag = resortTarget switch
            {
                ResortConsumptionTarget.ResortingConsumptionByOutgoing or ResortConsumptionTarget.ResortingConsumptionByTransfer =>
                    isNeedUnderspendingByConsumption && isPossibilityOverspendingByConsumption,
                ResortConsumptionTarget.ResortingReceipt => isNeedUnderspendingByReceipt && isPossibilityOverspendingByReceipt,
                _ => null
            };

            // Определение связанных списков с перерасходами и недорасходами.
            LinkedList<SKUForFinalReport>? overspendingList = DefinesListWithOverspending(resortTarget);
            LinkedList<SKUForFinalReport>? underspandingList = DefinesListWithUnderspanding(resortTarget);

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

                                MakeResort(false, ref skuForTargetReports, target, underConsumtionSkuIndex, overConsumtionSkuIndex, resortSum);
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

                            MakeResort(false, ref skuForTargetReports, resortTarget, underReceiptSkuIndex, overReceiptSkuIndex, resortSum);

                            break;
                        default:
                            break;
                    }
                }
            }

            // Initial set collections
            updateDataTask = UpdateDataForResorts(ref skuForTargetReports, isConsiderInProgressStatusInAutoResorting);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            if (workFlag != null && (bool)workFlag && resortTarget != ResortConsumptionTarget.ResortingReceipt)
            {
                return SetAutoResorts(ref skuForTargetReports, resortTarget, isConsiderInProgressStatusInAutoResorting);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Определяет список с недорасходами.
        /// </summary>
        /// <param name="resortTarget">Цель (направление пересорта).</param>
        /// <returns>Список с недорасходами (необязательно).</returns>
        private static LinkedList<SKUForFinalReport>? DefinesListWithUnderspanding(ResortConsumptionTarget resortTarget)
        {
            return resortTarget switch
            {
                ResortConsumptionTarget.ResortingConsumptionByOutgoing or ResortConsumptionTarget.ResortingConsumptionByTransfer => underspendingSkusByConsumption,
                ResortConsumptionTarget.ResortingReceipt => underspendingSkusByReceipt,
                _ => null
            };
        }

        /// <summary>   
        /// Определяет список с перерасходами.
        /// </summary>
        /// <param name="resortTarget">Цель (направление пересорта).</param>
        /// <returns>>Список с перерасходами (необязательно).</returns>
        private static LinkedList<SKUForFinalReport>? DefinesListWithOverspending(ResortConsumptionTarget resortTarget)
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
        public static Task FixWriteOffByPointsThroughWarehouse(ref List<SKUForFinalReport> skuForTargetReports, bool isConsiderInProgressStatusInAutoResorting)
        {
            // Initial set collections
            var updateDataTask = UpdateDataForResorts(ref skuForTargetReports, isConsiderInProgressStatusInAutoResorting);
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

        public static Task FixExtraReceipts(ref List<SKUForFinalReport> skuForTargetReports, bool isConsiderInProgressStatusInAutoResorting)
        {
            // Initial set collections
            var updateDataTask = UpdateDataForResorts(ref skuForTargetReports, isConsiderInProgressStatusInAutoResorting);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            if (isThereAreSkusWithExtraReceipt)
            {
                foreach (var sku in extraSkusByReceipt!)
                {
                    int sum = sku.SKUReceiptPointsTotal - sku.SKURansomsWaiting;
                    int skuIndex = skuForTargetReports.IndexOf(sku);

                    MakeResort(false, ref skuForTargetReports, ResortConsumptionTarget.ResortingReceipt, skuIndex, skuIndex, sum);
                }
            }

            return isThereAreSkusWithExtraReceipt ?
                FixExtraReceipts(ref skuForTargetReports, false) :
                Task.CompletedTask;
        }

        public static void MakeCorrectionWriteOffs(ref List<SKUForFinalReport> skuForTargetReports, SourceReportItemKey writeOffSource, int skuIndex)
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

        /// <summary>
        /// Учитывает пересорт.
        /// </summary>
        /// <param name="skuForTargetReports">Список артикулов для конечного отчета.</param>
        /// <param name="target">Цель прересортицы по перерасходу товаров.</param>
        /// <param name="underSkuIndex"></param>
        /// <param name="overSkuIndex"></param>
        /// <param name="sum"></param>
        private static void MakeResort(bool isManual, ref List<SKUForFinalReport> skuForTargetReports, ResortConsumptionTarget target, int underSkuIndex, int overSkuIndex, int sum)
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
                skuForTargetReports[underSkuIndex].MakeResort(isManual, target, SKUResortDirection.InternalParallel, skuForTargetReports[overSkuIndex].SKUNumber, sum);
            }


            // Пересорты расходов
            if (isIndicesNotEqual &&
                (target == ResortConsumptionTarget.ResortingConsumptionByOutgoing || target == ResortConsumptionTarget.ResortingConsumptionByTransfer))
            {
                skuForTargetReports[underSkuIndex].MakeResort(isManual, target, SKUResortDirection.Incoming, skuForTargetReports[overSkuIndex].SKUNumber, sum);
                skuForTargetReports[overSkuIndex].MakeResort(isManual, target, SKUResortDirection.Outgoing, skuForTargetReports[underSkuIndex].SKUNumber, sum);
            }

            // Пересорты приходов
            if (isIndicesNotEqual
                && target == ResortConsumptionTarget.ResortingReceipt)
            {
                skuForTargetReports[underSkuIndex].MakeResort(isManual, target, SKUResortDirection.Incoming, skuForTargetReports[overSkuIndex].SKUNumber, sum);
                skuForTargetReports[overSkuIndex].MakeResort(isManual, target, SKUResortDirection.Outgoing, skuForTargetReports[underSkuIndex].SKUNumber, sum);
            }

            // Списание экстра приходов
            if (isIndicesEqual && target == ResortConsumptionTarget.ResortingReceipt)
            {
                skuForTargetReports[underSkuIndex].MakeResort(isManual, target, SKUResortDirection.Internal, skuForTargetReports[overSkuIndex].SKUNumber, sum);
            }
        }

        public static Task FinalAnalizeSkus(ref List<SKUForFinalReport> skuForTargetReports)
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

        private static IEnumerable<SKUForFinalReport>? GetSkus(ref List<SKUForFinalReport> skuForTargetReports, bool isConsiderInProgressStatusInAutoResorting, List<Func<SKUForFinalReport, bool>> skuPredicates)
        {
            return from sku in skuForTargetReports
                   from predicate in skuPredicates
                   where predicate(sku)
                   where isConsiderInProgressStatusInAutoResorting ? (sku.isDeadline || !sku.isDeadline) : sku.isDeadline
                   select sku;
        }

        /// <summary>
        /// Фиксация приходов от параллельных клиентов.
        /// </summary>
        /// <param name="skuForTargetReports"></param>
        /// <returns></returns>
        public static Task FixReceiptBySKUsOfParallelClients(ref List<SKUForFinalReport> skuForTargetReports, bool isConsiderInProgressStatusInAutoResorting)
        {
            // Initial set collections
            var updateDataTask = UpdateDataForResorts(ref skuForTargetReports, isConsiderInProgressStatusInAutoResorting);
            Task.Run(() => updateDataTask);
            updateDataTask.Wait();

            if (isParallelClientSkuWithExtraReceipt)
            {
                foreach (var sku in parallelClientSkuWithExtraReceipt!)
                {
                    int sum = sku.SKUReceiptPointsTotal - sku.SKURansomsWaiting;
                    int skuIndex = skuForTargetReports.IndexOf(sku);

                    MakeResort(false, ref skuForTargetReports, ResortConsumptionTarget.ResortingReceipt, skuIndex, skuIndex, sum);
                }
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}