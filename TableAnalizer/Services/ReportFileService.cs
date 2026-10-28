using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TableAnalizer.Services.Parsing
{
    public interface IReportFileService
    {

    }

    public partial class ReportFileService : IReportFileService
    {
        public ReportFileService(ILogger<ReportFileService> logger, IOptions<ProgramOptions> options)
        {

        }
    }
}