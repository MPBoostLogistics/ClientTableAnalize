namespace TableAnalizer.Objects
{
    internal class ItemBalance
    {
        public int WaitBalance { get; private set; }    // -
        public int InitBalance { get; private set; }    // 14
        public int Coming { get; private set; }         // 15
        public int Consumption { get; private set; }    // 16
        public int FinBalance { get; private set; }     // 17

        public ItemBalance()
        {
            WaitBalance = InitBalance = Coming = Consumption = FinBalance = default;
        }

        public void ChangeBalance(ItemBalanceUnit unit, int value)
        {
            switch (unit)
            {
                case ItemBalanceUnit.WaitBalance:
                    WaitBalance += value;
                    break;
                case ItemBalanceUnit.Coming:
                    Coming += value;
                    break;
                case ItemBalanceUnit.Consumption:
                    Consumption += value;
                    break;
                case ItemBalanceUnit.FinBalance:
                    FinBalance += value;
                    break;
                default:
                    break;
            }
        }
    }
}