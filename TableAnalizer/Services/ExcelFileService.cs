using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TableAnalizer.Services.Excel
{
    public interface IExcelService
    {

    }

    public partial class ExcelService : IExcelService
    {
        public ExcelService(ILogger<ExcelService> logger, IOptions<ProgramOptions> options)
        {

        }
    }
}