using System.Reflection;

namespace ClientTableAnalizer.Objects.Parsing
{
    /// <summary>
    /// An entity for parsing properties from a data dictionary.
    /// </summary>
    public abstract class ParsingEntity
    {
        #region Variables and constants
        protected const string SPACE_SEPARATOR = "\u0020";
        #endregion


        #region Properties
        protected PropertyInfo[] PropertyInfos { get; private set; }
        protected string DateOnlyFormat;
        #endregion

        #region  Constructors
        public ParsingEntity(in Dictionary<string, (string value, Type type)> parsingElements, string dateOnlyFormat = "dd.MM.yyyy")
        {
            PropertyInfos = GetType().GetProperties();
            DateOnlyFormat = dateOnlyFormat;

            if(!int.Equals(PropertyInfos.Length, parsingElements.Count))
                throw new ArgumentException($"Представлено меньше аргументов {parsingElements.Count} для формирования свойств типа (ожидается {PropertyInfos.Length})");

            try
            {
                ParseValues(parsingElements);
            }
            catch (System.Exception ex)
            {
                // TODO: Обработать
                throw;
            }

        }
        #endregion

        #region Functionality

        protected static void ThrowExceptionOnArgument(string valueKey) => 
            throw new ArgumentException($"Key value not provided: '{valueKey}'");
        
        protected static void ThrowExceptionCast(string valueKey, Type type) => 
            throw new InvalidCastException($"Element with key {valueKey} cannot be casting to type {type}.");
        
        protected void ParseValues(in Dictionary<string, (string value, Type type)> parsingElements)
        {
            foreach (var parsingElement in parsingElements)
            {
                // Get target property
                var targetProperty = PropertyInfos.FirstOrDefault(p => p.Name == parsingElement.Key);
                if(targetProperty is null)
                   ThrowExceptionOnArgument(parsingElement.Key);

                try
                {
                    ParseValue(in parsingElement, ref targetProperty!);
                }
                catch (System.Exception)
                {
                    // TODO: Обработать
                    throw;
                }
            }
        }

        private void ParseValue(in KeyValuePair<string, (string value, Type type)> element, ref PropertyInfo property)
        {

            Type targetType = element.Value.type , sourceType = property.GetType();

            if (!Equals(targetType, sourceType))
                throw new Exception($"Type of prop being created '{targetType}' does not match parsing type '{sourceType}'");

            var elementValueString = element.Value.value.Trim();
            if (string.IsNullOrEmpty(elementValueString))
                throw new Exception($"Parsing value with key '{element.Key}' cannot be null.");

            switch (Type.GetTypeCode(targetType.GetType()))
            {
                case TypeCode.Double:

                    var elementDoubleValueString = elementValueString;

                    if(elementValueString.Split(SPACE_SEPARATOR).Length == 2) 
                        elementDoubleValueString = elementValueString.Split(SPACE_SEPARATOR)[0];
                    
                    if (!double.TryParse(elementDoubleValueString, out double doubleValue))
                        ThrowExceptionCast(element.Key, targetType);

                    property.SetValue(this, doubleValue);

                    break;

                case TypeCode.Int32:

                    if (!int.TryParse(elementValueString, out int intValue))
                        ThrowExceptionCast(element.Key, targetType);

                    property.SetValue(this, intValue);

                    break;

                case TypeCode.DateTime:
                    throw new NotImplementedException();
                    //    if (!DateOnly.TryParseExact(elementValueString, DateOnlyFormat, out DateOnly dateValue))
                    //     ReportCastError((element.Key, targetType));

                    // property.SetValue(this, dateValue);
                    // break;
                case TypeCode.String:
                    if(string.IsNullOrEmpty(elementValueString))
                        ThrowExceptionCast(element.Key, targetType);

                    property.SetValue(this, elementValueString);

                    break;
            }
        }

        #endregion
    }
}