using System;

namespace HybridCloudWorkloads.Core.Entities;

/// <summary>
/// Скидки и специальные предложения провайдера
/// </summary>
public class Discount
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// ID провайдера
    /// </summary>
    public Guid ProviderId { get; set; }
    
    /// <summary>
    /// Название скидки
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Описание скидки
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Тип скидки (VolumeDiscount, CommitmentDiscount, Promotional, EnterpriseAgreement)
    /// </summary>
    public string DiscountType { get; set; } = "Promotional";
    
    /// <summary>
    /// Условия скидки (JSON)
    /// </summary>
    public string? Conditions { get; set; }
    
    /// <summary>
    /// Процент скидки
    /// </summary>
    public decimal DiscountPercent { get; set; }
    
    /// <summary>
    /// Применяется к (типам сервисов)
    /// </summary>
    public string? AppliesTo { get; set; }
    
    /// <summary>
    /// Минимальная сумма для применения
    /// </summary>
    public decimal? MinimumSpend { get; set; }
    
    /// <summary>
    /// Максимальная сумма скидки
    /// </summary>
    public decimal? MaximumDiscount { get; set; }
    
    /// <summary>
    /// Промокод
    /// </summary>
    public string? PromoCode { get; set; }
    
    /// <summary>
    /// Срок действия: дата начала
    /// </summary>
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Срок действия: дата окончания
    /// </summary>
    public DateTime? ValidUntil { get; set; }
    
    /// <summary>
    /// Статус скидки
    /// </summary>
    public DiscountStatus Status { get; set; } = DiscountStatus.Active;
    
    /// <summary>
    /// Приоритет применения (при нескольких скидках)
    /// </summary>
    public int Priority { get; set; } = 0;
    
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
}

/// <summary>
/// Статус скидки
/// </summary>
public enum DiscountStatus
{
    Active = 0,
    Expired = 1,
    Suspended = 2,
    Pending = 3
}

/// <summary>
/// Типы скидок
/// </summary>
public static class DiscountTypes
{
    public const string VolumeDiscount = "VolumeDiscount";
    public const string CommitmentDiscount = "CommitmentDiscount";
    public const string Promotional = "Promotional";
    public const string EnterpriseAgreement = "EnterpriseAgreement";
    public const string StartupProgram = "StartupProgram";
    public const string Educational = "Educational";
    public const string NonProfit = "NonProfit";
}