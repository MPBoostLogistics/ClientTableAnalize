namespace ClientTableAnalizer.Interfaces
{
    interface IRepository<T> : IDisposable
    {
        IEnumerable<T> GetItemsList();
        T GetItem(int id);
        void Create(T item);
        void Update(T item);
        void Delete(int id);
        void Save();
    }
}