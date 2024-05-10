using System.Diagnostics;
using System.Reflection;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Interfaces;

namespace ClientTableAnalizer.Objects.Parsing
{
    /// <summary>
    /// An entity for parsing properties from a data dictionary.
    /// </summary>
    public abstract class ParsingRowEntity : IParsingRowEntity
    {
        #region Variables and constants
        protected const string SPACE_SEPARATOR = "\u0020";
        protected readonly List<TypeCode> baseTypeCodes = new() { TypeCode.Double, TypeCode.Int32, TypeCode.DateTime, TypeCode.String };
        #endregion

        #region Properties
        public PropertyInfo[] PropertyInfos { get; private set; }
        #endregion

        #region  Constructors
        public ParsingRowEntity(in Dictionary<string, ItemForParsing> parsingElements)
        {
            PropertyInfos = GetType().GetProperties();

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

        protected static void ThrowExceptionCast(string valueKey, string value, Type type) =>
            throw new InvalidCastException($"Element with key '{valueKey}' and value '{value}' cannot be casting to type {type}.");

        protected void ParseValues(in Dictionary<string, ItemForParsing> parsingElements)
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

        private void ParseValue(in KeyValuePair<string, ItemForParsing> element, ref PropertyInfo property)
        {
            Type targetType = property.PropertyType;

            string? elementValueString = element.Value.Value;
            bool isMaybeNullable = element.Value.IsMaybeNullable;

            if (string.IsNullOrEmpty(elementValueString))
            {
                if (!isMaybeNullable)
                    throw new Exception($"Parsing value with key '{element.Key}' cannot be null.");

                elementValueString = String.Empty;
            }
            else
            {
                elementValueString = elementValueString.Trim();
            }


            switch (element.Value.TypeCode)
            {
                case ParsingTypeCode.DoubleTypeCode:

                    var elementDoubleValueString = elementValueString;
                    if (isMaybeNullable && string.IsNullOrEmpty(elementDoubleValueString))
                    {
                        property.SetValue(this, 0.00);
                    }
                    else
                    {
                        if (elementValueString.Split(SPACE_SEPARATOR).Length == 2)
                            elementDoubleValueString = elementValueString.Split(SPACE_SEPARATOR)[0];

                        if (!double.TryParse(elementDoubleValueString, out double doubleValue))
                            ThrowExceptionCast(element.Key, elementDoubleValueString, targetType);

                        property.SetValue(this, doubleValue);
                    }

                    break;

                case ParsingTypeCode.IntTypeCode:

                    var elementIntValueString = elementValueString;
                    if (isMaybeNullable && string.IsNullOrEmpty(elementIntValueString))
                    {
                        property.SetValue(this, 0);
                    }
                    else
                    {
                        if (!int.TryParse(elementIntValueString, out int intValue))
                            ThrowExceptionCast(element.Key, elementValueString, targetType);

                        property.SetValue(this, intValue);
                    }

                    break;

                //22.03.2024 14:00
                case ParsingTypeCode.DataOnlyTypeCode:

                    var elementDateValueString = elementValueString;

                    if (isMaybeNullable && string.IsNullOrEmpty(elementDateValueString))
                    {
                        property.SetValue(this, new DateOnly(0001, 01, 01));
                    }
                    else
                    {
                        if (!DateOnly.TryParseExact(elementValueString, IParsingRowEntity.DateOnlyFormats.ToArray(), out DateOnly dateOnlyValue))
                            ThrowExceptionCast(element.Key, elementValueString, targetType);

                        property.SetValue(this, dateOnlyValue);
                    }

                    break;

                case ParsingTypeCode.StringTypeCode:

                    if (isMaybeNullable && string.IsNullOrEmpty(elementValueString))
                    {
                        property.SetValue(this, String.Empty);
                    }
                    else
                    {
                        if (elementValueString is null)
                            ThrowExceptionCast(element.Key, elementValueString ?? " ", targetType);

                        property.SetValue(this, elementValueString);
                    }

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