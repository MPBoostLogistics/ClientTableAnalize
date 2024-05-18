using ClientTableAnalizer.Objects.Parsing;

namespace ClientTableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    /// Shortage data unit for forming SKUShortageBalance.
    /// </summary>
    /// <remarks><i>New version</i></remarks>
    public class SKUShortageDataUnit : ParsingRowEntity
    {
        #region Properties
        public int SKUNumber { get; }
        public int ShortagesCount { get; }
        public int ShortagesSum { get; }
        public string? ShortagesComment { get; }
        #endregion

        #region Constructors
        public SKUShortageDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : base(parsingElements) {}
        #endregion Constructors
    }
}
