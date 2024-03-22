namespace ClientTableAnalizer.Objects.ArticleItems 
{
   public class SKUBalancesReport
    {
        #region Properties
        public int RedemtionSum { get; private set; }
        public int RedemtionCanceled { get; private set; }
        public int RedemtionOnWay { get; private set; }
        public int RedemtionDelivered { get; private set; }
        public int RedemtionWithDrawn { get; private set; }
        #endregion

        #region Functionality
        public void SetRedemtionsCount(int sum, int canceled, int onWay, int delivered, int withDrawn) 
        {
            RedemtionSum = sum;
            RedemtionCanceled = canceled;
            RedemtionOnWay = onWay;
            RedemtionDelivered = delivered;
            RedemtionWithDrawn = withDrawn;
        }
        #endregion
    }

}