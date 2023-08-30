namespace TableAnalizer.Objects
{
    internal class Article
    {
        private const int DEADLINE_DAYS = 14;

        public string Name { get; private set; } = default;
        public List<Item> Items { get; private set; }
        public ItemBalance Balance { get; private set; }
        public DateTime MaxRedemptionDate { get; private set; }
        public bool IsDeadline { get; private set; } = false;
        public ArticleStatus Status { get; private set; }

        public System.Drawing.Color SetStatusColor { get; set; }

        public Article(Item item)
        {
            Items = new List<Item> { item };
            Balance = new ItemBalance();
        }

        public void SetMaxRedemptionDate(DateTime date)
        {
            MaxRedemptionDate = date;
        }

        public void ChangeBalance(ItemBalanceUnit unit, int value)
        {
            this.Balance.ChangeBalance(unit, value);
        }

        public void Analize()
        {
            bool isOnWayCount = Items.Any(e => e.ItemStatus == RedemptionStatus.OnWay);
            this.IsDeadline = (this.MaxRedemptionDate - DateTime.Now).Duration().Days > DEADLINE_DAYS;

            if (IsDeadline)
            {
                if (Balance.WaitBalance == Balance.Consumption && Balance.FinBalance == 0)
                {
                    Status = ArticleStatus.FullSet;
                }
                else
                {
                    if (Balance.FinBalance < 0)
                    {
                        Status = ArticleStatus.Regrading;
                    }
                    else
                    {
                        if (Balance.Coming < Balance.WaitBalance || (Balance.Consumption < Balance.WaitBalance && Balance.FinBalance == 0))
                        {
                            Status = ArticleStatus.Lost;
                        }
                        else
                        {
                            Status = ArticleStatus.OnSearch;
                        }
                    }
                }
            }
            else
            {
                Status = ArticleStatus.InWork;
            }

            switch (Status)
            {
                case ArticleStatus.FullSet:
                    this.SetStatusColor = System.Drawing.ColorTranslator.FromHtml("#d4efdf");
                    break;
                case ArticleStatus.InWork:
                    this.SetStatusColor = System.Drawing.ColorTranslator.FromHtml("#fdebd0");
                    break;
                case ArticleStatus.OnSearch:
                    this.SetStatusColor = System.Drawing.ColorTranslator.FromHtml("#fadbd8");
                    break;
                case ArticleStatus.Regrading:
                    this.SetStatusColor = System.Drawing.ColorTranslator.FromHtml("#ebdef0");
                    break;
                case ArticleStatus.Lost:
                    this.SetStatusColor = System.Drawing.ColorTranslator.FromHtml("#f1948a");
                    break;
                default:
                    break;
            }
        }
    }
}

