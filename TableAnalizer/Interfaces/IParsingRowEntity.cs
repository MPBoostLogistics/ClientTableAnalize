using System.Reflection;

namespace TableAnalizer.Interfaces
{
    public interface IParsingRowEntity 
    {
        public PropertyInfo[] PropertyInfos { get; }
        public static List<string> DateOnlyFormats { get; } = ["dd.MM.yyyy HH:mm", "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy"];
    }
}