namespace ClientTableAnalizer.Objects.Parsing
{
    /// <summary>
    /// Represents a range of source report rows.
    /// </summary>
    public class RangeSourceReportRows
    {
        #region Properties
        public string? Key { get; }
        public (string key, int offset)? TerminatingStringValue { get; }
        public List<SourceCell> SourceCells { get; }
        public IEnumerable<int>? RowsIndexRange { get; set; }
        public List<Dictionary<string, ItemForParsing>> RowsWithRawData { get; private set; } = new();
        #endregion Properties

        #region  Constructors
        public RangeSourceReportRows(List<SourceCell> sourceCells, string? key = null, (string key, int offset)? terminatingStringValue = null)
        {
            SourceCells = sourceCells;
            Key = key;
            TerminatingStringValue = terminatingStringValue;
        }
        #endregion Constructors

        #region Functionality
        public void SetRange(int startRowIndex, int endRowIndex) =>
            RowsIndexRange = Enumerable.Range(startRowIndex, endRowIndex);

        public void SetValues(Dictionary<string, ItemForParsing> values) => RowsWithRawData.Add(values);
        #endregion Functionality
    }
}