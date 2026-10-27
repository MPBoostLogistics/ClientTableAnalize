namespace TableAnalizer.Objects.SKUItems.SKUForTargetReports
{
    /// <summary>
    /// Resorts part of "SKUForTargetReport" type.
    /// </summary>
    public partial class SKUForFinalReport
    {
        #region Fields
        private const string RESORT_CONSUMPTION_FIRST_LINE = "Пересортица расходов:";
        private const string RESORT_RECEIPT_FIRST_LINE = "Пересортица приходов:";
        private string skuResortsComment = string.Empty;
        #endregion

        #region Properties
        /// <summary>
        /// Склад: расход пересортом                
        /// </summary>
        public int SKUResort { get; set; }

        /// <summary>
        /// Комментарий к пересортами
        /// </summary>
        public string SKUResortsComment
        {
            get => skuResortsComment;
            set => skuResortsComment = string.IsNullOrEmpty(skuResortsComment) ? skuResortsComment = value : skuResortsComment += $"\n{value}";
        }

        /// <summary>
        /// Экстра-приход товаров от пересортов в 1С.
        /// </summary>
        public int ExtraReceiptFromResortingIn1C { get; private set; } = 0;
        #endregion

        #region Functionality
        /// <summary>
        /// Производит пересорт.
        /// </summary>
        /// <param name="target">Цель пересорта (расход -> расход, расход -> ФФ, приход) </param>
        /// <param name="dir">Направление пересорта (входящий, исходящий, внутренний, внутренний параллельный)</param>
        /// <param name="skuNumber">Артикул пересорта.</param>
        /// <param name="sum">Количество товаров.</param>
        public void MakeResort(bool isManual, ResortConsumptionTarget target, SKUResortDirection dir, long skuNumber, int sum)
        {
            switch (target)
            {
                // Расзходы на перемезение (ФФ)
                case ResortConsumptionTarget.ResortingConsumptionByTransfer:
                    if (!SKUResortsComment.Contains(RESORT_CONSUMPTION_FIRST_LINE))
                    {
                        SKUResortsComment = RESORT_CONSUMPTION_FIRST_LINE;
                    }

                    switch (dir)
                    {
                        case SKUResortDirection.Incoming:
                            SKUResort += sum;
                            SKUResortsComment = $"- от арт. {skuNumber}: {sum} (фф)";
                            break;
                        case SKUResortDirection.Outgoing:
                            SKUTransfered -= sum;
                            SKUResortsComment = $"- на арт. {skuNumber}: {sum} (фф)";
                            break;
                    }
                    break;
                // Расход на расход 
                case ResortConsumptionTarget.ResortingConsumptionByOutgoing:
                    if (!SKUResortsComment.Contains(RESORT_CONSUMPTION_FIRST_LINE))
                    {
                        SKUResortsComment = RESORT_CONSUMPTION_FIRST_LINE;
                    }

                    string marker = isManual ? "(скл)" : "(авто)";
                    switch (dir)
                    {
                        case SKUResortDirection.Incoming:
                            SKUResort += sum;

                            SKUResortsComment = $"- от арт. {skuNumber}: {sum} {marker}";
                            break;
                        case SKUResortDirection.Outgoing:
                            SKUShipmentGoods -= sum;
                            SKUResortsComment = $"- на арт. {skuNumber}: {sum} {marker}";
                            break;
                    }
                    break;

                // Приходы 
                case ResortConsumptionTarget.ResortingReceipt:

                    if (string.IsNullOrEmpty(SKUReceipComment) || !SKUReceipComment.Contains(RESORT_RECEIPT_FIRST_LINE))
                    {
                        SKUReceipComment = RESORT_RECEIPT_FIRST_LINE;
                    }

                    switch (dir)
                    {
                        case SKUResortDirection.Incoming:
                            SKUReceiptPointsGoods += sum;
                            SKUReceipComment = $"плюс {sum} от арт. {skuNumber}";
                            break;
                        case SKUResortDirection.Outgoing:
                            SKUReceiptPointsGoods -= sum;
                            SKUReceipComment = $"минус {sum} на арт. {skuNumber}";
                            break;
                        case SKUResortDirection.InternalParallel:
                            if (SKUReceiptPointsGoods >= sum)
                            {
                                SKUReceiptPointsGoods -= sum;
                                SKUReceipComment = $"- приход списано: {sum} (апк-) ???";
                            }
                            break;
                        case SKUResortDirection.Internal:

                            if (SKUReceiptPointsGoods >= sum)
                            {
                                SKUReceiptPointsGoods -= sum;
                                SKUResortsComment = $"- приход списано: {sum} (эп)";
                            }

                            // Final balance receipt
                            if (SKUReceiptPointsTotal > SKURansomsWaiting)
                            {
                                SKUReceiptPointsGoods -= SKUReceiptPointsTotal - SKURansomsWaiting;
                            }

                            break;
                        default:
                            break;
                    }
                    break;
            }
        }
        #endregion
    }
}