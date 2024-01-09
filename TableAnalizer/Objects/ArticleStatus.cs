namespace ClientTableAnalizer.Objects
{
    internal enum ArticleStatus
    {
        FullSet,            // Полный расчет
        InWork,             // В работе
        OnSearch,           // На поиске
        OnSearchWarehouse,  // На поиске склада 
        OnSearchPoint,      // На поиске ПВЗ
        Unknown             // Неопределенный
    }
}