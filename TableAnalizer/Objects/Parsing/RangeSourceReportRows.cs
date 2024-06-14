namespace TableAnalizer.Objects.Parsing
{
    /// <summary>
    /// Represents a range of source report rows.
    /// </summary>
    public class RangeSourceReportRows
    {
        #region Properties
        /// <summary>
        /// String key with name of room (range), can be empty (for example, if there is only one range).
        /// </summary>
        public string? Key { get; }

        /// <summary>
        /// Terminating string value from which index of the last row is calculated (can be empty)
        /// </summary>
        public (string key, int offset)? TerminatingStringValue { get; }

        /// <summary>
        /// Collection of SourceColumn objects (describe cource columns of range)
        /// </summary>
        public List<SourceColumn> SourceCells { get; }

        /// <summary>
        /// A collection of range row indexes.
        /// </summary>
        public IEnumerable<int>? СollectionRangeRowIndexes { get; set; }

        /// <summary>
        /// A collection of data on read rows (each row is a dictionary of values).
        /// </summary>
        public List<Dictionary<string, ItemForParsing>> CollectionDataOnReadRows { get; private set; } = new();
        #endregion Properties

        #region  Constructors
        public RangeSourceReportRows(List<SourceColumn> sourceCells, string? key = null, (string key, int offset)? terminatingStringValue = null)
        {
            SourceCells = sourceCells;
            Key = key;
            TerminatingStringValue = terminatingStringValue;
        }
        #endregion Constructors

        #region Functionality
        public void SetRange(int startRowIndex, int count) =>
            СollectionRangeRowIndexes = Enumerable.Range(startRowIndex, count);

        public void SetValues(Dictionary<string, ItemForParsing> values) => CollectionDataOnReadRows.Add(values);
        #endregion Functionality
    }
}