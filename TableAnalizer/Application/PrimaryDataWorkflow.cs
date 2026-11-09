namespace TableAnalizer.Application
{
    using Microsoft.Extensions.Logging;
    using TableAnalizer.Console;
    using TableAnalizer.Objects;
    using TableAnalizer.Services;

    public sealed class PrimaryDataWorkflow(
        IParsingService parsingService,
        IConsoleService consoleService,
        IReportFileService reportService,
        IExcelFileService excelFileService,
        ISKUService skuService,
        IAnalyticalService analyticalService,
        ILogger<PrimaryDataWorkflow> logger) : IPrimaryDataWorkflow
    {
        #region Fields
        private readonly IParsingService _parsingService = parsingService;
        private readonly IConsoleService _consoleService = consoleService;
        private readonly IReportFileService _reportService = reportService;
        private readonly IExcelFileService _excelFileService = excelFileService;
        private readonly ISKUService _skuService = skuService;
        private readonly IAnalyticalService _analyticalService = analyticalService;
        private readonly ILogger<PrimaryDataWorkflow> _logger = logger;
        #endregion

        public async Task<int> RunAsync(
            CancellationToken cancellationToken = default)
        {
            // var clientDataDirectory = _consoleService.GetPrimaryDataFromConsole();
            // if (clientDataDirectory is not null)
            //     _logger.LogInformation("Директория с клиентскими отчетами: {Directory}", clientDataDirectory.FullName);

            var permanentDataIsValid = _reportService.CheckOfAvailailityData(null, PrimaryDataType.Constant);
            if (!permanentDataIsValid)
            {
                _logger.LogCritical("Постоянные данные не валидны.");
                return 1;
            }
            _logger.LogInformation("Постоянные данные валидны.");


            // var variableDataIsValid =
            //     await _reportService.CheckOfAvailailityData(
            //         reportsDirectory.FullName,
            //         PrimaryDataType.Variable);

            // if (!variableDataIsValid)
            // {
            //     _logger.LogCritical("Переменные данные не валидны.");
            //     return 1;
            // }

            // Здесь размещается основная цепочка:
            // await _excelFileService....;
            // await _skuService....;
            // await _analyticalService....;

            _logger.LogInformation("Приложение успешно завершило работу");
            return 0;
        }
    }
}