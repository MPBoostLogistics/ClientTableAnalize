using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TableAnalizer.Configuration;

namespace TableAnalizer.Services
{
    public interface ISKUService : IService
    {
    }

    public partial class SKUService : ISKUService
    {
        #region Fields
        private readonly ProgramOptions _options;
        private readonly ILogger<SKUService> _logger;
        #endregion

        #region Properties
        public string ServiceName { get; private set; }
        #endregion

        public SKUService(ILogger<SKUService> logger, IOptions<ProgramOptions> options)
        {
            _logger = logger;
            _options = options.Value;

            ServiceName = GetType().Name;

            if (_options.IsTracing)
            {
                _logger.LogInformation("Создана служба  {ServiceName}", ServiceName);
            }
        }
    }
}