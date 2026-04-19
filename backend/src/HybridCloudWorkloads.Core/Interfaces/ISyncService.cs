using System;
using System.Threading.Tasks;

namespace HybridCloudWorkloads.Core.Interfaces;

/// <summary>
/// Сервис управления синхронизацией с провайдерами
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Запустить полную синхронизацию для провайдера
    /// </summary>
    Task<SyncResult> SyncProviderAsync(Guid providerId, bool force = false);
    
    /// <summary>
    /// Запустить синхронизацию всех провайдеров
    /// </summary>
    Task<Dictionary<string, SyncResult>> SyncAllProvidersAsync(bool force = false);
    
    /// <summary>
    /// Получить статус последней синхронизации
    /// </summary>
    Task<SyncStatus> GetSyncStatusAsync(Guid providerId);
    
    /// <summary>
    /// Проверить, требуется ли синхронизация
    /// </summary>
    Task<bool> NeedsSyncAsync(Guid providerId);
    
    /// <summary>
    /// Получить время последней успешной синхронизации
    /// </summary>
    Task<DateTime?> GetLastSyncTimeAsync(Guid providerId);
}

/// <summary>
/// Результат синхронизации
/// </summary>
public class SyncResult
{
    public Guid ProviderId { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public bool Success { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    
    public SyncStatistics Statistics { get; set; } = new();
}

/// <summary>
/// Статистика синхронизации
/// </summary>
public class SyncStatistics
{
    public int RegionsAdded { get; set; }
    public int RegionsUpdated { get; set; }
    public int ServicesAdded { get; set; }
    public int ServicesUpdated { get; set; }
    public int InstanceTypesAdded { get; set; }
    public int InstanceTypesUpdated { get; set; }
    public int PricingsUpdated { get; set; }
    public int DiscountsAdded { get; set; }
    public int DiscountsUpdated { get; set; }
    
    public int TotalChanges => RegionsAdded + RegionsUpdated + 
                               ServicesAdded + ServicesUpdated +
                               InstanceTypesAdded + InstanceTypesUpdated +
                               PricingsUpdated +
                               DiscountsAdded + DiscountsUpdated;
}

/// <summary>
/// Статус синхронизации
/// </summary>
public class SyncStatus
{
    public Guid ProviderId { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public DateTime? LastSyncAt { get; set; }
    public bool LastSyncSuccess { get; set; }
    public string? LastSyncError { get; set; }
    public bool IsRunning { get; set; }
    public DateTime? NextSyncAt { get; set; }
    public SyncStatistics? LastStatistics { get; set; }
}