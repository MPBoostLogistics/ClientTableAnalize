using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TableAnalizer.Services.Analytics
{
    public interface IAnalyticalService
    {

    }

    public partial class AnaliticalService : IAnalyticalService
    {
        public AnaliticalService(ILogger<AnaliticalService> logger, IOptions<ProgramOptions> options)
        {

        }
    }
}