namespace ClientTableAnalizer.Interfaces
{
    public interface IRepository<K,V> : IDisposable
    {
        /// <summary>
        /// Finds and updates a collection element, or adds a new one.
        /// </summary>
        /// <param name="id">Search key.</param>
        /// <param name="item">Value to update or add.</param>
        /// <returns>Boolean flag for performing operation.</returns>
        bool RetrieveAndUpdateOrAddValue(K id, V item);

        /// <summary>
        /// Returns elements of a collection as an IList.
        /// </summary>
        /// <returns>Enumerating elements of a collection./returns>
        IList<(K key, V value)> GetValuesList();

        /// <summary>
        /// Getting a collection element by key.
        /// </summary>
        /// <param name="id">Key to search for an element.</param>
        /// <param name="item">Found element (optional).</param>
        /// <returns>Boolean flag for performing the operation.</returns>
        bool GetValue(K id, out V? item);

        /// <summary>
        /// Removing a collection element by key.
        /// </summary>
        /// <param name="id">Key of element to delete.</param>
        /// <param name="item">Removed element (optional).</param>
        /// <returns>Boolean flag for performing operation.</returns>
        bool RemoveValue (K id, out V? item);

        /// <summary>
        /// Returns number of elements in collection.
        /// </summary>
        /// <returns>Number of collection elements.</returns>
        int Count();
    }
}