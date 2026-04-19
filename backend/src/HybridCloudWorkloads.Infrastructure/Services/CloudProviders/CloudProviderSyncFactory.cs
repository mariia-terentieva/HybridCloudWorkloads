using System;
using System.Collections.Generic;
using System.Linq;
using HybridCloudWorkloads.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HybridCloudWorkloads.Infrastructure.Services.CloudProviders;

/// <summary>
/// Фабрика для получения синхронизатора провайдера
/// </summary>
public class CloudProviderSyncFactory : ICloudProviderSyncFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CloudProviderSyncFactory> _logger;
    private readonly Dictionary<string, Type> _syncTypes;

    public CloudProviderSyncFactory(
        IServiceProvider serviceProvider,
        ILogger<CloudProviderSyncFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        _syncTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["aws"] = typeof(AwsProviderSync),
            ["azure"] = typeof(AzureProviderSync),
            ["gcp"] = typeof(GcpProviderSync),
            ["yandex"] = typeof(YandexProviderSync),
            ["vk"] = typeof(VkProviderSync)
        };
    }

    public ICloudProviderSync? GetSyncService(string providerCode)
    {
        if (_syncTypes.TryGetValue(providerCode, out var syncType))
        {
            return (ICloudProviderSync)_serviceProvider.GetRequiredService(syncType);
        }
        
        _logger.LogWarning("No sync service found for provider: {ProviderCode}", providerCode);
        return null;
    }

    public IEnumerable<string> GetSupportedProviders()
    {
        return _syncTypes.Keys.ToList();
    }

    public bool IsProviderSupported(string providerCode)
    {
        return _syncTypes.ContainsKey(providerCode);
    }
}