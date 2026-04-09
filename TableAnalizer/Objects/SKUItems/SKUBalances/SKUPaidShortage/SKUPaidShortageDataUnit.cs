using TableAnalizer.Objects.Managers.ExcelSheetParsing;
using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    /// Paid shortage data unit for forming SKUPaidShortageBalance.
    /// </summary>
    public class SKUPaidShortageDataUnit(in Dictionary<string, TableParsingCell> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        public string? ClientLogin { get; private set; }
        public SubjectClaim SubjectClaim { get; private set; }
        public long? SKUNumber { get; private set; }
        public int SKUSum { get; private set; }
        public double SKUAmount { get; private set; }
        public ClaimExecutor ClaimExecutor { get; private set; }
        public ClaimResolutionStatus ClaimResolutionStatus { get; private set; }
        #endregion

        #region Functionality
        public override string ToString()
        {
            return $"SKUPaidShortageDataUnit: {SKUNumber}, sum: {SKUSum}, amount: {SKUAmount}, exec: {ClaimExecutor}";
        }

        #endregion
    }
}