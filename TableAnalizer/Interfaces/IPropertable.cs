namespace ClientTableAnalizer.Interfaces
{
    interface IPropertable 
    {
        string? GetPropertyValueBy(string propertyName);

        // string? GetPropertyValueBy(string propertyName) 
        // {
        //     return typeof(this).GetPropertyInfo("Hello")?.GetValue(sku)?.ToString();
        // }

    }
}