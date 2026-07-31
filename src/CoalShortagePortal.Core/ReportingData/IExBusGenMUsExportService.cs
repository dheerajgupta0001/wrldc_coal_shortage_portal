using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CoalShortagePortal.Core.Interfaces.ReportingData
{
    public interface IExBusGenMUsExportService
    {
        Task GenerateAndUploadFilesAsync();
    }
}
