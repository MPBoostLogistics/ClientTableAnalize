using System.Reflection;

namespace ClientTableAnalizer.Interfaces
{
    public interface IParsingRowEntity 
    {
        public PropertyInfo[] PropertyInfos { get; }
        public static List<string> DateOnlyFormats { get; } = new() { "dd.MM.yyyy HH:mm", "dd.MM.yyyy HH:mm:ss" };
    }
}