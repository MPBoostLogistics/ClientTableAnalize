using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TableAnalizer.Services.Parsing
{
    public interface IParsingService
    {

    }

    public partial class ParsingService : IParsingService
    {
        public ParsingService(ILogger<ParsingService> logger, IOptions<ProgramOptions> options)
        {

        }
    }
}