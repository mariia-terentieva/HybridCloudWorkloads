using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HybridCloudWorkloads.Core.Entities;
using HybridCloudWorkloads.Core.Interfaces;
using HybridCloudWorkloads.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HybridCloudWorkloads.Infrastructure.Services;

/// <summary>
/// Фоновый сервис для автоматической синхронизации с облачными провайдерами
/// </summary>
public class CloudProviderSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CloudProviderSyncBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public CloudProviderSyncBackgroundService(
        IServiceProvider services,
        ILogger<CloudProviderSyncBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cloud Provider Sync Background Service started");

        // Первый запуск через 30 секунд после старта
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSyncProvidersAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cloud provider sync background service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Cloud Provider Sync Background Service stopped");
    }

    private async Task CheckAndSyncProvidersAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();

        // === ИСПРАВЛЕНО: Добавлен using для ProviderStatus ===
        var providers = await context.CloudProviders
            .Where(p => p.SyncEnabled && p.Status == HybridCloudWorkloads.Core.Entities.ProviderStatus.Active)
            .ToListAsync(stoppingToken);

        foreach (var provider in providers)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                var needsSync = await syncService.NeedsSyncAsync(provider.Id);
                
                if (needsSync)
                {
                    _logger.LogInformation("Starting scheduled sync for provider {ProviderCode}", provider.Code);
                    var result = await syncService.SyncProviderAsync(provider.Id);
                    
                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "Scheduled sync completed for {ProviderCode}. Changes: {Changes}, Duration: {Duration}ms",
                            provider.Code, result.Statistics.TotalChanges, result.Duration.TotalMilliseconds);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Scheduled sync failed for {ProviderCode}: {Error}",
                            provider.Code, result.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing provider {ProviderCode}", provider.Code);
            }
        }
    }
}