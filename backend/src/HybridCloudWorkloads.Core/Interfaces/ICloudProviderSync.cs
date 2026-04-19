using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HybridCloudWorkloads.Core.Entities;

namespace HybridCloudWorkloads.Core.Interfaces;

/// <summary>
/// Интерфейс для синхронизации с облачным провайдером
/// </summary>
public interface ICloudProviderSync
{
    /// <summary>
    /// Уникальный код провайдера (aws, azure, gcp, yandex, vk)
    /// </summary>
    string ProviderCode { get; }
    
    /// <summary>
    /// Название провайдера для отображения
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// Проверить доступность API провайдера
    /// </summary>
    Task<bool> IsApiAvailableAsync();
    
    /// <summary>
    /// Проверить валидность учетных данных
    /// </summary>
    Task<bool> ValidateCredentialsAsync(string credentials);
    
    /// <summary>
    /// Синхронизировать список регионов
    /// </summary>
    Task<List<CloudRegion>> SyncRegionsAsync(Guid providerId);
    
    /// <summary>
    /// Синхронизировать список сервисов
    /// </summary>
    Task<List<CloudService>> SyncServicesAsync(Guid providerId);
    
    /// <summary>
    /// Синхронизировать типы инстансов для указанного региона
    /// </summary>
    Task<List<InstanceType>> SyncInstanceTypesAsync(Guid providerId, Guid regionId, string regionCode);
    
    /// <summary>
    /// Синхронизировать цены для указанного типа инстанса
    /// </summary>
    Task<InstancePricing> SyncPricingAsync(Guid instanceTypeId, string instanceTypeCode, string regionCode);
    
    /// <summary>
    /// Получить историю спотовых цен за период
    /// </summary>
    Task<List<SpotPriceHistory>> SyncSpotPriceHistoryAsync(
        string instanceTypeCode, 
        string regionCode, 
        DateTime startDate, 
        DateTime endDate);
    
    /// <summary>
    /// Получить текущие скидки и специальные предложения
    /// </summary>
    Task<List<Discount>> SyncDiscountsAsync(Guid providerId);
}

/// <summary>
/// Запись истории спотовых цен
/// </summary>
public class SpotPriceHistory
{
    public DateTime Timestamp { get; set; }
    public decimal Price { get; set; }
    public string AvailabilityZone { get; set; } = string.Empty;
    public string InstanceType { get; set; } = string.Empty;
}