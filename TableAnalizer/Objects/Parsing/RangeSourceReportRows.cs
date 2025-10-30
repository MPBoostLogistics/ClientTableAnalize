using System.Text;

namespace TableAnalizer.Objects.Parsing
{
    /// <summary>
    /// Represents a range of source report rows.
    /// </summary>
    public class RangeSourceReportRows(List<SourceColumn> sourceCells, string? key = null, (string key, int offset)? terminatingStringValue = null)
    {
        #region Properties
        /// <summary>
        /// String key with name of room (range), can be empty (for example, if there is only one range).
        /// </summary>
        public string? Key { get; } = key;

        /// <summary>
        /// Terminating string value from which index of the last row is calculated (can be empty)
        /// </summary>
        public (string key, int offset)? TerminatingStringValue { get; } = terminatingStringValue;

        /// <summary>
        /// Collection of SourceColumn objects (describe cource columns of range)
        /// </summary>
        public List<SourceColumn> SourceCells { get; } = sourceCells;

        /// <summary>
        /// A collection of range row indexes.
        /// </summary>
        public IEnumerable<int>? RangeRowIndexesСollection { get; set; }

        /// <summary>
        /// A collection of data on read rows (each row is a dictionary of values).
        /// </summary>
        public List<Dictionary<string, ItemForParsing>> DataOnReadRowsCollection { get; private set; } = [];

        #endregion Properties

        #region Functionality
        public void SetRange(int startRowIndex, int count) =>
            RangeRowIndexesСollection = Enumerable.Range(startRowIndex, count);

        public void SetValues(Dictionary<string, ItemForParsing> values) => DataOnReadRowsCollection.Add(values);
        
        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append(string.IsNullOrEmpty(Key) ? "" : $"key: {Key}, ");
            sb.Append(string.IsNullOrEmpty(TerminatingStringValue?.key) ? "" : $"terminating: {TerminatingStringValue?.key}, ");
            sb.Append($"Source columns count: {SourceCells.Count}, ");
            sb.Append(RangeRowIndexesСollection != null ?
                 $"range row indexes count: {RangeRowIndexesСollection.Count()}, " : "");

            return sb.ToString();
        }
        
        #endregion Functionality
    }
}