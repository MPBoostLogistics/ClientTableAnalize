using System.Diagnostics;
using System.Reflection;
using ClientTableAnalizer.Enums;


namespace ClientTableAnalizer.Objects.Parsing
{

    /// <summary>
    /// An entity for parsing properties from a data dictionary.
    /// </summary>
    public abstract class ParsingEntity
    {
        #region Variables and constants
        protected const string SPACE_SEPARATOR = "\u0020";

        protected readonly List<TypeCode> baseTypeCodes = new() { TypeCode.Double, TypeCode.Int32, TypeCode.DateTime, TypeCode.String };
        #endregion

        #region Properties
        protected PropertyInfo[] PropertyInfos { get; private set; }
        protected string DateOnlyFormat;
        #endregion

        #region  Constructors
        public ParsingEntity(in Dictionary<string, (string value, ParsingTypeCode parsingTypeCode)> parsingElements, string dateOnlyFormat = "dd.MM.yyyy")
        {
            PropertyInfos = GetType().GetProperties();
            DateOnlyFormat = dateOnlyFormat;

            try
            {
                ParseValues(parsingElements);
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine(ex.Message);
                throw;
            }

        }
        #endregion

        #region Functionality

        protected static void ThrowExceptionOnArgument(string valueKey) =>
            throw new ArgumentException($"Key value not provided: '{valueKey}'");

        protected static void ThrowExceptionCast(string valueKey, Type type) =>
            throw new InvalidCastException($"Element with key {valueKey} cannot be casting to type {type}.");

        protected void ParseValues(in Dictionary<string, (string value, ParsingTypeCode parsingTypeCode)> parsingElements)
        {
            foreach (var parsingElement in parsingElements)
            {
                // Get target property
                var targetProperty = PropertyInfos.FirstOrDefault(p => p.Name == parsingElement.Key);
                if (targetProperty is null)
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

        private void ParseValue(in KeyValuePair<string, (string value, ParsingTypeCode parsingTypeCode)> element, ref PropertyInfo property)
        {
            Type targetType = property.PropertyType;

            var elementValueString = element.Value.value.Trim() ??
                throw new Exception($"Parsing value with key '{element.Key}' cannot be null.");

            switch (element.Value.parsingTypeCode)
            {
                 case ParsingTypeCode.DoubleTypeCode:

                    var elementDoubleValueString = elementValueString;

                    if (elementValueString.Split(SPACE_SEPARATOR).Length == 2)
                        elementDoubleValueString = elementValueString.Split(SPACE_SEPARATOR)[0];

                    if (!double.TryParse(elementDoubleValueString, out double doubleValue))
                        ThrowExceptionCast(element.Key, targetType);

                    property.SetValue(this, doubleValue);

                    break;

                case ParsingTypeCode.IntTypeCode:

                    if (!int.TryParse(elementValueString, out int intValue))
                        ThrowExceptionCast(element.Key, targetType);

                    property.SetValue(this, intValue);

                    break;

               
                case ParsingTypeCode.DataOnlyTypeCode:

                    if (!DateOnly.TryParseExact(elementValueString, DateOnlyFormat, out DateOnly dateValue))
                        ThrowExceptionCast(element.Key, targetType);

                    property.SetValue(this, dateValue);
                    break;

                case ParsingTypeCode.StringTypeCode:

                    if (elementValueString is null)
                        ThrowExceptionCast(element.Key, targetType);

                    property.SetValue(this, elementValueString);

                    break;

                case ParsingTypeCode.SKURansomsStatus:

                    var skuRansomsStatus = elementValueString switch
                    {
                        "Доставлен на ПВЗ" => SKURansomsStatus.Delivered,
                        "Забран с ПВЗ" => SKURansomsStatus.WithDrawn,
                        "Отменен" => SKURansomsStatus.Canceled,
                        "В пути" => SKURansomsStatus.OnWay,
                        _ => throw new NotImplementedException()
                    };

                    property.SetValue(this, skuRansomsStatus);

                    break;
            }
        }

        #endregion
    }
}