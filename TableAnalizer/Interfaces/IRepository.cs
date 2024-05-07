namespace ClientTableAnalizer.Interfaces
{
    public interface IRepository<Key,Value> : IDisposable
    {
        /// <summary>
        /// Returns elements of a collection as an enumeration.
        /// </summary>
        /// <returns>Enumerating the elements of a collection./returns>
        IEnumerable<Value> GetItemsList();

        /// <summary>
        /// Getting a collection element by key.
        /// </summary>
        /// <param name="id">Key to search for an element.</param>
        /// <param name="item">Found element (optional).</param>
        /// <returns>Boolean flag for performing the operation.</returns>
        bool GetItem(Key id, out Value? item);

        /// <summary>
        /// Creates or updates a entity in a collection.
        /// </summary>
        /// <param name="id">Entity ID.</param>
        /// <param name="item">Entity.</param>
        /// <returns>An added or updated entity in collection.</returns>
        Value? AddOrUpdateItem(Key id, Value item);

        /// <summary>
        /// Removing a collection element by key.
        /// </summary>
        /// <param name="id">Key of element to delete.</param>
        /// <param name="item">Removed element (optional).</param>
        /// <returns>Boolean flag for performing operation.</returns>
        bool RemoveItem (Key id, out Value? item);

        /// <summary>
        /// Returns number of elements in collection.
        /// </summary>
        /// <returns>Number of collection elements.</returns>
        int Count();
    }
}