using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HybridCloudWorkloads.Core.Entities;

namespace HybridCloudWorkloads.Core.Interfaces;

/// <summary>
/// Сервис кэширования данных о ценах и типах инстансов
/// </summary>
public interface IPricingCacheService
{
    /// <summary>
    /// Получить список провайдеров из кэша
    /// </summary>
    Task<List<CloudProvider>> GetProvidersAsync(bool forceRefresh = false);
    
    /// <summary>
    /// Получить провайдера по коду
    /// </summary>
    Task<CloudProvider?> GetProviderByCodeAsync(string providerCode);
    
    /// <summary>
    /// Получить список регионов провайдера
    /// </summary>
    Task<List<CloudRegion>> GetRegionsAsync(Guid providerId, bool forceRefresh = false);
    
    /// <summary>
    /// Получить регион по коду
    /// </summary>
    Task<CloudRegion?> GetRegionByCodeAsync(Guid providerId, string regionCode);
    
    /// <summary>
    /// Получить типы инстансов для региона
    /// </summary>
    Task<List<InstanceType>> GetInstanceTypesAsync(Guid providerId, Guid regionId, bool forceRefresh = false);
    
    /// <summary>
    /// Получить типы инстансов по фильтрам
    /// </summary>
    Task<List<InstanceType>> GetInstanceTypesFilteredAsync(
        Guid providerId, 
        string? regionCode = null,
        int? minCpu = null, 
        int? maxCpu = null,
        double? minMemory = null, 
        double? maxMemory = null,
        string? category = null,
        bool forceRefresh = false);
    
    /// <summary>
    /// Получить цены для типа инстанса
    /// </summary>
    Task<InstancePricing?> GetPricingAsync(Guid instanceTypeId, bool forceRefresh = false);
    
    /// <summary>
    /// Получить цены для нескольких типов инстансов
    /// </summary>
    Task<Dictionary<Guid, InstancePricing>> GetBatchPricingAsync(
        IEnumerable<Guid> instanceTypeIds, 
        bool forceRefresh = false);
    
    /// <summary>
    /// Получить сравнение цен между провайдерами
    /// </summary>
    Task<ProviderPriceComparison> ComparePricesAsync(
        int cpu, 
        double memoryGb, 
        IEnumerable<string>? providerCodes = null);
    
    /// <summary>
    /// Получить лучшие предложения по заданным критериям
    /// </summary>
    Task<List<BestPriceOffer>> GetBestPriceOffersAsync(
        int cpu, 
        double memoryGb,
        string? preferredRegion = null,
        bool includeSpot = true,
        bool includeReserved = true);
    
    /// <summary>
    /// Получить все скидки провайдера
    /// </summary>
    Task<List<Discount>> GetDiscountsAsync(Guid providerId, bool forceRefresh = false);
    
    /// <summary>
    /// Инвалидировать кэш для провайдера
    /// </summary>
    Task InvalidateProviderCacheAsync(Guid providerId);
    
    /// <summary>
    /// Инвалидировать весь кэш
    /// </summary>
    Task InvalidateAllCacheAsync();
    
    /// <summary>
    /// Получить статистику кэша
    /// </summary>
    Task<CacheStatistics> GetCacheStatisticsAsync();
}

/// <summary>
/// Сравнение цен между провайдерами
/// </summary>
public class ProviderPriceComparison
{
    public int Cpu { get; set; }
    public double MemoryGb { get; set; }
    public DateTime ComparedAt { get; set; } = DateTime.UtcNow;
    public List<ProviderPriceOption> Options { get; set; } = new();
    public ProviderPriceOption? BestOption { get; set; }
    public ProviderPriceOption? CheapestOption { get; set; }
    public ProviderPriceOption? BestPerformanceOption { get; set; }
}

/// <summary>
/// Вариант цены от провайдера
/// </summary>
public class ProviderPriceOption
{
    public string ProviderCode { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public string InstanceType { get; set; } = string.Empty;
    public string InstanceCategory { get; set; } = string.Empty;
    public int Vcpu { get; set; }
    public double MemoryGb { get; set; }
    public decimal OnDemandHourly { get; set; }
    public decimal OnDemandMonthly { get; set; }
    public decimal? SpotHourly { get; set; }
    public decimal? SpotSavingsPercent { get; set; }
    public decimal? Reserved1YearHourly { get; set; }
    public decimal? Reserved3YearHourly { get; set; }
    public double NetworkBandwidthGbps { get; set; }
    public string Currency { get; set; } = "USD";
    public double Score { get; set; } // Комплексная оценка
}

/// <summary>
/// Лучшее ценовое предложение
/// </summary>
public class BestPriceOffer
{
    public string ProviderCode { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public string InstanceType { get; set; } = string.Empty;
    public int Vcpu { get; set; }
    public double MemoryGb { get; set; }
    public string PricingModel { get; set; } = "OnDemand";
    public decimal HourlyPrice { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal SavingsPercent { get; set; }
    public string Currency { get; set; } = "USD";
    public List<string> Features { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Статистика кэша
/// </summary>
public class CacheStatistics
{
    public int CachedProviders { get; set; }
    public int CachedRegions { get; set; }
    public int CachedInstanceTypes { get; set; }
    public int CachedPricings { get; set; }
    public long TotalCacheSizeBytes { get; set; }
    public DateTime LastCleanup { get; set; }
    public Dictionary<string, int> HitsPerKey { get; set; } = new();
    public Dictionary<string, int> MissesPerKey { get; set; } = new();
    public double HitRatio => HitsPerKey.Values.Sum() / 
        (double)(HitsPerKey.Values.Sum() + MissesPerKey.Values.Sum() + 1);
}