using TableAnalizer.Enums;

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
                    StatusLabel = "Полный расчет";
                    StatusColor = System.Drawing.ColorTranslator.FromHtml("#e1ffdb");
                    break;
                case SKUSellmentStatus.InWork:
                    StatusLabel = "В работе";
                    StatusColor = System.Drawing.ColorTranslator.FromHtml("#ffe7cd");
                    break;
                case SKUSellmentStatus.OnSearchWarehouseAndPoints:
                    StatusLabel = "На поиске  ПВЗ и склада";
                    StatusColor =  System.Drawing.ColorTranslator.FromHtml("#ff6d6d");
                    break;
                case SKUSellmentStatus.OnSearchWarehouse:
                    StatusLabel = "На поиске склада"; 
                    StatusColor = System.Drawing.ColorTranslator.FromHtml("#ffd3d3");
                    break;
                case SKUSellmentStatus.OnSearchPoint:
                    StatusLabel = "На поиске ПВЗ";
                    StatusColor =  System.Drawing.ColorTranslator.FromHtml("#ffa0a0");
                    break;
                case SKUSellmentStatus.Unknown:
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