namespace ClientTableAnalizer.Objects
{
	public class ArticleRedempionBalance
	{
        public int WaitBalance { get; private set; } = default;     // Ожидается к получению (Доставлен на ПВЗ + Забран с ПВЗ)
        public int DeliveredCount { get; private set; } = default;  // Доставлен на ПВЗ
        public int WithDrawnCount { get; private set; } = default;  // Забран с ПВЗ
        public int CanceledCount { get; private set; } = default;   // Отменен 
        public int OnWayCount { get; private set; } = default;      // В пути

        public void SetBalance(RedemptionStatus status, int value)
        {
            switch (status)
            {
                case RedemptionStatus.Delivered:
                    DeliveredCount += value;
                    break;
                case RedemptionStatus.WithDrawn:
                    WithDrawnCount += value;
                    break;
                case RedemptionStatus.Canceled:
                    CanceledCount += value;
                    break;
                case RedemptionStatus.OnWay:
                    OnWayCount += value;
                    break;
                default:
                    break;
            }

            WaitBalance = DeliveredCount + WithDrawnCount;
        }
    }
}