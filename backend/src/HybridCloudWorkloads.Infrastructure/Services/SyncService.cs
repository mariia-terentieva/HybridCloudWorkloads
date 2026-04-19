using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HybridCloudWorkloads.Core.Entities;
using HybridCloudWorkloads.Core.Interfaces;
using HybridCloudWorkloads.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HybridCloudWorkloads.Infrastructure.Services;

/// <summary>
/// Сервис управления синхронизацией с облачными провайдерами
/// </summary>
public class SyncService : ISyncService
{
    private readonly ApplicationDbContext _context;
    private readonly ICloudProviderSyncFactory _syncFactory;
    private readonly ILogger<SyncService> _logger;
    
    private static readonly object _syncLock = new object();
    private static readonly HashSet<Guid> _runningSyncs = new HashSet<Guid>();

    public SyncService(
        ApplicationDbContext context,
        ICloudProviderSyncFactory syncFactory,
        ILogger<SyncService> logger)
    {
        _context = context;
        _syncFactory = syncFactory;
        _logger = logger;
    }

    public async Task<SyncResult> SyncProviderAsync(Guid providerId, bool force = false)
    {
        // Проверяем, не запущена ли уже синхронизация
        lock (_syncLock)
        {
            if (_runningSyncs.Contains(providerId))
            {
                _logger.LogWarning("Sync already running for provider {ProviderId}", providerId);
                return new SyncResult
                {
                    ProviderId = providerId,
                    ProviderCode = string.Empty,
                    Success = false,
                    StartedAt = DateTime.UtcNow,
                    ErrorMessage = "Sync already in progress"
                };
            }
            _runningSyncs.Add(providerId);
        }

        var sw = Stopwatch.StartNew();
        var result = new SyncResult
        {
            ProviderId = providerId,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            var provider = await _context.CloudProviders
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider == null)
            {
                result.Success = false;
                result.ErrorMessage = "Provider not found";
                return result;
            }

            result.ProviderCode = provider.Code;

            // Проверяем, нужна ли синхронизация
            if (!force && !await NeedsSyncAsync(providerId))
            {
                _logger.LogInformation("Provider {ProviderCode} doesn't need sync yet", provider.Code);
                result.Success = true;
                result.CompletedAt = DateTime.UtcNow;
                result.Duration = sw.Elapsed;
                return result;
            }

            // Получаем синхронизатор
            var syncService = _syncFactory.GetSyncService(provider.Code);
            if (syncService == null)
            {
                result.Success = false;
                result.ErrorMessage = $"No sync service for provider {provider.Code}";
                return result;
            }

            _logger.LogInformation("Starting sync for provider {ProviderCode}", provider.Code);

            // 1. Синхронизация регионов
            var regions = await syncService.SyncRegionsAsync(providerId);
            await SyncRegionsToDatabaseAsync(providerId, regions, result.Statistics);
            
            // 2. Синхронизация сервисов
            var services = await syncService.SyncServicesAsync(providerId);
            await SyncServicesToDatabaseAsync(providerId, services, result.Statistics);
            
            // 3. Синхронизация скидок
            var discounts = await syncService.SyncDiscountsAsync(providerId);
            await SyncDiscountsToDatabaseAsync(providerId, discounts, result.Statistics);
            
            // 4. Синхронизация типов инстансов и цен для каждого региона
            var dbRegions = await _context.CloudRegions
                .Where(r => r.ProviderId == providerId)
                .ToListAsync();

            foreach (var region in dbRegions)
            {
                try
                {
                    var instanceTypes = await syncService.SyncInstanceTypesAsync(providerId, region.Id, region.Code);
                    await SyncInstanceTypesToDatabaseAsync(providerId, region.Id, instanceTypes, result.Statistics, syncService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync instance types for region {Region}", region.Code);
                }
            }

            // Обновляем время последней синхронизации
            provider.LastSyncAt = DateTime.UtcNow;
            provider.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            result.Success = true;
            result.CompletedAt = DateTime.UtcNow;
            result.Duration = sw.Elapsed;

            _logger.LogInformation(
                "Sync completed for provider {ProviderCode} in {Duration}ms. Changes: {Changes}",
                provider.Code, sw.ElapsedMilliseconds, result.Statistics.TotalChanges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed for provider {ProviderId}", providerId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            lock (_syncLock)
            {
                _runningSyncs.Remove(providerId);
            }
        }

        return result;
    }

    public async Task<Dictionary<string, SyncResult>> SyncAllProvidersAsync(bool force = false)
    {
        var results = new Dictionary<string, SyncResult>();
        
        var providers = await _context.CloudProviders
            .Where(p => p.SyncEnabled && p.Status == ProviderStatus.Active)
            .ToListAsync();

        foreach (var provider in providers)
        {
            var result = await SyncProviderAsync(provider.Id, force);
            results[provider.Code] = result;
        }

        return results;
    }

    public async Task<SyncStatus> GetSyncStatusAsync(Guid providerId)
    {
        var provider = await _context.CloudProviders
            .FirstOrDefaultAsync(p => p.Id == providerId);

        if (provider == null)
        {
            return new SyncStatus
            {
                ProviderId = providerId,
                ProviderCode = string.Empty,
                LastSyncSuccess = false,
                LastSyncError = "Provider not found"
            };
        }

        var status = new SyncStatus
        {
            ProviderId = providerId,
            ProviderCode = provider.Code,
            LastSyncAt = provider.LastSyncAt,
            LastSyncSuccess = true,
            IsRunning = IsSyncRunning(providerId),
            NextSyncAt = provider.LastSyncAt?.AddMinutes(provider.SyncIntervalMinutes) ?? DateTime.UtcNow
        };

        return status;
    }

    public async Task<bool> NeedsSyncAsync(Guid providerId)
    {
        var provider = await _context.CloudProviders
            .FirstOrDefaultAsync(p => p.Id == providerId);

        if (provider == null || !provider.SyncEnabled)
            return false;

        if (provider.LastSyncAt == null)
            return true;

        var nextSyncTime = provider.LastSyncAt.Value.AddMinutes(provider.SyncIntervalMinutes);
        return DateTime.UtcNow >= nextSyncTime;
    }

    public async Task<DateTime?> GetLastSyncTimeAsync(Guid providerId)
    {
        var provider = await _context.CloudProviders
            .FirstOrDefaultAsync(p => p.Id == providerId);

        return provider?.LastSyncAt;
    }

    private bool IsSyncRunning(Guid providerId)
    {
        lock (_syncLock)
        {
            return _runningSyncs.Contains(providerId);
        }
    }

    private async Task SyncRegionsToDatabaseAsync(
        Guid providerId, 
        List<CloudRegion> regions, 
        SyncStatistics stats)
    {
        var existingRegions = await _context.CloudRegions
            .Where(r => r.ProviderId == providerId)
            .ToDictionaryAsync(r => r.Code);

        foreach (var region in regions)
        {
            if (existingRegions.TryGetValue(region.Code, out var existing))
            {
                // Обновляем существующий
                existing.Name = region.Name;
                existing.DisplayName = region.DisplayName;
                existing.Continent = region.Continent;
                existing.Country = region.Country;
                existing.City = region.City;
                existing.Status = region.Status;
                existing.AvailabilityZones = region.AvailabilityZones;
                existing.Compliance = region.Compliance;
                existing.UpdatedAt = DateTime.UtcNow;
                stats.RegionsUpdated++;
            }
            else
            {
                // Добавляем новый
                await _context.CloudRegions.AddAsync(region);
                stats.RegionsAdded++;
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SyncServicesToDatabaseAsync(
        Guid providerId, 
        List<CloudService> services, 
        SyncStatistics stats)
    {
        var existingServices = await _context.CloudServices
            .Where(s => s.ProviderId == providerId)
            .ToDictionaryAsync(s => s.Code);

        foreach (var service in services)
        {
            if (existingServices.TryGetValue(service.Code, out var existing))
            {
                // Обновляем существующий
                existing.Name = service.Name;
                existing.Description = service.Description;
                existing.DocumentationUrl = service.DocumentationUrl;
                existing.PricingModel = service.PricingModel;
                existing.FreeTier = service.FreeTier;
                existing.SlaInfo = service.SlaInfo;
                existing.UpdatedAt = DateTime.UtcNow;
                stats.ServicesUpdated++;
            }
            else
            {
                // Добавляем новый
                await _context.CloudServices.AddAsync(service);
                stats.ServicesAdded++;
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SyncInstanceTypesToDatabaseAsync(
        Guid providerId,
        Guid regionId,
        List<InstanceType> instanceTypes,
        SyncStatistics stats,
        ICloudProviderSync syncService)
    {
        var existingTypes = await _context.InstanceTypes
            .Where(t => t.ProviderId == providerId && t.RegionId == regionId)
            .ToDictionaryAsync(t => t.TypeCode);

        foreach (var instanceType in instanceTypes)
        {
            if (existingTypes.TryGetValue(instanceType.TypeCode, out var existing))
            {
                // Обновляем существующий
                existing.Category = instanceType.Category;
                existing.Family = instanceType.Family;
                existing.VcpuCount = instanceType.VcpuCount;
                existing.MemoryGb = instanceType.MemoryGb;
                existing.NetworkBandwidthGbps = instanceType.NetworkBandwidthGbps;
                existing.Availability = instanceType.Availability;
                existing.UpdatedAt = DateTime.UtcNow;
                stats.InstanceTypesUpdated++;
            }
            else
            {
                // Добавляем новый
                await _context.InstanceTypes.AddAsync(instanceType);
                stats.InstanceTypesAdded++;
                
                // Создаем цену для нового типа
                var region = await _context.CloudRegions.FindAsync(regionId);
                if (region != null)
                {
                    var pricing = await syncService.SyncPricingAsync(
                        instanceType.Id, 
                        instanceType.TypeCode, 
                        region.Code);
                    
                    await _context.InstancePricing.AddAsync(pricing);
                    stats.PricingsUpdated++;
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SyncDiscountsToDatabaseAsync(
        Guid providerId, 
        List<Discount> discounts, 
        SyncStatistics stats)
    {
        var existingDiscounts = await _context.Discounts
            .Where(d => d.ProviderId == providerId)
            .ToDictionaryAsync(d => d.Name);

        foreach (var discount in discounts)
        {
            if (existingDiscounts.TryGetValue(discount.Name, out var existing))
            {
                // Обновляем существующий
                existing.Description = discount.Description;
                existing.DiscountPercent = discount.DiscountPercent;
                existing.ValidUntil = discount.ValidUntil;
                existing.Status = discount.Status;
                existing.UpdatedAt = DateTime.UtcNow;
                stats.DiscountsUpdated++;
            }
            else
            {
                // Добавляем новый
                await _context.Discounts.AddAsync(discount);
                stats.DiscountsAdded++;
            }
        }

        await _context.SaveChangesAsync();
    }
}