using System;
using System.Collections.Generic;

namespace HybridCloudWorkloads.Core.Entities;

/// <summary>
/// Сервис облачного провайдера
/// </summary>
public class CloudService
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// ID провайдера
    /// </summary>
    public Guid ProviderId { get; set; }
    
    /// <summary>
    /// Код сервиса (ec2, compute, app-engine)
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Название сервиса
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Тип сервиса (Compute, Storage, Database, Network, etc.)
    /// </summary>
    public string ServiceType { get; set; } = "Compute";
    
    /// <summary>
    /// Описание сервиса
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// URL документации
    /// </summary>
    public string? DocumentationUrl { get; set; }
    
    /// <summary>
    /// Модель ценообразования (JSON)
    /// </summary>
    public string? PricingModel { get; set; }
    
    /// <summary>
    /// Бесплатный тир (JSON)
    /// </summary>
    public string? FreeTier { get; set; }
    
    /// <summary>
    /// SLA сервиса (JSON)
    /// </summary>
    public string? SlaInfo { get; set; }
    
    /// <summary>
    /// Метаданные сервиса (JSON)
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Дата обновления
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Навигационные свойства
    public virtual CloudProvider Provider { get; set; } = null!;
    public virtual ICollection<InstanceType> InstanceTypes { get; set; } = new List<InstanceType>();
}

/// <summary>
/// Типы сервисов
/// </summary>
public static class ServiceTypes
{
    public const string Compute = "Compute";
    public const string Storage = "Storage";
    public const string Database = "Database";
    public const string Network = "Network";
    public const string Container = "Container";
    public const string Serverless = "Serverless";
    public const string Analytics = "Analytics";
    public const string AI_ML = "AI/ML";
    public const string Security = "Security";
    public const string Management = "Management";
}