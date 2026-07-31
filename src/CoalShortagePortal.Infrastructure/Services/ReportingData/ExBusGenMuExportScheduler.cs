using CoalShortagePortal.Core.Interfaces.ReportingData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoalShortagePortal.Infrastructure.Services.ReportingData
{
    public class ExBusGenMuExportScheduler : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExBusGenMuExportScheduler> _logger;

        public ExBusGenMuExportScheduler(
            IServiceScopeFactory scopeFactory,
            ILogger<ExBusGenMuExportScheduler> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;

                    if (now.Hour == 4 && now.Minute == 0)
                        //if (true)
                    {
                        using var scope =
                            _scopeFactory.CreateScope();

                        var service =
                            scope.ServiceProvider
                                 .GetRequiredService<IExBusGenMUsExportService>();

                        await service.GenerateAndUploadFilesAsync();

                        _logger.LogInformation(
                            "SCADA export executed successfully");

                        await Task.Delay(
                            TimeSpan.FromMinutes(1),
                            stoppingToken);
                    }

                    await Task.Delay(
                        TimeSpan.FromSeconds(30),
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "SCADA export scheduler failed");
                }
            }
        }
    }
}