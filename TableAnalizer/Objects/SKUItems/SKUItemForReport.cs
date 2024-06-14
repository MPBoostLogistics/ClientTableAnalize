namespace TableAnalizer.Objects.SKUItems
{
    /// <summary>
    /// “Lightweight” item of article (product).
    /// </summary>
    /// <remark>
    /// Used for reports (shortages, overruns).
    /// </remark>
    public class SKUItemForReport
    {
        #region Properties
        public int SKUNumber { get; private set; }
        public string Name { get; private set; }
        public int Count { get; private set; }
        public double Cost { get; private set; }
        public double Sum { get; private set; }
        #endregion

        #region Constructors 
        public SKUItemForReport(int skuNumber, string name, int count, double cost, double sum)
        {
            SKUNumber = skuNumber; Name = name; Count = count; Cost = cost; Sum = sum;
        }
        #endregion
    }
}

