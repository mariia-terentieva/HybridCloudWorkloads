using System.Collections.Generic;

namespace HybridCloudWorkloads.Core.Interfaces;

/// <summary>
/// Фабрика для получения синхронизатора провайдера
/// </summary>
public interface ICloudProviderSyncFactory
{
    /// <summary>
    /// Получить сервис синхронизации для указанного провайдера
    /// </summary>
    ICloudProviderSync? GetSyncService(string providerCode);
    
    /// <summary>
    /// Получить список поддерживаемых провайдеров
    /// </summary>
    IEnumerable<string> GetSupportedProviders();
    
    /// <summary>
    /// Проверить, поддерживается ли провайдер
    /// </summary>
    bool IsProviderSupported(string providerCode);
}