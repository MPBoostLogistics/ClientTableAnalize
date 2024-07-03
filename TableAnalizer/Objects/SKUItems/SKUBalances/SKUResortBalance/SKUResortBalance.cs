namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    public class SKUResortBalance
    {
        #region Properties
        public int SKUNumber { get; private set; }
        public readonly Dictionary<int, int> ResortSKUs = new(); // Key: resort SKUNumber, Value: resort Count
        public string ResortComment { get; private set; } = string.Empty;
        
        #endregion Properties

        #region Constructors
        public SKUResortBalance(int skuNumber, List<SKUResortDataUnit> skuResortDataUnits)
        {
            SKUNumber = skuNumber;
            Update(skuResortDataUnits);
        }
        #endregion

        #region Functionality
        public void Update(List<SKUResortDataUnit> skuResortDataUnits) 
        {
            foreach (var skuResortDataUnit in skuResortDataUnits)
            {
                var key = skuResortDataUnit.ResortSKUNumber;
                var count = skuResortDataUnit.ResortSKUCount;
      

                UpdateResortSKUs(key, count);
            }
        }
        
        public void Update(Dictionary<int, int> newValues) 
        {
            foreach (var (key, value) in newValues)
            {
                UpdateResortSKUs(key, value);
            }
        }

        private void UpdateResortSKUs(int resortSKUNumber, int resortCount) 
        {
            ResortSKUs[resortSKUNumber] = ResortSKUs.TryGetValue(resortSKUNumber, out int resortSKUCount) ? 
                resortSKUCount + resortCount : 
                resortCount; 
        }

        public void AddСommentToBalance() 
        {
           foreach (var resortSKU in ResortSKUs)
           {
                var resortSku = resortSKU.Key;
                var resortSkuCount = resortSKU.Value;

                var comment = $"\nПересорт на {resortSku} ({resortSkuCount})";
                comment = ResortSKUs.Values.Last() != resortSKU.Value ? 
                    string.Concat(comment, ", ") : 
                    string.Concat(comment, ".");

                ResortComment = string.IsNullOrEmpty(ResortComment) ? 
                    comment :
                    string.Concat(ResortComment, "\n", comment);
           }
        }
        
        #endregion
    }
}