namespace ClientTableAnalizer.Objects.Parsing
{
    /// <summary>
    /// Represents a range of source report rows.
    /// </summary>
    public class RangeSourceReportRows
    {
        public string? Key { get; }
        public List<SourceCell> SourceCells { get; }
        public IEnumerable<int>? RowsRange { get; set; }

        public RangeSourceReportRows(List<SourceCell> sourceCells,  string? key = null)
        {
            SourceCells = sourceCells;
            Key = key;
        }

        public void SetRange(int startRowIndex, int endRowIndex) => 
            RowsRange = Enumerable.Range(startRowIndex, endRowIndex);
    }
}