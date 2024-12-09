using System.Collections.Concurrent;
using TableAnalizer.Interfaces;

namespace TableAnalizer.Objects.Repositories 
{
    public abstract class BaseRepo<K,V> : IRepository<K,V> 
    {
        protected ConcurrentDictionary<K,V> Entiies { get; } = new ConcurrentDictionary<K,V>();

        public virtual int Count() => Entiies.Count;
  
        protected virtual void Dispose() => Entiies.Clear();
        
        public virtual bool GetValue(K id, out V? item) => Entiies.TryGetValue(id, out item);

        public virtual List<V> GetValues(Predicate<V> predicate) => Entiies.Values.ToList().FindAll(predicate).ToList();

        public virtual int GetValuesCount(Predicate<V> predicate) => Entiies.Values.ToList().FindAll(predicate).Count;
  
        public virtual bool RemoveValue(K id, out V? item) => Entiies.TryRemove(id, out item);
 
        public abstract bool RetrieveAndUpdateOrAddValue(K id, V item, bool isMulty);

        IList<(K key, V value)> IRepository<K, V>.GetValuesList() => 
            Entiies.Select(e => (e.Key, e.Value)).ToList();

        void IDisposable.Dispose() => Entiies.Clear();
    }
}