using TableAnalizer.Objects.Managers.ExcelSheetParsing;
using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    class SKURemainingDataRowComparer : IEqualityComparer<SKURemainingDataRow>
    {
        public bool Equals(SKURemainingDataRow? x, SKURemainingDataRow? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.RowNumber == y.RowNumber;
        }

        public int GetHashCode(SKURemainingDataRow entity)
        {
            if (entity is null) return 0;
            int hashEntityCode = entity.RowNumber.GetHashCode();
            return hashEntityCode;
        }
    }




    /// <summary>
    /// Строка с данными по артикулу (баланс товаров на складе).
    /// </summary>
    public class SKURemainingDataRow(int rowNumber, in Dictionary<string, TableParsingCell> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        public int RowNumber { get; } = rowNumber;

        /// <summary>
        /// Товар - артикул.
        /// </summary>
        public long SKUNumber { get; private set; }

        // Товар - бренд и наименование.
        public string SKUBrandAndName { get; private set; } = string.Empty;

        /// <summary>
        /// Приход товаров на склад (товарами)
        /// </summary>
        public int SKUReceipt { get; private set; }

        /// <summary>
        /// Списано товаров по ордерам пересорта.
        /// </summary>
        public int SKUWriteOffOnResortOrders { get; set; } = 0;

        /// <summary>
        /// Остаток товаров на складе.
        /// </summary>
        public int SKURemaining { get; private set; }
        #endregion
    }
}