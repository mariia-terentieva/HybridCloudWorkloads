using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HybridCloudWorkloads.Core.Entities;
using HybridCloudWorkloads.Core.Interfaces;
using HybridCloudWorkloads.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HybridCloudWorkloads.Infrastructure.Services;

/// <summary>
/// Сервис кэширования данных о ценах с использованием IMemoryCache
/// </summary>
public class PricingCacheService : IPricingCacheService, IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private readonly ISyncService _syncService;
    private readonly ILogger<PricingCacheService> _logger;
    
    private readonly ConcurrentDictionary<string, int> _hits = new();
    private readonly ConcurrentDictionary<string, int> _misses = new();
    private readonly SemaphoreSlim _cleanupLock = new(1, 1);
    private DateTime _lastCleanup = DateTime.UtcNow;
    
    // Настройки кэширования
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromHours(1);
    private static readonly TimeSpan PricingCacheDuration = TimeSpan.FromHours(6);
    private static readonly TimeSpan LongCacheDuration = TimeSpan.FromDays(1);
    
    // Ключи кэша
    private const string ProvidersKey = "pricing:providers";
    private const string ProviderByCodePrefix = "pricing:provider:code:";
    private const string RegionsPrefix = "pricing:regions:";
    private const string RegionByCodePrefix = "pricing:region:code:";
    private const string InstanceTypesPrefix = "pricing:instancetypes:";
    private const string InstanceTypesFilteredPrefix = "pricing:instancetypes:filtered:";
    private const string PricingPrefix = "pricing:instance:";
    private const string DiscountsPrefix = "pricing:discounts:";
    private const string ComparisonPrefix = "pricing:comparison:";
    private const string BestOffersPrefix = "pricing:bestoffers:";

    public PricingCacheService(
        ApplicationDbContext context,
        IMemoryCache memoryCache,
        ISyncService syncService,
        ILogger<PricingCacheService> logger)
    {
        _context = context;
        _memoryCache = memoryCache;
        _syncService = syncService;
        _logger = logger;
    }

    #region Public Methods

    public async Task<List<CloudProvider>> GetProvidersAsync(bool forceRefresh = false)
    {
        var cacheKey = ProvidersKey;
        
        if (!forceRefresh && _memoryCache.TryGetValue(cacheKey, out List<CloudProvider>? cached))
        {
            IncrementHits(cacheKey);
            return cached ?? new List<CloudProvider>();
        }
        
        IncrementMisses(cacheKey);
        
        var providers = await _context.CloudProviders
            .Where(p => p.Status == ProviderStatus.Active)
            .OrderBy(p => p.DisplayName)
            .ToListAsync();
        
        _memoryCache.Set(cacheKey, providers, LongCacheDuration);
        _logger.LogDebug("Cached {Count} providers", providers.Count);
        
        return providers;
    }

    public async Task<CloudProvider?> GetProviderByCodeAsync(string providerCode)
    {
        var cacheKey = $"{ProviderByCodePrefix}{providerCode}";
        
        if (_memoryCache.TryGetValue(cacheKey, out CloudProvider? cached))
        {
            IncrementHits(cacheKey);
            return cached;
        }
        
        IncrementMisses(cacheKey);
        
        var provider = await _context.CloudProviders
            .FirstOrDefaultAsync(p => p.Code == providerCode && p.Status == ProviderStatus.Active);
        
        if (provider != null)
        {
            _memoryCache.Set(cacheKey, provider, LongCacheDuration);
        }
        
        return provider;
    }

    public async Task<List<CloudRegion>> GetRegionsAsync(Guid providerId, bool forceRefresh = false)
    {
        var cacheKey = $"{RegionsPrefix}{providerId}";
        
        if (!forceRefresh && _memoryCache.TryGetValue(cacheKey, out List<CloudRegion>? cached))
        {
            IncrementHits(cacheKey);
            return cached ?? new List<CloudRegion>();
        }
        
        IncrementMisses(cacheKey);
        
        var regions = await _context.CloudRegions
            .Where(r => r.ProviderId == providerId && r.Status == RegionStatus.Available)
            .OrderBy(r => r.Continent)
            .ThenBy(r => r.DisplayName)
            .ToListAsync();
        
        _memoryCache.Set(cacheKey, regions, LongCacheDuration);
        _logger.LogDebug("Cached {Count} regions for provider {ProviderId}", regions.Count, providerId);
        
        return regions;
    }

    public async Task<CloudRegion?> GetRegionByCodeAsync(Guid providerId, string regionCode)
    {
        var cacheKey = $"{RegionByCodePrefix}{providerId}:{regionCode}";
        
        if (_memoryCache.TryGetValue(cacheKey, out CloudRegion? cached))
        {
            IncrementHits(cacheKey);
            return cached;
        }
        
        IncrementMisses(cacheKey);
        
        var region = await _context.CloudRegions
            .FirstOrDefaultAsync(r => r.ProviderId == providerId && r.Code == regionCode);
        
        if (region != null)
        {
            _memoryCache.Set(cacheKey, region, LongCacheDuration);
        }
        
        return region;
    }

    public async Task<List<InstanceType>> GetInstanceTypesAsync(
        Guid providerId, 
        Guid regionId, 
        bool forceRefresh = false)
    {
        var cacheKey = $"{InstanceTypesPrefix}{providerId}:{regionId}";
        
        if (!forceRefresh && _memoryCache.TryGetValue(cacheKey, out List<InstanceType>? cached))
        {
            IncrementHits(cacheKey);
            return cached ?? new List<InstanceType>();
        }
        
        IncrementMisses(cacheKey);
        
        // Проверяем, нужна ли синхронизация
        if (await _syncService.NeedsSyncAsync(providerId))
        {
            _logger.LogInformation("Triggering sync for provider {ProviderId} before caching", providerId);
            _ = Task.Run(() => _syncService.SyncProviderAsync(providerId));
        }
        
        var instanceTypes = await _context.InstanceTypes
            .Where(t => t.ProviderId == providerId && t.RegionId == regionId)
            .Where(t => t.Availability == InstanceAvailability.Available)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.VcpuCount)
            .ThenBy(t => t.MemoryGb)
            .ToListAsync();
        
        _memoryCache.Set(cacheKey, instanceTypes, DefaultCacheDuration);
        _logger.LogDebug("Cached {Count} instance types for region {RegionId}", instanceTypes.Count, regionId);
        
        return instanceTypes;
    }

    public async Task<List<InstanceType>> GetInstanceTypesFilteredAsync(
        Guid providerId,
        string? regionCode = null,
        int? minCpu = null,
        int? maxCpu = null,
        double? minMemory = null,
        double? maxMemory = null,
        string? category = null,
        bool forceRefresh = false)
    {
        // Генерируем ключ кэша на основе фильтров
        var filterHash = $"{providerId}|{regionCode}|{minCpu}|{maxCpu}|{minMemory}|{maxMemory}|{category}";
        var cacheKey = $"{InstanceTypesFilteredPrefix}{filterHash.GetHashCode()}";
        
        if (!forceRefresh && _memoryCache.TryGetValue(cacheKey, out List<InstanceType>? cached))
        {
            IncrementHits(cacheKey);
            return cached ?? new List<InstanceType>();
        }
        
        IncrementMisses(cacheKey);
        
        var query = _context.InstanceTypes
            .Where(t => t.ProviderId == providerId)
            .Where(t => t.Availability == InstanceAvailability.Available);
        
        if (!string.IsNullOrEmpty(regionCode))
        {
            query = query.Where(t => t.Region.Code == regionCode);
        }
        
        if (minCpu.HasValue)
        {
            query = query.Where(t => t.VcpuCount >= minCpu.Value);
        }
        
        if (maxCpu.HasValue)
        {
            query = query.Where(t => t.VcpuCount <= maxCpu.Value);
        }
        
        if (minMemory.HasValue)
        {
            query = query.Where(t => t.MemoryGb >= minMemory.Value);
        }
        
        if (maxMemory.HasValue)
        {
            query = query.Where(t => t.MemoryGb <= maxMemory.Value);
        }
        
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(t => t.Category == category);
        }
        
        var instanceTypes = await query
            .Include(t => t.Region)
            .OrderBy(t => t.VcpuCount)
            .ThenBy(t => t.MemoryGb)
            .ToListAsync();
        
        _memoryCache.Set(cacheKey, instanceTypes, DefaultCacheDuration);
        _logger.LogDebug("Cached {Count} filtered instance types", instanceTypes.Count);
        
        return instanceTypes;
    }

    public async Task<InstancePricing?> GetPricingAsync(Guid instanceTypeId, bool forceRefresh = false)
    {
        var cacheKey = $"{PricingPrefix}{instanceTypeId}";
        
        if (!forceRefresh && _memoryCache.TryGetValue(cacheKey, out InstancePricing? cached))
        {
            IncrementHits(cacheKey);
            return cached;
        }
        
        IncrementMisses(cacheKey);
        
        var pricing = await _context.InstancePricing
            .FirstOrDefaultAsync(p => p.InstanceTypeId == instanceTypeId && p.ExpirationDate == null)
            ?? await _context.InstancePricing
                .Where(p => p.InstanceTypeId == instanceTypeId)
                .OrderByDescending(p => p.EffectiveDate)
                .FirstOrDefaultAsync();
        
        if (pricing != null)
        {
            _memoryCache.Set(cacheKey, pricing, PricingCacheDuration);
        }
        
        return pricing;
    }

    public async Task<Dictionary<Guid, InstancePricing>> GetBatchPricingAsync(
        IEnumerable<Guid> instanceTypeIds,
        bool forceRefresh = false)
    {
        var result = new Dictionary<Guid, InstancePricing>();
        var idsToFetch = new List<Guid>();
        
        // Проверяем кэш
        foreach (var id in instanceTypeIds.Distinct())
        {
            var cacheKey = $"{PricingPrefix}{id}";
            
            if (!forceRefresh && _memoryCache.TryGetValue(cacheKey, out InstancePricing? cached) && cached != null)
            {
                IncrementHits(cacheKey);
                result[id] = cached;
            }
            else
            {
                IncrementMisses(cacheKey);
                idsToFetch.Add(id);
            }
        }
        
        // Загружаем отсутствующие из БД
        if (idsToFetch.Any())
        {
            var pricings = await _context.InstancePricing
                .Where(p => idsToFetch.Contains(p.InstanceTypeId))
                .GroupBy(p => p.InstanceTypeId)
                .Select(g => g.OrderByDescending(p => p.EffectiveDate).FirstOrDefault())
                .ToListAsync();
            
            foreach (var pricing in pricings.Where(p => p != null))
            {
                var cacheKey = $"{PricingPrefix}{pricing!.InstanceTypeId}";
                _memoryCache.Set(cacheKey, pricing, PricingCacheDuration);
                result[pricing.InstanceTypeId] = pricing;
            }
        }
        
        return result;
    }

    public async Task<ProviderPriceComparison> ComparePricesAsync(
        int cpu,
        double memoryGb,
        IEnumerable<string>? providerCodes = null)
    {
        var cacheKey = $"{ComparisonPrefix}{cpu}:{memoryGb}:{string.Join(",", providerCodes ?? Array.Empty<string>())}";
        
        if (_memoryCache.TryGetValue(cacheKey, out ProviderPriceComparison? cached) && cached != null)
        {
            IncrementHits(cacheKey);
            return cached;
        }
        
        IncrementMisses(cacheKey);
        
        var comparison = new ProviderPriceComparison
        {
            Cpu = cpu,
            MemoryGb = memoryGb,
            ComparedAt = DateTime.UtcNow
        };
        
        // Получаем всех активных провайдеров
        var providers = await GetProvidersAsync();
        
        if (providerCodes?.Any() == true)
        {
            providers = providers.Where(p => providerCodes.Contains(p.Code)).ToList();
        }
        
        foreach (var provider in providers)
        {
            // Находим подходящие типы инстансов
            var instanceTypes = await GetInstanceTypesFilteredAsync(
                provider.Id,
                minCpu: cpu,
                maxCpu: cpu * 2,
                minMemory: memoryGb * 0.8,
                maxMemory: memoryGb * 2
            );
            
            foreach (var instanceType in instanceTypes.Take(10)) // Ограничиваем количество
            {
                var pricing = await GetPricingAsync(instanceType.Id);
                
                if (pricing != null)
                {
                    var option = new ProviderPriceOption
                    {
                        ProviderCode = provider.Code,
                        ProviderName = provider.DisplayName,
                        RegionCode = instanceType.Region?.Code ?? string.Empty,
                        RegionName = instanceType.Region?.DisplayName ?? string.Empty,
                        InstanceType = instanceType.TypeCode,
                        InstanceCategory = instanceType.Category,
                        Vcpu = (int)instanceType.VcpuCount,
                        MemoryGb = instanceType.MemoryGb,
                        OnDemandHourly = pricing.OnDemandHourly,
                        OnDemandMonthly = pricing.OnDemandMonthly,
                        SpotHourly = pricing.SpotCurrentPrice,
                        SpotSavingsPercent = pricing.SpotSavingsPercent,
                        Reserved1YearHourly = pricing.Reserved1YearAllUpfront / 730,
                        Reserved3YearHourly = pricing.Reserved3YearAllUpfront / 730,
                        NetworkBandwidthGbps = instanceType.NetworkBandwidthGbps,
                        Currency = pricing.Currency,
                        Score = CalculateScore(instanceType, pricing, cpu, memoryGb)
                    };
                    
                    comparison.Options.Add(option);
                }
            }
        }
        
        // Сортируем и находим лучшие варианты
        comparison.Options = comparison.Options
            .OrderBy(o => o.OnDemandHourly)
            .ToList();
        
        comparison.CheapestOption = comparison.Options.FirstOrDefault();
        comparison.BestOption = comparison.Options
            .OrderByDescending(o => o.Score)
            .FirstOrDefault();
        comparison.BestPerformanceOption = comparison.Options
            .OrderByDescending(o => o.Vcpu)
            .ThenByDescending(o => o.NetworkBandwidthGbps)
            .FirstOrDefault();
        
        _memoryCache.Set(cacheKey, comparison, DefaultCacheDuration);
        
        return comparison;
    }

    public async Task<List<BestPriceOffer>> GetBestPriceOffersAsync(
        int cpu,
        double memoryGb,
        string? preferredRegion = null,
        bool includeSpot = true,
        bool includeReserved = true)
    {
        var cacheKey = $"{BestOffersPrefix}{cpu}:{memoryGb}:{preferredRegion}:{includeSpot}:{includeReserved}";
        
        if (_memoryCache.TryGetValue(cacheKey, out List<BestPriceOffer>? cached) && cached != null)
        {
            IncrementHits(cacheKey);
            return cached;
        }
        
        IncrementMisses(cacheKey);
        
        var offers = new List<BestPriceOffer>();
        var providers = await GetProvidersAsync();
        
        foreach (var provider in providers)
        {
            var instanceTypes = await GetInstanceTypesFilteredAsync(
                provider.Id,
                regionCode: preferredRegion,
                minCpu: cpu,
                maxCpu: cpu * 3,
                minMemory: memoryGb * 0.7,
                maxMemory: memoryGb * 3
            );
            
            foreach (var instanceType in instanceTypes.OrderBy(t => Math.Abs(t.VcpuCount - cpu)))
            {
                var pricing = await GetPricingAsync(instanceType.Id);
                
                if (pricing == null) continue;
                
                // On-Demand предложение
                offers.Add(CreateOffer(instanceType, pricing, provider, "OnDemand", 
                    pricing.OnDemandHourly, pricing.OnDemandMonthly, 0));
                
                // Spot предложение
                if (includeSpot && pricing.SpotCurrentPrice > 0)
                {
                    offers.Add(CreateOffer(instanceType, pricing, provider, "Spot",
                        pricing.SpotCurrentPrice.Value, 
                        pricing.SpotCurrentPrice.Value * 730,
                        pricing.SpotSavingsPercent ?? 0));
                }
                
                // Reserved предложения
                if (includeReserved)
                {
                    if (pricing.Reserved1YearAllUpfront > 0)
                    {
                        var hourly = pricing.Reserved1YearAllUpfront.Value / 8760;
                        offers.Add(CreateOffer(instanceType, pricing, provider, "Reserved-1Y",
                            hourly, pricing.Reserved1YearAllUpfront.Value / 12,
                            pricing.Reserved1YearSavingsPercent ?? 0));
                    }
                    
                    if (pricing.Reserved3YearAllUpfront > 0)
                    {
                        var hourly = pricing.Reserved3YearAllUpfront.Value / 26280;
                        offers.Add(CreateOffer(instanceType, pricing, provider, "Reserved-3Y",
                            hourly, pricing.Reserved3YearAllUpfront.Value / 36,
                            pricing.Reserved3YearSavingsPercent ?? 0));
                    }
                }
            }
        }
        
        // Группируем и сортируем
        var bestOffers = offers
            .GroupBy(o => new { o.ProviderCode, o.PricingModel })
            .Select(g => g.OrderBy(o => o.HourlyPrice).First())
            .OrderBy(o => o.HourlyPrice)
            .Take(20)
            .ToList();
        
        _memoryCache.Set(cacheKey, bestOffers, DefaultCacheDuration);
        
        return bestOffers;
    }

    public async Task<List<Discount>> GetDiscountsAsync(Guid providerId, bool forceRefresh = false)
    {
        var cacheKey = $"{DiscountsPrefix}{providerId}";
        
        if (!forceRefresh && _memoryCache.TryGetValue(cacheKey, out List<Discount>? cached))
        {
            IncrementHits(cacheKey);
            return cached ?? new List<Discount>();
        }
        
        IncrementMisses(cacheKey);
        
        var discounts = await _context.Discounts
            .Where(d => d.ProviderId == providerId)
            .Where(d => d.Status == DiscountStatus.Active)
            .Where(d => d.ValidUntil == null || d.ValidUntil > DateTime.UtcNow)
            .OrderByDescending(d => d.DiscountPercent)
            .ToListAsync();
        
        _memoryCache.Set(cacheKey, discounts, LongCacheDuration);
        
        return discounts;
    }

    public async Task InvalidateProviderCacheAsync(Guid providerId)
    {
        _logger.LogInformation("Invalidating cache for provider {ProviderId}", providerId);
        
        // Удаляем все ключи, связанные с провайдером
        var keysToRemove = new List<string>();
        
        // Здесь можно реализовать более точное удаление через отслеживание ключей
        // Для простоты - инвалидируем основные ключи
        _memoryCache.Remove(ProvidersKey);
        _memoryCache.Remove($"{RegionsPrefix}{providerId}");
        _memoryCache.Remove($"{DiscountsPrefix}{providerId}");
        
        await Task.CompletedTask;
    }

    public async Task InvalidateAllCacheAsync()
    {
        _logger.LogInformation("Invalidating all pricing cache");
        
        if (_memoryCache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }
        
        _hits.Clear();
        _misses.Clear();
        
        await Task.CompletedTask;
    }

    public async Task<CacheStatistics> GetCacheStatisticsAsync()
    {
        var stats = new CacheStatistics
        {
            LastCleanup = _lastCleanup,
            HitsPerKey = new Dictionary<string, int>(_hits),
            MissesPerKey = new Dictionary<string, int>(_misses)
        };
        
        // Подсчитываем количество закэшированных объектов
        if (_memoryCache.TryGetValue(ProvidersKey, out List<CloudProvider>? providers))
        {
            stats.CachedProviders = providers?.Count ?? 0;
        }
        
        // Примерная оценка размера кэша
        stats.TotalCacheSizeBytes = EstimateCacheSize();
        
        return stats;
    }

    #endregion

    #region Private Methods

    private void IncrementHits(string key)
    {
        _hits.AddOrUpdate(key, 1, (_, count) => count + 1);
    }

    private void IncrementMisses(string key)
    {
        _misses.AddOrUpdate(key, 1, (_, count) => count + 1);
    }

    private double CalculateScore(InstanceType instanceType, InstancePricing pricing, int targetCpu, double targetMemory)
    {
        double score = 100;
        
        // Соответствие CPU
        var cpuDiff = Math.Abs(instanceType.VcpuCount - targetCpu);
        score -= cpuDiff * 5;
        
        // Соответствие памяти
        var memoryDiff = Math.Abs(instanceType.MemoryGb - targetMemory) / targetMemory;
        score -= memoryDiff * 20;
        
        // Цена
        var priceScore = 30 * (1 - (double)(pricing.OnDemandHourly / 0.5m));
        score += Math.Max(0, priceScore);
        
        // Производительность сети
        score += instanceType.NetworkBandwidthGbps * 2;
        
        // Категория инстанса
        score += instanceType.Category switch
        {
            "Compute Optimized" => 10,
            "General Purpose" => 5,
            "Memory Optimized" => 5,
            _ => 0
        };
        
        return Math.Max(0, Math.Min(100, score));
    }

    private BestPriceOffer CreateOffer(
        InstanceType instanceType,
        InstancePricing pricing,
        CloudProvider provider,
        string pricingModel,
        decimal hourlyPrice,
        decimal monthlyPrice,
        decimal savingsPercent)
    {
        return new BestPriceOffer
        {
            ProviderCode = provider.Code,
            ProviderName = provider.DisplayName,
            RegionCode = instanceType.Region?.Code ?? string.Empty,
            RegionName = instanceType.Region?.DisplayName ?? string.Empty,
            InstanceType = instanceType.TypeCode,
            Vcpu = (int)instanceType.VcpuCount,
            MemoryGb = instanceType.MemoryGb,
            PricingModel = pricingModel,
            HourlyPrice = hourlyPrice,
            MonthlyPrice = monthlyPrice,
            SavingsPercent = savingsPercent,
            Currency = pricing.Currency,
            Features = GetFeatures(instanceType),
            Metadata = new Dictionary<string, object>
            {
                ["instanceTypeId"] = instanceType.Id,
                ["category"] = instanceType.Category,
                ["networkBandwidthGbps"] = instanceType.NetworkBandwidthGbps,
                ["hasGpu"] = instanceType.HasGpu
            }
        };
    }

    private List<string> GetFeatures(InstanceType instanceType)
    {
        var features = new List<string>();
        
        if (instanceType.EbsOptimized) features.Add("EBS Optimized");
        if (instanceType.EnhancedNetworking) features.Add("Enhanced Networking");
        if (instanceType.HasGpu) features.Add($"GPU: {instanceType.GpuModel}");
        if (instanceType.NetworkBandwidthGbps >= 10) features.Add("High Network");
        if (instanceType.CpuArchitecture == "arm64") features.Add("ARM64");
        
        return features;
    }

    private long EstimateCacheSize()
    {
        // Примерная оценка
        long size = 0;
        size += _hits.Count * 1024;
        size += _misses.Count * 512;
        return size;
    }

    #endregion

    #region Background Cleanup

    public void Dispose()
    {
        _cleanupLock?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}