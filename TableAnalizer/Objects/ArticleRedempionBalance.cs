using ClientTableAnalizer.Enums;

namespace ClientTableAnalizer.Objects
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

	public class ArticleRedempionBalance
	{
        #region  Properties
        public List<ArticleRedempionBalanceUnit> Balances {get; private set;}= new () {
             new(ArticleRedemptionStatus.Delivered, "Доставлено на ПВЗ, шт.", 0),
             new(ArticleRedemptionStatus.WithDrawn, "Забрано с ПВЗ, шт.", 0),
             new(ArticleRedemptionStatus.Canceled, "Отменено, шт.", 0),
             new(ArticleRedemptionStatus.Unknown, "Неизвестный, шт.", 0)
        };
        public List<ArticleRowInReport> ArticleRowsInRedemptionReport { get; private set; }
        #endregion

        public ArticleRedempionBalance(ArticleRowInReport articleRowInReport)
        {
            ArticleRowsInRedemptionReport = new(){articleRowInReport};
        }

        public void Set(ArticleRowInReport articleRow) => ArticleRowsInRedemptionReport.Add(articleRow);

        public int GetWaitBalance() 
        {
            var deliveredBalance = Balances.FirstOrDefault(b => Equals(b.Status, ArticleRedemptionStatus.Delivered));
            var withDrawnBalance = Balances.FirstOrDefault(b => Equals(b.Status, ArticleRedemptionStatus.WithDrawn));
            return deliveredBalance.Count + withDrawnBalance.Count;
        }

        public bool GetRowGroupsByStatus(out IOrderedEnumerable<IGrouping<ArticleRedemptionStatus, ArticleRowInReport>> groups)
        {
           groups = from row in ArticleRowsInRedemptionReport
                    group row by row.ItemStatus
                    into itemGroup
                    orderby itemGroup.Key
                    select itemGroup;

            return groups.Any();
        }

        public (DateTime,double, bool) AnalizeBalance(int deadLineDays) 
        {
            DateTime maxRedemptionDate = default;
            double averagePrice = default;
            bool isDeadLine = default;

            // Get max redemption date
            if(ArticleRowsInRedemptionReport.Count > 0) 
                maxRedemptionDate = ArticleRowsInRedemptionReport
                    .Select(r => r.RedemptionDate)
                    .Max();

            // Set is deadline
            isDeadLine = (maxRedemptionDate - DateTime.Now).Duration().Days > deadLineDays;

            // Calculate balances && set average price
            if(GetRowGroupsByStatus(out IOrderedEnumerable<IGrouping<ArticleRedemptionStatus, ArticleRowInReport>> groups))
            {
                foreach (var group in groups) 
                {
                    var balance = this.Balances.FirstOrDefault(b => Equals(b.Status, group.Key));
                    balance.Count += group.Count();

                    // Set average price
                    averagePrice = group.Average(it => it.Price);
                }
            }

            return (maxRedemptionDate, averagePrice, isDeadLine);
        }
    }
}