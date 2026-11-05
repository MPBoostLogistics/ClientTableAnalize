namespace TableAnalizer.Application
{
    using Microsoft.Extensions.Logging;
    using TableAnalizer.Console;
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
            // Workflow приложения
            // Получить постоянные данные приложения и проверить их, лог
            // Получить постоянные данные приложения и проверить их, лог 



            // var reportsDirectory =
            //     await _consoleService.GetPrimaryDataFromConsole();

            // _logger.LogInformation(
            //     "Директория с клиентскими отчетами: {Directory}",
            //     reportsDirectory);

            // var permanentDataIsValid =
            //     await _reportService.CheckOfAvailailityData(
            //         null,
            //         PrimaryDataType.Permanent);

            // if (!permanentDataIsValid)
            // {
            //     _logger.LogCritical("Постоянные данные не валидны.");
            //     return 1;
            // }

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