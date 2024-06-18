using System.Diagnostics;
using System.Reflection;
using TableAnalizer.Enums;
using TableAnalizer.Interfaces;

namespace TableAnalizer.Objects.Parsing
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
                var targetProperty = PropertyInfos
                    .FirstOrDefault(p => p.Name == parsingElement.Key);
                    
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
            string? elementValueString = element.Value.Value;
            bool isMaybeNullable = element.Value.IsMaybeNullable;

            if (string.IsNullOrEmpty(elementValueString))
            {
                if (!isMaybeNullable)
                    throw new Exception($"Parsing value with key '{element.Key}' cannot be null.");

                elementValueString = string.Empty;
            }
            else
            {
                elementValueString = elementValueString.Trim();
            }

            if(Parse(elementValueString, element.Value.TypeCode, isMaybeNullable, out object? value) 
                && value is not null) 
            {
                if(property.CanWrite) 
                {
                    switch (element.Value.TypeCode)
                    {
                        case ParsingTypeCode.DoubleTypeCode:
                            property.SetValue(this, (double)value);
                            break;

                        case ParsingTypeCode.IntTypeCode:
                            property.SetValue(this, (int)value);
                            break;

                        //22.03.2024 14:00
                        case ParsingTypeCode.DataOnlyTypeCode:
                            property.SetValue(this, (DateOnly)value);
                            break;

                        case ParsingTypeCode.StringTypeCode:
                            property.SetValue(this, (string)value);
                            break;

                        case ParsingTypeCode.SKURansomsStatus:
                            property.SetValue(this, (SKURansomsStatus)value);
                            break;

                        case ParsingTypeCode.Marketplace:
                            property.SetValue(this, (Marketplace)value);
                            break;
                    }
                }
            }
        }

        public static bool Parse(string valueString, ParsingTypeCode parsingTypeCode, bool isMaybeNullable,  out object? value) 
        {
            value = null;

            switch (parsingTypeCode)
            {
                case ParsingTypeCode.IntTypeCode:

                    if (isMaybeNullable && string.IsNullOrEmpty(valueString))
                    {
                        value = 0;
                    }
                    else
                    {
                        value = int.TryParse(valueString, out int intValue) ? intValue : null;
                    }
                    break;

                case ParsingTypeCode.DoubleTypeCode:

                    if (isMaybeNullable && string.IsNullOrEmpty(valueString))
                    {
                        value = 0.00;
                    }
                    else
                    {
                        if (valueString.Split(SPACE_SEPARATOR).Length == 2)
                            valueString = valueString.Split(SPACE_SEPARATOR)[0];

                        value = double.TryParse(valueString, out double doubleValue) ? doubleValue : null;
                    }
                    break;

                case ParsingTypeCode.DataOnlyTypeCode:

                    if (isMaybeNullable && string.IsNullOrEmpty(valueString))
                    {
                        value = new DateOnly(0001, 01, 01);
                    }
                    else
                    {
                        value = DateOnly.TryParseExact(valueString, IParsingRowEntity.DateOnlyFormats.ToArray(), out DateOnly dateOnlyValue) ? dateOnlyValue : null;
                    }
                    break;

                case ParsingTypeCode.StringTypeCode:

                    if (isMaybeNullable && string.IsNullOrEmpty(valueString))
                    {
                        value = String.Empty;
                    }
                    else
                    {
                        value = valueString is not null ? valueString!.Trim() : null;
                    }
                    break;

                case ParsingTypeCode.SKURansomsStatus:

                    value = valueString switch
                    {
                        "Доставлен на ПВЗ" => SKURansomsStatus.Delivered,
                        "Забран с ПВЗ" => SKURansomsStatus.WithDrawn,
                        "Отменен" => SKURansomsStatus.Canceled,
                        "В пути" => SKURansomsStatus.OnWay,
                        _ => throw new NotImplementedException()
                    };

                    break;

                case ParsingTypeCode.Marketplace:

                    value = valueString switch
                    {
                        "wb" or "WB" or "Wb" or "Wildberries" => Marketplace.WB,
                        "oz" or "Oz" or "OZ" or "Ozon" => Marketplace.OZ,
                        _ => throw new NotImplementedException(),
                    };
                    break;

                default:
                    throw new NotImplementedException();
            }

            return value is not null;
        }

        #endregion
    }
}