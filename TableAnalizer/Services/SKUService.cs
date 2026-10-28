using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TableAnalizer.Services.SKU
{
    public interface ISKUService
    {

    }

    public partial class SKUService : ISKUService
    {
        public SKUService(ILogger<SKUService> logger, IOptions<ProgramOptions> options)
        {

        }
    }
}