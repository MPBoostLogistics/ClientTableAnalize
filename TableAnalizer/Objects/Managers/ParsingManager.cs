using System.Globalization;
using TableAnalizer.Interfaces;

namespace TableAnalizer.Objects.Managers
{
    public class ParsingManager
    {
        #region Fields
        protected const string SPACE_SEPARATOR = "\u0020";

        private static readonly CultureInfo cultureInfo = CultureInfo.GetCultureInfo("ru-RU");

        #endregion

        public static bool Parse(string valueString, ParsingTypeCode parsingTypeCode, bool isRequred, out object? value)
        {
            value = null;

            switch (parsingTypeCode)
            {
                case ParsingTypeCode.IntTypeCode:

                    if (!isRequred && string.IsNullOrEmpty(valueString))
                    {
                        value = 0;
                    }
                    else
                    {
                        value = int.TryParse(valueString, out int intValue) ? intValue : null;
                    }

                    break;

                case ParsingTypeCode.LongTypeCode:

                    if (!isRequred && string.IsNullOrEmpty(valueString))
                    {
                        value = (long)0;
                    }
                    else
                    {
                        value = long.TryParse(valueString, out long longValue) ? longValue : null;
                    }

                    break;

                case ParsingTypeCode.DoubleTypeCode:

                    if (!isRequred && string.IsNullOrEmpty(valueString))
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

                    if (!isRequred && string.IsNullOrEmpty(valueString))
                    {
                        value = new DateOnly(0001, 01, 01);
                    }
                    else
                    {
                        if (int.TryParse(valueString, out int intValue))
                        {
                            value = DateOnly.FromDateTime(DateTime.FromOADate(intValue));
                        }
                        else
                        {
                            value = DateOnly.TryParseExact(valueString, [.. IParsingRowEntity.DateOnlyFormats], cultureInfo, DateTimeStyles.None, out DateOnly dateOnlyValue) ? dateOnlyValue : null;
                        }
                    }
                    break;

                case ParsingTypeCode.StringTypeCode:

                    if (!isRequred && string.IsNullOrEmpty(valueString))
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
                        "Доставлен на ПВЗ" => SKURansomsStatus.DeliveredToPoints,
                        "Забран с ПВЗ" => SKURansomsStatus.PickedupFromPoints,
                        "Отменен" => SKURansomsStatus.Canceled,
                        "В пути" => SKURansomsStatus.InTransit,
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

                case ParsingTypeCode.BoolTypeCode:
                    value = bool.TryParse(valueString, out bool boolValue) ?
                        boolValue :
                        throw new Exception($"Failed to convert string \"{valueString}\" to boolean");
                    break;

                case ParsingTypeCode.SubjectClaim:
                    value = valueString switch
                    {
                        "Возврат начисления" => SubjectClaim.RefundAccrual,
                        "Недовоз товаров" => SubjectClaim.ShortDeliveryGoods,
                        _ => throw new NotImplementedException()
                    };
                    break;

                case ParsingTypeCode.ClaimExecutor:
                    value = valueString switch
                    {
                        "Организация" => ClaimExecutor.Organization,
                        "Организация и склад" => ClaimExecutor.OrganizationAndWarehouse,
                        "Склад" => ClaimExecutor.Warehouse,
                        _ => throw new NotImplementedException()
                    };
                    break;

                case ParsingTypeCode.ClaimResolutionStatus:
                    value = valueString switch
                    {
                        "Не оплачено" => ClaimResolutionStatus.NotPaid,
                        "Докупаем сами" => ClaimResolutionStatus.IndependentPurchase,
                        "Оплачено" => ClaimResolutionStatus.Paid,
                        "Товар найден" => ClaimResolutionStatus.ProductFound,
                        "Возвращено на баланс" => ClaimResolutionStatus.ReturnedToBalance,
                        _ => throw new NotImplementedException()
                    };
                    break;

                case ParsingTypeCode.DirectoryTypeCode:
                    value = Directory.Exists(valueString) ?
                        new DirectoryInfo(valueString) :
                        null;
                    break;

                default:
                    throw new NotImplementedException();
            }

            return value is not null;
        }
    }
}