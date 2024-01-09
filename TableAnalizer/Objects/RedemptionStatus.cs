namespace ClientTableAnalizer.Objects
{
    public enum RedemptionStatus
    {
        Delivered,  // Доставлен на ПВЗ
        WithDrawn,  // Забран с ПВЗ
        Canceled,   // Отменен 
        OnWay,      // В пути
        Unknown     // Неизвестный
    }
}