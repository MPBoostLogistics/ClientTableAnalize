using ClientTableAnalizer.Enums;

namespace ClientTableAnalizer.Objects.SKUItems
{
    public struct ArticleRedempionBalanceUnit 
    {
        public ArticleRedemptionStatus Status {get; set;}
        public string Describtion {get; set;}
        public int Count {get; set;}

        public ArticleRedempionBalanceUnit(ArticleRedemptionStatus status, string describtion, int count)
        {
            Status = status; Describtion = describtion; Count = count;
        }
    }

	public class SKURedempionBalance
	{
        #region  Properties
        public List<ArticleRedempionBalanceUnit> Balances { get; } = new () 
        {
             new(ArticleRedemptionStatus.Delivered, "Доставлено на ПВЗ, шт.", 0),
             new(ArticleRedemptionStatus.WithDrawn, "Забрано с ПВЗ, шт.", 0),
             new(ArticleRedemptionStatus.Canceled, "Отменено, шт.", 0),
             new(ArticleRedemptionStatus.Unknown, "Неизвестный, шт.", 0)
        };
        public List<SKURowInReport> SKURowsInRedemptionReport { get; private set; }
        #endregion

        public SKURedempionBalance(SKURowInReport skuRowInReport)
        {
            SKURowsInRedemptionReport = new(){skuRowInReport};
        }

        public SKURedempionBalance(List<SKURowInReport> skuRowsInReport)
        {
            SKURowsInRedemptionReport = new(skuRowsInReport);
        }

        public void Set(SKURowInReport articleRow) => SKURowsInRedemptionReport.Add(articleRow);

        private int GetBalanceCountByStatus(ArticleRedemptionStatus ars) =>  
            Balances.FirstOrDefault(b => b.Status == ars).Count;

        // public int GetExpectedBalance() 
        // {
        //     var deliveredBalance = GetBalanceCountByStatus(ArticleRedemptionStatus.Delivered);
        //     var withDrawnBalance = GetBalanceCountByStatus(ArticleRedemptionStatus.WithDrawn);
        //     return deliveredBalance + withDrawnBalance;
        // }

        public void AnalizeBalance(ref SKUBalancesReport articleBalancesReport) 
        {
            int canceled = GetBalanceCountByStatus(ArticleRedemptionStatus.Canceled);
            int onWay = GetBalanceCountByStatus(ArticleRedemptionStatus.OnWay);
            int delivered = GetBalanceCountByStatus(ArticleRedemptionStatus.Delivered);
            int withDrawn = GetBalanceCountByStatus(ArticleRedemptionStatus.WithDrawn);
            int sum = canceled + onWay + delivered + withDrawn;

            articleBalancesReport.SetRedemtionsCount(sum, canceled, onWay, delivered, withDrawn);
        }

        private bool GetRowGroupsByStatus(out IOrderedEnumerable<IGrouping<ArticleRedemptionStatus, SKURowInReport>> groups)
        {
           groups = from row in SKURowsInRedemptionReport
                    group row by row.ItemStatus
                    into itemGroup
                    orderby itemGroup.Key
                    select itemGroup;

            return groups.Any();
        }

        public (DateTime,double, bool) AnalizeArticleRows(int deadLineDays) 
        {
            DateTime maxRedemptionDate = default;
            double averagePrice = default;
            bool isDeadLine = default;

            // Get max redemption date
            if(SKURowsInRedemptionReport.Count > 0) 
                maxRedemptionDate = SKURowsInRedemptionReport
                    .Select(r => r.RedemptionDate)
                    .Max();

            // Set is deadline
            isDeadLine = (maxRedemptionDate - DateTime.Now).Duration().Days > deadLineDays;

            // Calculate rows, set balances && set average price
            if(GetRowGroupsByStatus(out IOrderedEnumerable<IGrouping<ArticleRedemptionStatus, SKURowInReport>> groups))
            {
                foreach (var group in groups) 
                {
                    var balance = this.Balances
                        .FirstOrDefault(b => Equals(b.Status, group.Key));
                        
                    balance.Count += group.Count();

                    // Set average price
                    averagePrice = group.Average(it => it.Price);
                }
            }

            foreach (var balance in Balances)
            {
                Console.WriteLine($"{balance.Status}, {balance.Count}");
            }

            return (maxRedemptionDate, averagePrice, isDeadLine);
        }
    }
}