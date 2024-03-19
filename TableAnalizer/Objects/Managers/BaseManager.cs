namespace ClientTableAnalizer.Objects.Managers
{
    public class BaseManager
    {
        

    protected static int? ParseValueToInt(object? obj) =>
        obj is not null && !string.IsNullOrEmpty(obj.ToString()) ?
        int.Parse(obj.ToString()!.Trim()) :
        null;

    }
    
}