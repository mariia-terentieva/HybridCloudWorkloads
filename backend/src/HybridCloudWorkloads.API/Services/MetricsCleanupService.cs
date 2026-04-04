using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HybridCloudWorkloads.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HybridCloudWorkloads.API.Services;

public class MetricsCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MetricsCleanupService> _logger;

    public MetricsCleanupService(
        IServiceProvider services,
        ILogger<MetricsCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldMetricsAsync();
                
                // Запускаем раз в день
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in metrics cleanup service");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private async Task CleanupOldMetricsAsync()
    {
        using var scope = _services.CreateScope();
        var metricsRepository = scope.ServiceProvider
            .GetRequiredService<IPerformanceMetricsRepository>();

        // Храним метрики 30 дней
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        
        _logger.LogInformation("Cleaning up metrics older than {CutoffDate}", cutoffDate);
        await metricsRepository.DeleteOldMetricsAsync(cutoffDate);
    }
}