using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TableAnalizer.Services.Parsing
{
    public interface IParsingService : IService
    {

    }

    public partial class ParsingService : IParsingService
    {
        #region Fields
        private readonly ProgramOptions _options;
        private readonly ILogger<ParsingService> _logger;
        #endregion

        #region Properties
        public string ServiceName { get; private set; }
        #endregion

        public ParsingService(ILogger<ParsingService> logger, IOptions<ProgramOptions> options)
        {
            _logger = logger;
            _options = options.Value;

            ServiceName = GetType().Name;

            if (_options.IsTracing)
            {
                _logger.LogInformation("{TraceInfoFlag} Создан {ServiceName}", _options.TraceInfoFlag, ServiceName);
            }
        }
    }
}