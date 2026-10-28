using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TableAnalizer.Services.Parsing
{
    public interface IReportFileService : IService
    {

    }

    public partial class ReportFileService : IReportFileService
    {
        #region Fields
        private readonly ProgramOptions _options;
        private readonly ILogger<ReportFileService> _logger;
        #endregion

        #region Properties
        public string ServiceName { get; private set; }
        #endregion

        public ReportFileService(ILogger<ReportFileService> logger, IOptions<ProgramOptions> options)
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