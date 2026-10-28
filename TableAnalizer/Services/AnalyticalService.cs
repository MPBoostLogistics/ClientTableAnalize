using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TableAnalizer.Services.Analytics
{
    public interface IAnalyticalService : IService
    {

    }

    public partial class AnalyticalService : IAnalyticalService
    {
        #region Fields
        private readonly ProgramOptions _options;
        private readonly ILogger<AnalyticalService> _logger;
        #endregion

        #region Properties
        public string ServiceName { get; private set; }
        #endregion

        public AnalyticalService(ILogger<AnalyticalService> logger, IOptions<ProgramOptions> options)
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