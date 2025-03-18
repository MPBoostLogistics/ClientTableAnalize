using TableAnalizer.Objects.Managers;

namespace TableAnalizer.Objects.SKUItems
{
     public class SellmentStatus 
    {
        #region Properties
        public SKUSellmentStatus Status { get; }
        public string StatusLabel { get; }
        public System.Drawing.Color StatusColor { get; }
        #endregion

        #region Constructors
        public SellmentStatus(SKUSellmentStatus status)
        {
            Status = status;

            switch (Status)
            {
                case SKUSellmentStatus.FullSet:
                    StatusLabel = SKUManager.StatusStrings[SKUSellmentStatus.FullSet];
                    StatusColor = SKUManager.StatusColors[SKUSellmentStatus.FullSet];
                    break;
                case SKUSellmentStatus.InWork:
                    StatusLabel = SKUManager.StatusStrings[SKUSellmentStatus.InWork];
                    StatusColor = SKUManager.StatusColors[SKUSellmentStatus.InWork];
                    break;
                case SKUSellmentStatus.DelayDeliveryFromPointsAndWarehouse:
                    StatusLabel = SKUManager.StatusStrings[SKUSellmentStatus.DelayDeliveryFromPointsAndWarehouse];
                    StatusColor = SKUManager.StatusColors[SKUSellmentStatus.DelayDeliveryFromPointsAndWarehouse];
                    break;
                case SKUSellmentStatus.DelayDeliveryFromWarehouse:
                    StatusLabel = SKUManager.StatusStrings[SKUSellmentStatus.DelayDeliveryFromWarehouse];
                    StatusColor = SKUManager.StatusColors[SKUSellmentStatus.DelayDeliveryFromWarehouse];
                    break;
                case SKUSellmentStatus.DelayDeliveryFromPoints:
                    StatusLabel = SKUManager.StatusStrings[SKUSellmentStatus.DelayDeliveryFromPoints];
                    StatusColor = SKUManager.StatusColors[SKUSellmentStatus.DelayDeliveryFromPoints];
                    break;
                default:
                    StatusLabel = SKUManager.StatusStrings[SKUSellmentStatus.Unknown]; 
                    StatusColor = SKUManager.StatusColors[SKUSellmentStatus.Unknown];
                    break;
            }
        }
        #endregion
   }
}