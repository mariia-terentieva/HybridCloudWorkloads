using System;
using System.Threading;
using System.Threading.Tasks;
using HybridCloudWorkloads.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HybridCloudWorkloads.Infrastructure.Services;

/// <summary>
/// Фоновый сервис для периодической очистки и обновления кэша
/// </summary>
public class CacheCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CacheCleanupBackgroundService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6);

    public CacheCleanupBackgroundService(
        IServiceProvider services,
        ILogger<CacheCleanupBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cache Cleanup Background Service started");

        // Первый запуск через 5 минут
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupCacheAsync(stoppingToken);
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cache cleanup background service");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        _logger.LogInformation("Cache Cleanup Background Service stopped");
    }

    private async Task CleanupCacheAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<IPricingCacheService>();

        try
        {
            _logger.LogInformation("Starting cache cleanup");
            
            var stats = await cacheService.GetCacheStatisticsAsync();
            _logger.LogInformation(
                "Cache stats before cleanup: {HitRatio:P} hit ratio, {Size} bytes estimated",
                stats.HitRatio, stats.TotalCacheSizeBytes);

            // Предзагрузка популярных данных
            await PreloadPopularDataAsync(cacheService, stoppingToken);
            
            _logger.LogInformation("Cache cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup cache");
        }
    }

    private async Task PreloadPopularDataAsync(IPricingCacheService cacheService, CancellationToken stoppingToken)
    {
        try
        {
            // Предзагружаем провайдеров
            var providers = await cacheService.GetProvidersAsync(true);
            
            // Предзагружаем популярные сравнения
            var popularConfigs = new[]
            {
                (cpu: 2, memory: 4.0),
                (cpu: 2, memory: 8.0),
                (cpu: 4, memory: 16.0),
                (cpu: 8, memory: 32.0)
            };

            foreach (var config in popularConfigs)
            {
                if (stoppingToken.IsCancellationRequested) break;
                
                try
                {
                    await cacheService.ComparePricesAsync(config.cpu, config.memory);
                    await cacheService.GetBestPriceOffersAsync(config.cpu, config.memory);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to preload cache for {Cpu} CPU, {Memory} GB", 
                        config.cpu, config.memory);
                }
            }

            _logger.LogInformation("Preloaded cache with popular data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preload cache data");
        }
    }
}