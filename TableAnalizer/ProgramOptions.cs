namespace TableAnalizer
{
    /// <summary>
    /// Опции запуска программы, читаются из config.json и передаются в менеджеры.
    /// </summary>
    public sealed class ProgramOptions
    {
        #region Properties ("Flags" section )
        public bool IsTracing { get; set; }
        public bool IsCheckRansoms { get; set; }
        public bool IsCheckBalance { get; set; }
        public bool IsCheckOutgoingProducts { get; set; }
        public bool IsCheckAdditionalData { get; set; }
        public bool IsManualResorting { get; set; }
        public bool IsAutoResorting { get; set; }
        public bool IsConsiderInProgressStatusInAutoResorting { get; set; }
        public bool IsCombineAliases { get; set; }
        #endregion

        #region Properties ("Messages" section)
        public string? GoodbyMessage { get; set; }
        public string? TraceInfoFlag { get; set; }
        public string? TraceErrorFlag { get; set; }
        #endregion

        #region Properties ("Numbers" section)
        public int DataAgeLimitInDays { get; set; }
        public int DeadlineDays { get; set; }
        public int TraceDelay { get; set; }
        #endregion

        #region Properties ("Formulas" section)
        public string? SumFormula { get; set; }
        #endregion

        #region Properties ("Collections" section)
        public List<string>? ValidFilesExtentionsList { get; set; }
        #endregion

        #region Properties ("Strings" section)
        public string? BaseFinalReportsDirectoryPath { get; set; }
        public string? PermanentDataSourcePath { get; set; }
        public string? AppVersion { get; set; }
        public string? LogisticsReportFirstName { get; set; }
        public string? FullSetStatusString { get; set; }
        public string? InWorkStatusString { get; set; }
        public string? DelayDeliveryFromPointsAndWarehouseStatusString { get; set; }
        public string? DelayDeliveryFromWarehouseStatusString { get; set; }
        public string? DelayDeliveryFromPointsStatusString { get; set; }
        public string? DelayFromOrganizationStatusString { get; set; }
        public string? DelayFromOrganizationAndWarehouseStatusString { get; set; }
        public string? UnknownStatusString { get; set; }
        #endregion
    }
}
