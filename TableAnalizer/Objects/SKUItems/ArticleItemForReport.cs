namespace ClientTableAnalizer.Objects.SKUItems
{
    /// <summary>
    /// “Lightweight” item of article (product).
    /// </summary>
    /// <remark>
    /// Used for reports (shortages, overruns).
    /// </remark>
    public class ArticleItemForReport
    {
        #region Properties
        public int ArticleNumber { get; private set; }
        public string Name { get; private set; }
        public int Count { get; private set; }
        public double Cost { get; private set; }
        public double Sum { get; private set; }
        #endregion

        #region Constructors 
        public ArticleItemForReport(int articleNumber, string name, int count, double cost, double sum)
        {
            ArticleNumber = articleNumber; Name = name; Count = count; Cost = cost; Sum = sum;
        }
        #endregion
    }
}

