using System.Collections.Concurrent;
using ClientTableAnalizer.Interfaces;
using ClientTableAnalizer.Objects.SKUItems;

namespace ClientTableAnalizer.Objects
{
    public class SKUDictionaryRepo : IRepository<int,SKU>
    {
        private ConcurrentDictionary<int, Lazy<SKU>> SKUs { get; } = new(){};

        private readonly LazyThreadSafetyMode lazyMode = LazyThreadSafetyMode.ExecutionAndPublication;

        public bool GetItem(int id, out SKU? item)
        {
            SKUs.TryGetValue(id, out Lazy<SKU>? lazyItem);
            item = lazyItem?.Value;
            return item is not null;
        }

        public SKU? AddOrUpdateItem(int id, SKU item) => 
            SKUs.AddOrUpdate(id, new Lazy<SKU>(() => item, lazyMode), (key, oldValue) => new Lazy<SKU>(() => item, lazyMode)).Value;

        public bool RemoveItem(int id, out SKU? item) 
        {
            SKUs.TryRemove(id, out Lazy<SKU>? lazyItem);
            item = lazyItem?.Value;
            return item is not null;
        }

        public void Dispose() => GC.SuppressFinalize(this.SKUs);

        public IEnumerable<SKU> GetItemsList() => 
            SKUs.Select(s => s.Value.Value).AsEnumerable();

        public int Count() => SKUs.Count;
        
    }
}