namespace ClientTableAnalizer.Objects.SKUItems.SKUBalances
{
    public class SKUResortBalance
    {
        #region Properties
        public int SKUNumber { get; private set; }
        public readonly Dictionary<int, (int resortSKUCount, string? resortSKUComment)> ResortSKUs = new(); // Key: resort SKUNumber
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
                var comment = skuResortDataUnit.ResortComment;

                UpdateResortSKUs(key, count, comment);
            }
        }
        
        public void Update(Dictionary<int, (int resortSKUCount, string? resortSKUComment)> newValues) 
        {
            foreach (var (key, value) in newValues)
            {
                var count = value.resortSKUCount;
                var comment = value.resortSKUComment;

                UpdateResortSKUs(key, count, comment);
            }
        }

        private void UpdateResortSKUs(int resortSKUNumber, int resortCount, string? resortComment) 
        {
             if(ResortSKUs.TryGetValue(resortSKUNumber, out (int resortSKUCount, string? resortSKUComment) retriveResortSKU)) 
                {
                    retriveResortSKU.resortSKUCount += resortCount;
                    if(!string.IsNullOrEmpty(resortComment)) 
                    {
                        retriveResortSKU.resortSKUComment = string.Concat(retriveResortSKU.resortSKUComment, ", ", resortComment);
                    }
                } 
                else 
                {
                    var newValue = (resortCount, resortComment ?? string.Empty);
                    ResortSKUs.TryAdd(resortSKUNumber, newValue);
                }
        }

        public void AddСommentToBalance() 
        {
           foreach (var resortSKU in ResortSKUs)
           {
                var resortSku = resortSKU.Key;
                var resortSkuCount = resortSKU.Value.resortSKUCount;
                var resortSKUComment = resortSKU.Value.resortSKUComment;

                List<string> strings = new();

                if(!string.IsNullOrEmpty(ResortComment)) 
                    strings.Add(ResortComment);
                
                if(!string.IsNullOrEmpty(resortSKUComment)) 
                {
                    if(strings.Count > 0) 
                        strings.Add(", ");

                    strings.Add(resortSKUComment);
                }

                if(strings.Count > 0) strings.Add(", ");

                strings.Add($"пересорт на {resortSku} ({resortSkuCount} шт)");
                strings.Add(ResortSKUs.Values.Last() != resortSKU.Value ? ", " : ".");

                ResortComment = string.Join("", strings.ToArray());
           }
        }
        
        #endregion
    }
}