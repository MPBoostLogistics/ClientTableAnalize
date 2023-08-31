namespace TableAnalizer.Objects
{
    internal class Article
    {
        #region Variables and constants

        private const int DEADLINE_DAYS = 14;

        public string Name { get; private set; }
        public List<ArticleRowItem> Items { get; private set; }
        public ArticleRedempionBalance RedBalance { get; private set; }
        public ItemStorageBalance StoreBalance { get; private set; }
        public DateTime MaxRedemptionDate { get; private set; }
        public bool IsDeadline { get; private set; } = false;
        public ArticleStatus Status { get; private set; }
        public double AveragePrice {get; private set;}

        public System.Drawing.Color StatusColor { get; set; }

        #endregion

        #region Constructors && destructors

        public Article(ArticleRowItem item)
        {
            Name = "No name";
            Items = new List<ArticleRowItem> { item };
            RedBalance = new();
            MaxRedemptionDate = DateTime.Now;
            StoreBalance = new ItemStorageBalance();
        }

        #endregion

        #region Functionality
        public void SetName(string name)
        {
            this.Name = name;
        }

        public void ChangeBalance(ItemBalanceUnit unit, int value)
        {
            this.StoreBalance.ChangeBalance(unit, value);
        }

        public void Analize()
        {
            if (Items.Count > 0)
            {
                // Get max redemption date
                MaxRedemptionDate = Items.MaxBy(i => i.RedemptionDate).RedemptionDate;

                var itemGroups =
                    from item in Items
                    group item by item.ItemStatus
                    into itemGroup
                    orderby itemGroup.Key
                    select itemGroup;

                // Get redemtion balance
                foreach (var group in itemGroups)
                {
                    switch (group.Key)
                    {
                        case RedemptionStatus.Delivered:
                            RedBalance.SetBalance(RedemptionStatus.Delivered, group.Count());
                            break;
                        case RedemptionStatus.WithDrawn:
                            RedBalance.SetBalance(RedemptionStatus.WithDrawn, group.Count());
                            break;
                        case RedemptionStatus.Canceled:
                            RedBalance.SetBalance(RedemptionStatus.Canceled, group.Count());
                            break;
                        case RedemptionStatus.OnWay:
                            RedBalance.SetBalance(RedemptionStatus.OnWay, group.Count());
                            break;
                        default:
                            break;
                    }

                    AveragePrice = group.Average(it => it.Price);
                }

                var waitBalance = RedBalance.DeliveredCount + RedBalance.WithDrawnCount;
                this.StoreBalance.ChangeBalance(ItemBalanceUnit.WaitBalance, waitBalance);

                this.IsDeadline = (this.MaxRedemptionDate - DateTime.Now).Duration().Days > DEADLINE_DAYS;

                if (IsDeadline)
                {
                    if (StoreBalance.WaitBalance == StoreBalance.Consumption && StoreBalance.FinBalance == 0)
                    {
                        Status = ArticleStatus.FullSet;
                    }
                    else
                    {
                        if (StoreBalance.FinBalance < 0)
                        {
                            Status = ArticleStatus.Regrading;
                        }
                        else
                        {
                            if (StoreBalance.Coming < StoreBalance.WaitBalance || (StoreBalance.Consumption < StoreBalance.WaitBalance && StoreBalance.FinBalance == 0))
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
                        this.StatusColor = System.Drawing.ColorTranslator.FromHtml("#d4efdf");
                        break;
                    case ArticleStatus.InWork:
                        this.StatusColor = System.Drawing.ColorTranslator.FromHtml("#fdebd0");
                        break;
                    case ArticleStatus.OnSearch:
                        this.StatusColor = System.Drawing.ColorTranslator.FromHtml("#fadbd8");
                        break;
                    case ArticleStatus.Regrading:
                        this.StatusColor = System.Drawing.ColorTranslator.FromHtml("#ebdef0");
                        break;
                    case ArticleStatus.Lost:
                        this.StatusColor = System.Drawing.ColorTranslator.FromHtml("#f1948a");
                        break;
                    default:
                        break;
                }
            }
        }

        public override string ToString()
        {
            return $"Name: {Name}\n" +
                $"RedBalanсe: can {RedBalance.CanceledCount}, way {RedBalance.OnWayCount}, del {RedBalance.DeliveredCount}, drawn {RedBalance.WithDrawnCount}\n" +
                $"StoreBalance: {StoreBalance}\nMaxRedDate: {MaxRedemptionDate}, IsDeadline: {IsDeadline}\nStatus{Status}\nPrice: {AveragePrice}\n\r";
        }
        #endregion
    }
}