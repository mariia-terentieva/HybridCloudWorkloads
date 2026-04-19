using System;
using System.Collections.Generic;

namespace HybridCloudWorkloads.Core.Entities;


// Облачный провайдер

public class CloudProvider
{
    public Guid Id { get; set; }
    
    
    // Уникальный код провайдера (aws, azure, gcp, yandex, vk, on-premise)
    
    public string Code { get; set; } = string.Empty;
    
    
    // Отображаемое имя
    
    public string DisplayName { get; set; } = string.Empty;
    
    
    // Описание провайдера
    
    public string? Description { get; set; }
    
    
    // URL логотипа
    
    public string? LogoUrl { get; set; }
    
    
    // Базовый URL API провайдера
    
    public string? ApiEndpoint { get; set; }
    
    
    // Тип аутентификации: api-key, oauth, none
    
    public string AuthType { get; set; } = "api-key";
    
    
    // Конфигурация аутентификации (JSON)
    
    public string? AuthConfig { get; set; }
    
    
    // Статус провайдера
    
    public ProviderStatus Status { get; set; } = ProviderStatus.Active;
    
    
    // Метаданные провайдера (JSON)
    
    public string? Metadata { get; set; }
    
    
    // Включена ли синхронизация
    
    public bool SyncEnabled { get; set; } = true;
    
    
    // Интервал синхронизации в минутах
    
    public int SyncIntervalMinutes { get; set; } = 60;
    
    
    // Дата последней синхронизации
    
    public DateTime? LastSyncAt { get; set; }
    
    
    // Дата создания
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    
    // Дата обновления
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Навигационные свойства
    public virtual ICollection<CloudRegion> Regions { get; set; } = new List<CloudRegion>();
    public virtual ICollection<CloudService> Services { get; set; } = new List<CloudService>();
    public virtual ICollection<Discount> Discounts { get; set; } = new List<Discount>();
}


// Статус провайдера

public enum ProviderStatus
{
    Active = 0,
    Inactive = 1,
    Maintenance = 2,
    Error = 3
}
