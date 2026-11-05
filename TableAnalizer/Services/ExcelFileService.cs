using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TableAnalizer.Configuration;

namespace TableAnalizer.Services
{
    public interface IExcelFileService : IService
    {

    }

    public partial class ExcelFileService : IExcelFileService
    {
        #region Fields
        private readonly ProgramOptions _options;
        private readonly ILogger<ExcelFileService> _logger;
        #endregion

        #region Properties
        public string ServiceName { get; private set; }
        #endregion

        public ExcelFileService(ILogger<ExcelFileService> logger, IOptions<ProgramOptions> options)
        {
            _logger = logger;
            _options = options.Value;

            ServiceName = GetType().Name;

            if (_options.IsTracing)
            {
                _logger.LogInformation("Создана служба {ServiceName}", ServiceName);
            }
        }
    }
}