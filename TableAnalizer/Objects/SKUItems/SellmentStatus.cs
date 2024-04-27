using ClientTableAnalizer.Enums;

namespace ClientTableAnalizer.Objects.SKUItems
{
     public class SellmentStatus 
    {
        #region Properties
        public ArticleSellmentStatus Status { get; }
        public string StatusLabel { get; }
        public System.Drawing.Color StatusColor { get; }
        #endregion

        #region Constructors
        public SellmentStatus(ArticleSellmentStatus status)
        {
            Status = status;

            switch (Status)
            {
                case ArticleSellmentStatus.FullSet:
                    StatusLabel = "Полный расчет";
                    StatusColor = System.Drawing.ColorTranslator.FromHtml("#e1ffdb");
                    break;
                case ArticleSellmentStatus.InWork:
                    StatusLabel = "В работе";
                    StatusColor = System.Drawing.ColorTranslator.FromHtml("#ffe7cd");
                    break;
                case ArticleSellmentStatus.OnSearchWarehouseAndPoints:
                    StatusLabel = "На поиске склада и ПВЗ и склада";
                    StatusColor =  System.Drawing.ColorTranslator.FromHtml("#ff6d6d");
                    break;
                case ArticleSellmentStatus.OnSearchWarehouse:
                    StatusLabel = "На поиске склада"; 
                    StatusColor = System.Drawing.ColorTranslator.FromHtml("#ffd3d3");
                    break;
                case ArticleSellmentStatus.OnSearchPoint:
                    StatusLabel = "На поиске ПВЗ";
                    StatusColor =  System.Drawing.ColorTranslator.FromHtml("#ffa0a0");
                    break;
                case ArticleSellmentStatus.Unknown:
                    StatusLabel = "Не определен"; 
                    StatusColor = System.Drawing.ColorTranslator.FromHtml("#e6fcff");
                    break;
                default:
                    StatusLabel = "Не определен"; 
                    StatusColor = System.Drawing.ColorTranslator.FromHtml("#e6fcff");
                    break;

            }
        }
        #endregion
   }
}