namespace TableAnalizer.Objects.SKUItems.SKUForTargetReports 
{
    /// <summary>
    /// Resorts part of "SKUForTargetReport" type.
    /// </summary>
    public partial class SKUForTargetReport 
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
        #endregion

        #region Functionality
        public void MakeResort(ResortConsumptionTarget target, SKUResortDirection dir, int skuNumber, int sum) 
        {
            switch (target)
            {
                case ResortConsumptionTarget.ResortingConsumptionByTransfer:
                    if(!SKUResortsComment.Contains(RESORT_CONSUMPTION_FIRST_LINE)) 
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

                case ResortConsumptionTarget.ResortingConsumptionByOutgoing:
                    if(!SKUResortsComment.Contains(RESORT_CONSUMPTION_FIRST_LINE)) 
                    {
                        SKUResortsComment = RESORT_CONSUMPTION_FIRST_LINE;
                    }
                    
                    switch (dir)
                    {
                        case SKUResortDirection.Incoming:
                            SKUResort += sum;
                            SKUResortsComment = $"- от арт. {skuNumber}: {sum} (р)";
                            break;
                        case SKUResortDirection.Outgoing:
                            SKUOutgoingProducts -= sum;
                            SKUResortsComment = $"- на арт. {skuNumber}: {sum} (р)";
                            break;
                    }
                    break;
                
                case ResortConsumptionTarget.ResortingReceipt:

                if(!SKUResortsComment.Contains(RESORT_RECEIPT_FIRST_LINE)) 
                {
                    SKUResortsComment = RESORT_RECEIPT_FIRST_LINE;
                }
                
                switch (dir) 
                {
                    case SKUResortDirection.Incoming:
                        SKUReceiptGoods += sum;
                        SKUResortsComment = $"- от арт. {skuNumber}: {sum} (п+)";
                        break;
                    case SKUResortDirection.Outgoing:
                        SKUReceiptGoods -= sum;
                        SKUResortsComment = $"- на арт. {skuNumber}: {sum} (п-)";
                        break;
                    case SKUResortDirection.InternalParallel:
                        if(SKUReceiptGoods >= sum) 
                        {
                            SKUReceiptGoods -= sum;
                            SKUResortsComment = $"- приход списано: {sum} (апк-)";
                        } 
                        break;
                    case SKUResortDirection.Internal:
                    
                        if(SKUReceiptGoods >= sum) 
                        {
                            SKUReceiptGoods -= sum;
                            SKUResortsComment = $"- приход списано: {sum} (эп)";
                        } 

                        // Final balance receipt
                        if(SKUReceiptTotal > SKURansomsWaiting) 
                        {
                            SKUReceiptGoods -= SKUReceiptTotal - SKURansomsWaiting;
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