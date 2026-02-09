using System.Diagnostics;
using System.Reflection;
using TableAnalizer.Interfaces;
using TableAnalizer.Objects.Managers;

namespace TableAnalizer.Objects.Parsing
{
    /// <summary>
    /// An entity for parsing properties from a data dictionary.
    /// </summary>
    public abstract class ParsingRowEntity : IParsingRowEntity
    {
        #region Variables and constants
        protected const string SPACE_SEPARATOR = "\u0020";
        //protected readonly List<TypeCode> baseTypeCodes = [TypeCode.Double, TypeCode.Int32, TypeCode.DateTime, TypeCode.String];
        #endregion

        #region Properties
        public PropertyInfo[] PropertyInfos { get; private set; }
        #endregion

        #region Constructors
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

            if (ParsingManager.Parse(elementValueString, element.Value.TypeCode, isMaybeNullable, out object? value)
                && value is not null)
            {
                if (property.CanWrite)
                {
                    switch (element.Value.TypeCode)
                    {
                        case ParsingTypeCode.DoubleTypeCode:
                            property.SetValue(this, (double)value);
                            break;

                        case ParsingTypeCode.IntTypeCode:
                            property.SetValue(this, (int)value);
                            break;

                        case ParsingTypeCode.LongTypeCode:
                            property.SetValue(this, (long)value);
                            break;

                        case ParsingTypeCode.LongTypeArrayCode:
                            property.SetValue(this, (List<long>)value);
                            break;

                        //22.03.2024 14:00
                        //06.11.2024
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
                        case ParsingTypeCode.BoolTypeCode:
                            property.SetValue(this, (bool)value);
                            break;
                        case ParsingTypeCode.ClaimExecutor:
                            property.SetValue(this, (ClaimExecutor)value);
                            break;
                        case ParsingTypeCode.SubjectClaim:
                            property.SetValue(this, (SubjectClaim)value);
                            break;
                        case ParsingTypeCode.ClaimResolutionStatus:
                            property.SetValue(this, (ClaimResolutionStatus)value);
                            break;

                    }
                }
            }
        }
        #endregion
    }
}