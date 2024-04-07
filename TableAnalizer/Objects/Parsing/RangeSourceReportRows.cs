namespace ClientTableAnalizer.Objects.Parsing
{
    /// <summary>
    /// Represents a range of source report rows.
    /// </summary>
    public class RangeSourceReportRows
    {
        public string? Key { get; }
        public (string key, int offset)? TerminatingStringValue { get; }
        public List<SourceCell> SourceCells { get; }
        public IEnumerable<int>? RowsRange { get; set; }

        public RangeSourceReportRows(List<SourceCell> sourceCells,  string? key = null, (string key, int offset)? terminatingStringValue = null)
        {
            SourceCells = sourceCells;
            Key = key;
            TerminatingStringValue = terminatingStringValue;
        }

        public void SetRange(int startRowIndex, int endRowIndex) => 
            RowsRange = Enumerable.Range(startRowIndex, endRowIndex);
    }
}