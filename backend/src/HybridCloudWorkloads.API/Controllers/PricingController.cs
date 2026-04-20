using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HybridCloudWorkloads.Core.Entities; 
using HybridCloudWorkloads.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HybridCloudWorkloads.API.Controllers;

[Authorize]
[ApiController]
[Route("api/pricing")]
public class PricingController : ControllerBase
{
    private readonly IPricingCacheService _cacheService;
    private readonly ISyncService _syncService;
    private readonly ILogger<PricingController> _logger;

    public PricingController(
        IPricingCacheService cacheService,
        ISyncService syncService,
        ILogger<PricingController> logger)
    {
        _cacheService = cacheService;
        _syncService = syncService;
        _logger = logger;
    }

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
    /// Получить типы инстансов с фильтрацией
    /// </summary>
    [HttpGet("instance-types")]
    public async Task<ActionResult<List<InstanceType>>> GetInstanceTypes(
        [FromQuery] string providerCode,
        [FromQuery] string? regionCode = null,
        [FromQuery] int? minCpu = null,
        [FromQuery] int? maxCpu = null,
        [FromQuery] double? minMemory = null,
        [FromQuery] double? maxMemory = null,
        [FromQuery] string? category = null,
        [FromQuery] bool forceRefresh = false)
    {
        var provider = await _cacheService.GetProviderByCodeAsync(providerCode);
        if (provider == null)
        {
            return NotFound($"Provider {providerCode} not found");
        }

        var instanceTypes = await _cacheService.GetInstanceTypesFilteredAsync(
            provider.Id, regionCode, minCpu, maxCpu, minMemory, maxMemory, category, forceRefresh);
        
        return Ok(instanceTypes);
    }

    /// <summary>
    /// Получить цены для типа инстанса
    /// </summary>
    [HttpGet("instance-types/{instanceTypeId}/pricing")]
    public async Task<ActionResult<InstancePricing>> GetPricing(
        Guid instanceTypeId,
        [FromQuery] bool forceRefresh = false)
    {
        var pricing = await _cacheService.GetPricingAsync(instanceTypeId, forceRefresh);
        if (pricing == null)
        {
            return NotFound();
        }
        return Ok(pricing);
    }

    /// <summary>
    /// Получить цены для нескольких типов инстансов
    /// </summary>
    [HttpPost("batch-pricing")]
    public async Task<ActionResult<Dictionary<Guid, InstancePricing>>> GetBatchPricing(
        [FromBody] Guid[] instanceTypeIds,
        [FromQuery] bool forceRefresh = false)
    {
        var pricings = await _cacheService.GetBatchPricingAsync(instanceTypeIds, forceRefresh);
        return Ok(pricings);
    }

    /// <summary>
    /// Получить статистику кэша (только для админов)
    /// </summary>
    [HttpGet("cache/stats")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CacheStatistics>> GetCacheStats()
    {
        var stats = await _cacheService.GetCacheStatisticsAsync();
        return Ok(stats);
    }

    /// <summary>
    /// Инвалидировать кэш провайдера (только для админов)
    /// </summary>
    [HttpPost("cache/invalidate/{providerId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> InvalidateCache(Guid providerId)
    {
        await _cacheService.InvalidateProviderCacheAsync(providerId);
        return Ok(new { message = "Cache invalidated" });
    }

    /// <summary>
    /// Инвалидировать весь кэш (только для админов)
    /// </summary>
    [HttpPost("cache/invalidate-all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> InvalidateAllCache()
    {
        await _cacheService.InvalidateAllCacheAsync();
        return Ok(new { message = "All cache invalidated" });
    }

    /// <summary>
    /// Запустить синхронизацию с провайдером
    /// </summary>
    [HttpPost("sync/{providerCode}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SyncProvider(string providerCode, [FromQuery] bool force = false)
    {
        var provider = await _cacheService.GetProviderByCodeAsync(providerCode);
        if (provider == null)
        {
            return NotFound($"Provider {providerCode} not found");
        }

        var result = await _syncService.SyncProviderAsync(provider.Id, force);
        
        // Инвалидируем кэш после синхронизации
        await _cacheService.InvalidateProviderCacheAsync(provider.Id);
        
        return Ok(result);
    }

    /// <summary>
    /// Получить статус синхронизации
    /// </summary>
    [HttpGet("sync/status/{providerCode}")]
    public async Task<ActionResult<SyncStatus>> GetSyncStatus(string providerCode)
    {
        var provider = await _cacheService.GetProviderByCodeAsync(providerCode);
        if (provider == null)
        {
            return NotFound($"Provider {providerCode} not found");
        }

        var status = await _syncService.GetSyncStatusAsync(provider.Id);
        return Ok(status);
    }
}