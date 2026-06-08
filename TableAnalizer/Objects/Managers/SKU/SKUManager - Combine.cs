
using TableAnalizer.Objects.SKUItems.SKUBalance;
using TableAnalizer.Objects.SKUItems.SKUForTargetReports;

namespace TableAnalizer.Objects.Managers
{
    /// <summary>
    ///  Функицональность для объединения артикулов по алиесам.
    /// </summary>
    public partial class SKUManager : BaseManager
    {
        /// <summary>
        /// Комбинирует артикулы по алиасам для финального отчета
        /// </summary>
        /// <param name="skus">Исходная коллекция артикулов.</param>
        private void CombineSkusForTargetReport(ref List<SKUForFinalReport> skus)
        {
            var aliases = SkuAliasesBalanceUnits.GetValuesList();

            // Итерация по алиасам
            for (int i = 0; i < aliases.Count; i++)
            {
                // Итерация по коллекции артикулов алиаса
                for (int j = 0; j < aliases[i].value.Count; j++)
                {
                    SKUAliasDataUnit unit = (SKUAliasDataUnit)aliases[i].value[j];
                    CombineSkus(unit, ref skus);
                }
            }
        }

        /// <summary>
        /// Комбинирует конретный артикул по его алиасам.
        /// </summary>
        /// <param name="aliasDataUnit">Объект SKUAliasDataUnit с коллекцией артикулов-алиасов.</param>
        /// <param name="skus">Исходная коллекция артикулов.</param>
        private void CombineSkus(SKUAliasDataUnit aliasDataUnit, ref List<SKUForFinalReport> skus)
        {
            if (!FindTargetAndSourceSkusForCombine(aliasDataUnit, skus,
                out List<SKUForFinalReport> combines, out List<long> sources).Result)
            {
                OnManagerReached(new($"Не найдены артикулы для комбинирования по алиасам для {aliasDataUnit}", true, false));
                return;
            }

            // price
            combines.First().SKUPrice = combines.Average(el => el.SKUPrice);

            // date 
            combines.First().SKUDate = combines.Max(el => el.SKUDate);

            // combines ransoms & waiting
            combines.First().SKURansomsCanceled = combines.Sum(el => el.SKURansomsCanceled);
            combines.First().SKURansomsOnWay = combines.Sum(el => el.SKURansomsOnWay);
            combines.First().SKURansomsDelivered = combines.Sum(el => el.SKURansomsDelivered);
            combines.First().SKURansomsWithDrawn = combines.Sum(el => el.SKURansomsWithDrawn);
            combines.First().SKURansomsWaiting = combines.Sum(el => el.SKURansomsWaiting);

            // combines receipt, receipt comment  && write off from points
            combines.First().SKUReceiptGoods = combines.Sum(el => el.SKUReceiptGoods);

            if (combines.Any(el => !string.IsNullOrEmpty(el.SKUReceipComment)))
            {
                List<string> comment = [];
                for (int i = 0; i < combines.Count; i++)
                {
                    if (!string.IsNullOrEmpty(combines[i].SKUReceipComment))
                        comment.Add(combines[i].SKUReceipComment);
                }

                if (comment.Count != 0)
                {
                    // "" - start with empty string to handle empty list case.
                    combines.First().SKUReceipComment = comment.Aggregate("", (current, next) => current + "\n" + next);
                }
            }

            // сombine paid shortage
            combines.First().SKUPaidShortageWarehouse = combines.Sum(el => el.SKUPaidShortageWarehouse);
            combines.First().SKUPaidShortageOrganization = combines.Sum(el => el.SKUPaidShortageOrganization);
            combines.First().SKUPaidShortageOrganizationAndWarehouse = combines.Sum(el => el.SKUPaidShortageOrganizationAndWarehouse);

            // сombine outgoing & transfered & resort & remaining
            combines.First().SKUShipmentGoods = combines.Sum(el => el.SKUShipmentGoods);
            combines.First().SKUTransfered = combines.Sum(el => el.SKUTransfered);
            combines.First().SKUResort = combines.Sum(el => el.SKUResort);
            combines.First().SKURemaining = combines.Sum(el => el.SKURemaining);

            // combine write offs
            combines.First().SKUWriteOffFromPointsAndCouriers = combines.Sum(el => el.SKUWriteOffFromPointsAndCouriers);
            combines.First().SKUWriteOffFromPointsViaWarehouseShortage = combines.Sum(el => el.SKUWriteOffFromPointsViaWarehouseShortage);
            combines.First().SKUWrittenOffFromPointsViaWarehouseAdjustment = combines.Sum(el => el.SKUWrittenOffFromPointsViaWarehouseAdjustment);

            // add combine comment
            if (sources.Count > 0)
            {
                combines.First().SKUAliasesComment = "Тот же товар под другими арт:";
                foreach (var source in sources)
                {
                    combines.First().SKUAliasesComment = source.ToString();
                }

                if (!string.IsNullOrEmpty(aliasDataUnit.AliasComment))
                    combines.First().SKUAliasesComment = $"\n - {aliasDataUnit.AliasComment}";
            }

            // delete aliases
            foreach (var sourceSKUNumber in aliasDataUnit.SKUAliasNumbers)
            {
                int index = skus.FindIndex(s => s.SKUNumber == sourceSKUNumber);
                if (index >= 0)
                {
                    skus.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// Поиск исходного и целевых артикулов для комбинирования.
        /// </summary>
        /// <param name="aliasDataUnit">Объект SKUAliasDataUnit для поиска артикулов.</param>
        /// <param name="skus">Ссылка на коллекцию с артикулами.</param>
        /// <param name="combines">Коллекция артикулов, по которым комбинируем (целевые артикулы).</param>
        /// <param name="sources">Коллекция артикулов, которые комбинируем (исходные артикулы).</param>
        private static Task<bool> FindTargetAndSourceSkusForCombine(SKUAliasDataUnit aliasDataUnit, List<SKUForFinalReport> skus, out List<SKUForFinalReport> combines, out List<long> sources)
        {
            combines = []; sources = [];

            // Поиск целевого артикула.
            SKUForFinalReport? target = skus
                .SingleOrDefault(s => s.SKUNumber == aliasDataUnit.SKUSourceNumber);

            if (target is not null)
                combines.Add(target);

            // Поиск исходных артикулов.
            foreach (var sourceSKUNumber in aliasDataUnit.SKUAliasNumbers)
            {
                SKUForFinalReport? source = skus.SingleOrDefault(s => s.SKUNumber == sourceSKUNumber);
                if (source is not null)
                    combines.Add(source);
                sources.Add(sourceSKUNumber);
            }

            return Task.FromResult(combines.Count > 0 && sources.Count > 0);
        }
    }
}