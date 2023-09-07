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
        public List<PayShortages> PayShortages { get; private set; }
        public string Comment { get; private set; }

        public System.Drawing.Color StatusColor { get; set; }

        #endregion

        #region Constructors && destructors

        public Article(ArticleRowItem item)
        {
            Name = "No name";
            Items = new List<ArticleRowItem> { item };
            RedBalance = new();
            MaxRedemptionDate = DateTime.Now;
            Status = ArticleStatus.Unknown;
            StoreBalance = new ItemStorageBalance();
            PayShortages = new();
            Comment = "-";
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

        public void SetPayShortages(int count, double price)
        {
            PayShortages.Add(new PayShortages(count, price));
        }

        public void Analize()
        {
            if (Items.Count > 0)
            {
                // Get max redemption date
                MaxRedemptionDate = Items.MaxBy(i => i.RedemptionDate).RedemptionDate;

                var itemGroups = GetGroupItemsByStatus(Items);

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

                if (!IsDeadline)
                {
                    Status = ArticleStatus.InWork;
                    Comment = "В работе";
                }
                else
                {
                    // Готовность склада = расход клиенту + остаток
                    var warehouseReadiness = StoreBalance.Consumption + StoreBalance.FinBalance + PayShortages.Sum(el => el.Count);

                    if (StoreBalance.WaitBalance == warehouseReadiness)
                    {
                        // Если ожидание == готовность и остаток 0 - полный расчет
                        if (StoreBalance.FinBalance == 0)
                        {
                            Status = ArticleStatus.FullSet;
                            Comment = "Полный расчет";
                        }
                        // Если ожидание == готовность (расход и остаток) и остаток больше 0 - довоз
                        else if (StoreBalance.FinBalance > 0)
                        {
                            Status = ArticleStatus.OnSearch;
                            Comment = "Довоз складом до клиента";
                        }
                    }
                    else if(StoreBalance.WaitBalance > warehouseReadiness)
                    {
                        // Если ожидание > готовность (расход и остаток) и остаток >= 0 - поиск 
                        if (StoreBalance.FinBalance >= 0)
                        {
                            Status = ArticleStatus.Lost;
                            Comment = $"На поиске ({StoreBalance.WaitBalance - warehouseReadiness})";
                        }
                        // Если ожидание > готовность (расход и остаток) и остаток < 0 - пересортица 
                        else if (StoreBalance.FinBalance < 0)
                        {
                            Status = ArticleStatus.Regrading;
                            Comment = "Пересортица";
                        }
                    }
                    // Если ожидание < готовность (расход и остаток) 
                    else if (StoreBalance.WaitBalance < warehouseReadiness)
                    {
                        Status = ArticleStatus.Regrading;
                        Comment = "Пересортица";
                    }
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
                    case ArticleStatus.Unknown:
                        StatusColor = System.Drawing.ColorTranslator.FromHtml("#D5D8DC");
                        break;
                    default:
                        break;
                }
            }
        }

        private static IOrderedEnumerable<IGrouping<RedemptionStatus, ArticleRowItem>> GetGroupItemsByStatus(List<ArticleRowItem> items)
        {
           return from item in items
                  group item by item.ItemStatus
                  into itemGroup
                  orderby itemGroup.Key
                  select itemGroup;
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