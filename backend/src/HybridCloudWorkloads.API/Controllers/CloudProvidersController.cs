using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HybridCloudWorkloads.Core.Entities;
using HybridCloudWorkloads.Core.Interfaces;
using HybridCloudWorkloads.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HybridCloudWorkloads.API.Controllers;

[Authorize]
[ApiController]
[Route("api/providers")]
public class CloudProvidersController : ControllerBase
{
    private readonly IPricingCacheService _cacheService;
    private readonly ISyncService _syncService;
    private readonly ICloudProviderSyncFactory _syncFactory;
    private readonly ILogger<CloudProvidersController> _logger;

    public CloudProvidersController(
        IPricingCacheService cacheService,
        ISyncService syncService,
        ICloudProviderSyncFactory syncFactory,
        ILogger<CloudProvidersController> logger)
    {
        _cacheService = cacheService;
        _syncService = syncService;
        _syncFactory = syncFactory;
        _logger = logger;
    }

    #region Providers

    /// <summary>
    /// Получить список всех облачных провайдеров
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ProviderDto>>> GetProviders([FromQuery] bool includeInactive = false)
    {
        var providers = await _cacheService.GetProvidersAsync();
        
        if (!includeInactive)
        {
            providers = providers.Where(p => p.Status == ProviderStatus.Active).ToList();
        }

        var result = providers.Select(p => new ProviderDto
        {
            Id = p.Id,
            Code = p.Code,
            DisplayName = p.DisplayName,
            Description = p.Description,
            LogoUrl = p.LogoUrl,
            Status = p.Status.ToString(),
            SyncEnabled = p.SyncEnabled,
            LastSyncAt = p.LastSyncAt,
            SupportedFeatures = GetSupportedFeatures(p.Code),
            SyncIntervalMinutes = p.SyncIntervalMinutes, 
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Получить детальную информацию о провайдере
    /// </summary>
    [HttpGet("{providerCode}")]
    public async Task<ActionResult<ProviderDetailDto>> GetProvider(string providerCode)
    {
        var provider = await _cacheService.GetProviderByCodeAsync(providerCode);
        if (provider == null)
        {
            return NotFound($"Provider '{providerCode}' not found");
        }

        var regions = await _cacheService.GetRegionsAsync(provider.Id);
        var services = await GetServicesAsync(provider.Id);
        var syncStatus = await _syncService.GetSyncStatusAsync(provider.Id);

        var result = new ProviderDetailDto
        {
            Id = provider.Id,
            Code = provider.Code,
            DisplayName = provider.DisplayName,
            Description = provider.Description,
            LogoUrl = provider.LogoUrl,
            ApiEndpoint = provider.ApiEndpoint,
            AuthType = provider.AuthType,
            Status = provider.Status.ToString(),
            SyncEnabled = provider.SyncEnabled,
            SyncIntervalMinutes = provider.SyncIntervalMinutes,
            LastSyncAt = provider.LastSyncAt,
            CreatedAt = provider.CreatedAt,
            UpdatedAt = provider.UpdatedAt,
            RegionsCount = regions.Count,
            ServicesCount = services.Count,
            SyncStatus = new SyncStatusDto
            {
                IsRunning = syncStatus.IsRunning,
                LastSyncSuccess = syncStatus.LastSyncSuccess,
                LastSyncError = syncStatus.LastSyncError,
                NextSyncAt = syncStatus.NextSyncAt
            },
            SupportedFeatures = GetSupportedFeatures(provider.Code)
        };

        return Ok(result);
    }

    /// <summary>
    /// Получить список поддерживаемых провайдеров (коды)
    /// </summary>
    [HttpGet("supported")]
    public ActionResult<List<string>> GetSupportedProviders()
    {
        var supported = _syncFactory.GetSupportedProviders().ToList();
        return Ok(supported);
    }

    /// <summary>
    /// Проверить доступность API провайдера
    /// </summary>
    [HttpGet("{providerCode}/availability")]
    public async Task<ActionResult<AvailabilityResponse>> CheckAvailability(string providerCode)
    {
        var syncService = _syncFactory.GetSyncService(providerCode);
        if (syncService == null)
        {
            return NotFound($"Provider '{providerCode}' not supported");
        }

        var isAvailable = await syncService.IsApiAvailableAsync();
        
        return Ok(new AvailabilityResponse
        {
            ProviderCode = providerCode,
            IsAvailable = isAvailable,
            CheckedAt = DateTime.UtcNow
        });
    }

    #endregion

    #region Regions

    /// <summary>
    /// Получить список регионов провайдера
    /// </summary>
    [HttpGet("{providerCode}/regions")]
    public async Task<ActionResult<List<RegionDto>>> GetRegions(
        string providerCode, 
        [FromQuery] bool forceRefresh = false)
    {
        var provider = await _cacheService.GetProviderByCodeAsync(providerCode);
        if (provider == null)
        {
            return NotFound($"Provider '{providerCode}' not found");
        }

        var regions = await _cacheService.GetRegionsAsync(provider.Id, forceRefresh);
        
        var result = regions.Select(r => new RegionDto
        {
            Id = r.Id,
            Code = r.Code,
            Name = r.Name,
            DisplayName = r.DisplayName,
            Continent = r.Continent,
            Country = r.Country,
            City = r.City,
            Status = r.Status.ToString(),
            AvailabilityZones = r.AvailabilityZones,
            Compliance = r.Compliance != null ? System.Text.Json.JsonSerializer.Deserialize<string[]>(r.Compliance) : null
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Получить детальную информацию о регионе
    /// </summary>
    [HttpGet("{providerCode}/regions/{regionCode}")]
    public async Task<ActionResult<RegionDetailDto>> GetRegion(string providerCode, string regionCode)
    {
        var provider = await _cacheService.GetProviderByCodeAsync(providerCode);
        if (provider == null)
        {
            return NotFound($"Provider '{providerCode}' not found");
        }

        var region = await _cacheService.GetRegionByCodeAsync(provider.Id, regionCode);
        if (region == null)
        {
            return NotFound($"Region '{regionCode}' not found");
        }

        var instanceTypes = await _cacheService.GetInstanceTypesAsync(provider.Id, region.Id);
        
        // Группировка по категориям для статистики
        var categoryStats = instanceTypes
            .GroupBy(t => t.Category)
            .Select(g => new CategoryStatDto
            {
                Category = g.Key,
                Count = g.Count(),
                MinVcpu = (int)g.Min(t => t.VcpuCount),
                MaxVcpu = (int)g.Max(t => t.VcpuCount),
                MinMemory = g.Min(t => t.MemoryGb),
                MaxMemory = g.Max(t => t.MemoryGb)
            }).ToList();

        var result = new RegionDetailDto
        {
            Id = region.Id,
            Code = region.Code,
            Name = region.Name,
            DisplayName = region.DisplayName,
            Continent = region.Continent,
            Country = region.Country,
            City = region.City,
            Coordinates = region.Coordinates,
            Status = region.Status.ToString(),
            AvailabilityZones = region.AvailabilityZones,
            Compliance = region.Compliance != null ? System.Text.Json.JsonSerializer.Deserialize<string[]>(region.Compliance) : null,
            CreatedAt = region.CreatedAt,
            UpdatedAt = region.UpdatedAt,
            TotalInstanceTypes = instanceTypes.Count,
            CategoryStats = categoryStats
        };

        return Ok(result);
    }

    #endregion

    #region Instance Types

    /// <summary>
    /// Получить типы инстансов для региона
    /// </summary>
    [HttpGet("{providerCode}/instance-types")]
    public async Task<ActionResult<InstanceTypesResponse>> GetInstanceTypes(
        string providerCode,
        [FromQuery] string? regionCode = null,
        [FromQuery] int? minCpu = null,
        [FromQuery] int? maxCpu = null,
        [FromQuery] double? minMemory = null,
        [FromQuery] double? maxMemory = null,
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] bool forceRefresh = false)
    {
        var provider = await _cacheService.GetProviderByCodeAsync(providerCode);
        if (provider == null)
        {
            return NotFound($"Provider '{providerCode}' not found");
        }

        var instanceTypes = await _cacheService.GetInstanceTypesFilteredAsync(
            provider.Id, regionCode, minCpu, maxCpu, minMemory, maxMemory, category, forceRefresh);

        // Пагинация
        var totalCount = instanceTypes.Count;
        var pagedTypes = instanceTypes
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Получаем цены для типов инстансов
        var instanceTypeIds = pagedTypes.Select(t => t.Id).ToList();
        var pricings = await _cacheService.GetBatchPricingAsync(instanceTypeIds);

        var result = new InstanceTypesResponse
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = pagedTypes.Select(t => new InstanceTypeDto
            {
                Id = t.Id,
                TypeCode = t.TypeCode,
                DisplayName = t.DisplayName,
                Category = t.Category,
                Family = t.Family,
                VcpuCount = t.VcpuCount,
                MemoryGb = t.MemoryGb,
                CpuArchitecture = t.CpuArchitecture,
                CpuModel = t.CpuModel,
                NetworkBandwidthGbps = t.NetworkBandwidthGbps,
                NetworkPerformance = t.NetworkPerformance,
                StorageType = t.StorageType,
                LocalStorageGb = t.LocalStorageGb,
                HasGpu = t.HasGpu,
                GpuModel = t.GpuModel,
                GpuCount = t.GpuCount,
                Availability = t.Availability.ToString(),
                RegionCode = t.Region?.Code,
                RegionName = t.Region?.DisplayName,
                Pricing = pricings.TryGetValue(t.Id, out var pricing) ? new PricingDto
                {
                    Currency = pricing.Currency,
                    OnDemandHourly = pricing.OnDemandHourly,
                    OnDemandMonthly = pricing.OnDemandMonthly,
                    SpotCurrentPrice = pricing.SpotCurrentPrice,
                    SpotSavingsPercent = pricing.SpotSavingsPercent,
                    Reserved1YearHourly = pricing.Reserved1YearAllUpfront / 8760,
                    Reserved3YearHourly = pricing.Reserved3YearAllUpfront / 26280
                } : null
            }).ToList(),
            AvailableFilters = new FilterOptionsDto
            {
                Categories = instanceTypes.Select(t => t.Category).Distinct().OrderBy(c => c).ToList(),
                Regions = instanceTypes.Select(t => t.Region?.Code).Where(c => c != null).Distinct().OrderBy(c => c).ToList(),
                CpuRange = new RangeDto { Min = (int)instanceTypes.Min(t => t.VcpuCount), Max = (int)instanceTypes.Max(t => t.VcpuCount) },
                MemoryRange = new RangeDto { Min = instanceTypes.Min(t => t.MemoryGb), Max = instanceTypes.Max(t => t.MemoryGb) }
            }
        };

        return Ok(result);
    }

    /// <summary>
    /// Получить детальную информацию о типе инстанса
    /// </summary>
    [HttpGet("{providerCode}/instance-types/{typeCode}")]
    public async Task<ActionResult<InstanceTypeDetailDto>> GetInstanceType(
        string providerCode, 
        string typeCode,
        [FromQuery] string? regionCode = null)
    {
        var provider = await _cacheService.GetProviderByCodeAsync(providerCode);
        if (provider == null)
        {
            return NotFound($"Provider '{providerCode}' not found");
        }

        // Ищем тип инстанса
        var instanceTypes = await _cacheService.GetInstanceTypesFilteredAsync(
            provider.Id, regionCode);
        
        var instanceType = instanceTypes.FirstOrDefault(t => 
            t.TypeCode.Equals(typeCode, StringComparison.OrdinalIgnoreCase));
        
        if (instanceType == null)
        {
            return NotFound($"Instance type '{typeCode}' not found");
        }

        var pricing = await _cacheService.GetPricingAsync(instanceType.Id);
        var region = instanceType.Region ?? await _cacheService.GetRegionByCodeAsync(provider.Id, regionCode ?? "");

        var result = new InstanceTypeDetailDto
        {
            Id = instanceType.Id,
            TypeCode = instanceType.TypeCode,
            DisplayName = instanceType.DisplayName,
            Description = instanceType.Description,
            Category = instanceType.Category,
            Family = instanceType.Family,
            Generation = instanceType.Generation,
            VcpuCount = instanceType.VcpuCount,
            CpuArchitecture = instanceType.CpuArchitecture,
            CpuModel = instanceType.CpuModel,
            CpuClockSpeedGhz = instanceType.CpuClockSpeedGhz,
            CpuType = instanceType.CpuType,
            PhysicalProcessor = instanceType.PhysicalProcessor,
            MemoryGb = instanceType.MemoryGb,
            NetworkBandwidthGbps = instanceType.NetworkBandwidthGbps,
            NetworkPerformance = instanceType.NetworkPerformance,
            EnhancedNetworking = instanceType.EnhancedNetworking,
            StorageType = instanceType.StorageType,
            LocalStorageGb = instanceType.LocalStorageGb,
            LocalStorageDisks = instanceType.LocalStorageDisks,
            EbsOptimized = instanceType.EbsOptimized,
            MaxEbsBandwidthMbps = instanceType.MaxEbsBandwidthMbps,
            MaxIops = instanceType.MaxIops,
            HasGpu = instanceType.HasGpu,
            GpuModel = instanceType.GpuModel,
            GpuCount = instanceType.GpuCount,
            GpuMemoryGb = instanceType.GpuMemoryGb,
            HasFpga = instanceType.HasFpga,
            VirtualizationType = instanceType.VirtualizationType,
            PlacementGroupSupported = instanceType.PlacementGroupSupported,
            DedicatedHostSupported = instanceType.DedicatedHostSupported,
            Availability = instanceType.Availability.ToString(),
            Region = region != null ? new RegionInfoDto
            {
                Code = region.Code,
                Name = region.DisplayName,
                AvailabilityZones = region.AvailabilityZones
            } : null,
            Pricing = pricing != null ? new PricingDetailDto
            {
                Currency = pricing.Currency,
                OnDemand = new OnDemandPricingDto
                {
                    Hourly = pricing.OnDemandHourly,
                    Monthly = pricing.OnDemandMonthly
                },
                Spot = pricing.SpotCurrentPrice.HasValue ? new SpotPricingDto
                {
                    CurrentPrice = pricing.SpotCurrentPrice.Value,
                    AveragePrice = pricing.SpotAveragePrice,
                    MinPrice = pricing.SpotMinPrice,
                    MaxPrice = pricing.SpotMaxPrice,
                    SavingsPercent = pricing.SpotSavingsPercent,
                    InterruptionRate = pricing.SpotInterruptionRate
                } : null,
                Reserved = new ReservedPricingDto
                {
                    OneYear = pricing.Reserved1YearAllUpfront.HasValue ? new ReservedOptionDto
                    {
                        Upfront = pricing.Reserved1YearAllUpfront.Value,
                        HourlyEquivalent = pricing.Reserved1YearAllUpfront.Value / 8760,
                        SavingsPercent = pricing.Reserved1YearSavingsPercent
                    } : null,
                    ThreeYear = pricing.Reserved3YearAllUpfront.HasValue ? new ReservedOptionDto
                    {
                        Upfront = pricing.Reserved3YearAllUpfront.Value,
                        HourlyEquivalent = pricing.Reserved3YearAllUpfront.Value / 26280,
                        SavingsPercent = pricing.Reserved3YearSavingsPercent
                    } : null
                },
                AdditionalCosts = new AdditionalCostsDto
                {
                    StorageGbMonthly = pricing.StorageGbMonthly,
                    DataTransferOutGb = pricing.DataTransferOutGb,
                    DataTransferInGb = pricing.DataTransferInGb,
                    DataTransferInterRegionGb = pricing.DataTransferInterRegionGb,
                    StaticIpMonthly = pricing.StaticIpMonthly,
                    LoadBalancerHourly = pricing.LoadBalancerHourly
                }
            } : null
        };

        return Ok(result);
    }

    /// <summary>
    /// Сравнить типы инстансов
    /// </summary>
    [HttpPost("instance-types/compare")]
    public async Task<ActionResult<InstanceTypesComparisonDto>> CompareInstanceTypes(
        [FromBody] CompareInstanceTypesRequest request)
    {
        var result = new InstanceTypesComparisonDto
        {
            Items = new List<InstanceTypeDetailDto>()
        };

        foreach (var item in request.Items)
        {
            var provider = await _cacheService.GetProviderByCodeAsync(item.ProviderCode);
            if (provider == null) continue;

            var instanceTypes = await _cacheService.GetInstanceTypesFilteredAsync(
                provider.Id, item.RegionCode);
            
            var instanceType = instanceTypes.FirstOrDefault(t => 
                t.TypeCode.Equals(item.TypeCode, StringComparison.OrdinalIgnoreCase));
            
            if (instanceType != null)
            {
                var pricing = await _cacheService.GetPricingAsync(instanceType.Id);
                var detail = await GetInstanceTypeDetail(instanceType, pricing, provider, item.RegionCode);
                result.Items.Add(detail);
            }
        }

        // Вычисляем сравнение
        if (result.Items.Count >= 2)
        {
            result.Comparison = new ComparisonSummaryDto
            {
                BestValue = result.Items.OrderBy(i => i.Pricing?.OnDemand?.Hourly ?? decimal.MaxValue).FirstOrDefault()?.TypeCode,
                BestPerformance = result.Items.OrderByDescending(i => i.VcpuCount).FirstOrDefault()?.TypeCode,
                BestMemory = result.Items.OrderByDescending(i => i.MemoryGb).FirstOrDefault()?.TypeCode,
                BestNetwork = result.Items.OrderByDescending(i => i.NetworkBandwidthGbps).FirstOrDefault()?.TypeCode
            };
        }

        return Ok(result);
    }

    #endregion

    #region Services

    /// <summary>
    /// Получить список сервисов провайдера
    /// </summary>
    [HttpGet("{providerCode}/services")]
    public async Task<ActionResult<List<ServiceDto>>> GetServices(string providerCode)
    {
        var provider = await _cacheService.GetProviderByCodeAsync(providerCode);
        if (provider == null)
        {
            return NotFound($"Provider '{providerCode}' not found");
        }

        var services = await GetServicesAsync(provider.Id);
        
        var result = services.Select(s => new ServiceDto
        {
            Id = s.Id,
            Code = s.Code,
            Name = s.Name,
            ServiceType = s.ServiceType,
            Description = s.Description,
            DocumentationUrl = s.DocumentationUrl,
            HasFreeTier = s.FreeTier != null
        }).ToList();

        return Ok(result);
    }

    #endregion

    #region Pricing & Comparison

    /// <summary>
    /// Получить сравнение цен между провайдерами
    /// </summary>
    [HttpGet("compare")]
    public async Task<ActionResult<ProviderPriceComparison>> ComparePrices(
        [FromQuery] int cpu,
        [FromQuery] double memory,
        [FromQuery] string? providers = null)
    {
        var providerList = providers?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var comparison = await _cacheService.ComparePricesAsync(cpu, memory, providerList);
        return Ok(comparison);
    }

    /// <summary>
    /// Получить лучшие ценовые предложения
    /// </summary>
    [HttpGet("best-offers")]
    public async Task<ActionResult<List<BestPriceOffer>>> GetBestOffers(
        [FromQuery] int cpu,
        [FromQuery] double memory,
        [FromQuery] string? region = null,
        [FromQuery] bool includeSpot = true,
        [FromQuery] bool includeReserved = true)
    {
        var offers = await _cacheService.GetBestPriceOffersAsync(
            cpu, memory, region, includeSpot, includeReserved);
        return Ok(offers);
    }

    /// <summary>
    /// Получить рекомендации по инстансам для workload
    /// </summary>
    [HttpPost("recommendations")]
    public async Task<ActionResult<RecommendationsResponse>> GetRecommendations(
        [FromBody] RecommendationsRequest request)
    {
        var recommendations = new List<InstanceRecommendationDto>();
        
        foreach (var providerCode in request.Providers ?? new[] { "aws", "azure", "gcp" })
        {
            var provider = await _cacheService.GetProviderByCodeAsync(providerCode);
            if (provider == null) continue;

            var instanceTypes = await _cacheService.GetInstanceTypesFilteredAsync(
                provider.Id,
                regionCode: request.PreferredRegion,
                minCpu: request.Cpu,
                maxCpu: request.Cpu * 2,
                minMemory: request.Memory * 0.7,
                maxMemory: request.Memory * 2,
                category: request.PreferredCategory);

            foreach (var instanceType in instanceTypes.Take(10))
            {
                var pricing = await _cacheService.GetPricingAsync(instanceType.Id);
                if (pricing == null) continue;

                var recommendation = new InstanceRecommendationDto
                {
                    ProviderCode = provider.Code,
                    ProviderName = provider.DisplayName,
                    RegionCode = instanceType.Region?.Code ?? "",
                    RegionName = instanceType.Region?.DisplayName ?? "",
                    InstanceType = instanceType.TypeCode,
                    Vcpu = (int)instanceType.VcpuCount,
                    MemoryGb = instanceType.MemoryGb,
                    Category = instanceType.Category,
                    OnDemandHourly = pricing.OnDemandHourly,
                    OnDemandMonthly = pricing.OnDemandMonthly,
                    SpotHourly = pricing.SpotCurrentPrice,
                    Currency = pricing.Currency,
                    MatchScore = CalculateMatchScore(instanceType, pricing, request),
                    Features = GetFeatureList(instanceType)
                };

                recommendations.Add(recommendation);
            }
        }

        var result = new RecommendationsResponse
        {
            Request = request,
            Recommendations = recommendations
                .OrderByDescending(r => r.MatchScore)
                .ThenBy(r => r.OnDemandHourly)
                .Take(20)
                .ToList(),
            GeneratedAt = DateTime.UtcNow
        };

        return Ok(result);
    }

    #endregion

    #region Sync

    /// <summary>
    /// Запустить синхронизацию с провайдером (только для админов)
    /// </summary>
    [HttpPost("{providerCode}/sync")]
    [Authorize]
    //[Authorize(Roles = "Admin")]
    public async Task<ActionResult<SyncResult>> SyncProvider(
        string providerCode, 
        [FromQuery] bool force = false)
    {
        var provider = await _cacheService.GetProviderByCodeAsync(providerCode);
        if (provider == null)
        {
            return NotFound($"Provider '{providerCode}' not found");
        }

        var result = await _syncService.SyncProviderAsync(provider.Id, force);
        
        // Инвалидируем кэш после синхронизации
        await _cacheService.InvalidateProviderCacheAsync(provider.Id);
        
        return Ok(result);
    }

    /// <summary>
    /// Получить статус синхронизации
    /// </summary>
    [HttpGet("{providerCode}/sync/status")]
    public async Task<ActionResult<SyncStatusDto>> GetSyncStatus(string providerCode)
    {
        var provider = await _cacheService.GetProviderByCodeAsync(providerCode);
        if (provider == null)
        {
            return NotFound($"Provider '{providerCode}' not found");
        }

        var status = await _syncService.GetSyncStatusAsync(provider.Id);
        
        return Ok(new SyncStatusDto
        {
            ProviderCode = providerCode,
            LastSyncAt = status.LastSyncAt,
            LastSyncSuccess = status.LastSyncSuccess,
            LastSyncError = status.LastSyncError,
            IsRunning = status.IsRunning,
            NextSyncAt = status.NextSyncAt,
            LastStatistics = status.LastStatistics != null ? new SyncStatisticsDto
            {
                RegionsAdded = status.LastStatistics.RegionsAdded,
                RegionsUpdated = status.LastStatistics.RegionsUpdated,
                ServicesAdded = status.LastStatistics.ServicesAdded,
                ServicesUpdated = status.LastStatistics.ServicesUpdated,
                InstanceTypesAdded = status.LastStatistics.InstanceTypesAdded,
                InstanceTypesUpdated = status.LastStatistics.InstanceTypesUpdated,
                PricingsUpdated = status.LastStatistics.PricingsUpdated,
                TotalChanges = status.LastStatistics.TotalChanges
            } : null
        });
    }

    /// <summary>
    /// Запустить синхронизацию всех провайдеров (только для админов)
    /// </summary>
    [HttpPost("sync-all")]
    [Authorize]
    //[Authorize(Roles = "Admin")]
    public async Task<ActionResult<Dictionary<string, SyncResult>>> SyncAllProviders([FromQuery] bool force = false)
    {
        var results = await _syncService.SyncAllProvidersAsync(force);
        
        // Инвалидируем весь кэш
        await _cacheService.InvalidateAllCacheAsync();
        
        return Ok(results);
    }

    #endregion

    #region Cache

    /// <summary>
    /// Получить статистику кэша (только для админов)
    /// </summary>
    [HttpGet("cache/stats")]
    [Authorize]
    //[Authorize(Roles = "Admin")]
    public async Task<ActionResult<CacheStatistics>> GetCacheStats()
    {
        var stats = await _cacheService.GetCacheStatisticsAsync();
        return Ok(stats);
    }

    /// <summary>
    /// Инвалидировать кэш провайдера (только для админов)
    /// </summary>
    [HttpPost("{providerCode}/cache/invalidate")]
    [Authorize]
    //[Authorize(Roles = "Admin")]    
    public async Task<IActionResult> InvalidateCache(string providerCode)
    {
        var provider = await _cacheService.GetProviderByCodeAsync(providerCode);
        if (provider == null)
        {
            return NotFound($"Provider '{providerCode}' not found");
        }

        await _cacheService.InvalidateProviderCacheAsync(provider.Id);
        return Ok(new { message = $"Cache for {providerCode} invalidated" });
    }

    /// <summary>
    /// Инвалидировать весь кэш (только для админов)
    /// </summary>
    [HttpPost("cache/invalidate-all")]
    [Authorize]
    //[Authorize(Roles = "Admin")]    
    public async Task<IActionResult> InvalidateAllCache()
    {
        await _cacheService.InvalidateAllCacheAsync();
        return Ok(new { message = "All cache invalidated" });
    }

    #endregion

    #region Private Helper Methods

    private async Task<List<CloudService>> GetServicesAsync(Guid providerId)
    {
        // Используем кэш или прямой запрос
        var cacheKey = $"services:{providerId}";
        // Здесь можно добавить кэширование сервисов
        return new List<CloudService>(); // Заглушка
    }

    private async Task<InstanceTypeDetailDto> GetInstanceTypeDetail(
        InstanceType instanceType, 
        InstancePricing? pricing, 
        CloudProvider provider, 
        string? regionCode)
    {
        var region = instanceType.Region ?? await _cacheService.GetRegionByCodeAsync(provider.Id, regionCode ?? "");

        return new InstanceTypeDetailDto
        {
            Id = instanceType.Id,
            TypeCode = instanceType.TypeCode,
            DisplayName = instanceType.DisplayName,
            Category = instanceType.Category,
            Family = instanceType.Family,
            VcpuCount = instanceType.VcpuCount,
            MemoryGb = instanceType.MemoryGb,
            NetworkBandwidthGbps = instanceType.NetworkBandwidthGbps,
            HasGpu = instanceType.HasGpu,
            Region = region != null ? new RegionInfoDto
            {
                Code = region.Code,
                Name = region.DisplayName
            } : null,
            Pricing = pricing != null ? new PricingDetailDto
            {
                Currency = pricing.Currency,
                OnDemand = new OnDemandPricingDto
                {
                    Hourly = pricing.OnDemandHourly,
                    Monthly = pricing.OnDemandMonthly
                }
            } : null
        };
    }

    private double CalculateMatchScore(InstanceType instanceType, InstancePricing pricing, RecommendationsRequest request)
    {
        double score = 100;
        
        // CPU match
        var cpuDiff = Math.Abs(instanceType.VcpuCount - request.Cpu);
        score -= cpuDiff * 10;
        
        // Memory match
        var memoryDiff = Math.Abs(instanceType.MemoryGb - request.Memory) / request.Memory;
        score -= memoryDiff * 20;
        
        // Price score
        if (request.MaxBudget.HasValue)
        {
            var monthlyPrice = pricing.OnDemandMonthly;
            if (monthlyPrice > request.MaxBudget.Value)
            {
                score -= 30;
            }
            else
            {
                var priceRatio = (double)(monthlyPrice / request.MaxBudget.Value);
                score += (1 - priceRatio) * 20;
            }
        }
        
        // Category bonus
        if (!string.IsNullOrEmpty(request.PreferredCategory) && 
            instanceType.Category == request.PreferredCategory)
        {
            score += 15;
        }
        
        // Spot bonus if requested
        if (request.IncludeSpot && pricing.SpotSavingsPercent > 50)
        {
            score += 10;
        }
        
        return Math.Max(0, Math.Min(100, score));
    }

    private List<string> GetFeatureList(InstanceType instanceType)
    {
        var features = new List<string>();
        if (instanceType.EbsOptimized) features.Add("EBS Optimized");
        if (instanceType.EnhancedNetworking) features.Add("Enhanced Networking");
        if (instanceType.HasGpu) features.Add("GPU");
        if (instanceType.NetworkBandwidthGbps >= 10) features.Add("High Network");
        return features;
    }

    private string[] GetSupportedFeatures(string providerCode)
    {
        return providerCode switch
        {
            "aws" => new[] { "spot", "reserved", "savings-plan", "auto-scaling", "load-balancer" },
            "azure" => new[] { "spot", "reserved", "hybrid-benefit", "auto-scaling" },
            "gcp" => new[] { "preemptible", "committed-use", "sustained-use", "auto-scaling" },
            "yandex" => new[] { "preemptible", "committed", "auto-scaling" },
            "vk" => new[] { "basic" },
            _ => new[] { "basic" }
        };
    }

    #endregion
}