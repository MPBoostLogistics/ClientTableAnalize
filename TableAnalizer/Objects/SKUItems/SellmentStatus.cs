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

            StatusLabel = SKUManager.StatusStrings[Status];
            StatusColor = SKUManager.StatusColors[Status];
        }
        #endregion
   }
}