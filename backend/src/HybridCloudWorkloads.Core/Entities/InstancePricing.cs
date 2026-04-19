using System;

namespace HybridCloudWorkloads.Core.Entities;

/// <summary>
/// Ценообразование для типа инстанса
/// </summary>
public class InstancePricing
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// ID типа инстанса
    /// </summary>
    public Guid InstanceTypeId { get; set; }
    
    /// <summary>
    /// Валюта (USD, EUR, RUB)
    /// </summary>
    public string Currency { get; set; } = "USD";
    
    /// <summary>
    /// Цена On-Demand за час
    /// </summary>
    public decimal OnDemandHourly { get; set; }
    
    /// <summary>
    /// Цена On-Demand за месяц (приблизительно)
    /// </summary>
    public decimal OnDemandMonthly { get; set; }
    
    /// <summary>
    /// Цена Spot текущая
    /// </summary>
    public decimal? SpotCurrentPrice { get; set; }
    
    /// <summary>
    /// Средняя цена Spot за 30 дней
    /// </summary>
    public decimal? SpotAveragePrice { get; set; }
    
    /// <summary>
    /// Минимальная цена Spot за 30 дней
    /// </summary>
    public decimal? SpotMinPrice { get; set; }
    
    /// <summary>
    /// Максимальная цена Spot за 30 дней
    /// </summary>
    public decimal? SpotMaxPrice { get; set; }
    
    /// <summary>
    /// Процент экономии Spot относительно On-Demand
    /// </summary>
    public decimal? SpotSavingsPercent { get; set; }
    
    /// <summary>
    /// Частота прерывания Spot (interruption rate)
    /// </summary>
    public double? SpotInterruptionRate { get; set; }
    
    /// <summary>
    /// Цена Reserved 1 год (без предоплаты)
    /// </summary>
    public decimal? Reserved1YearNoUpfront { get; set; }
    
    /// <summary>
    /// Цена Reserved 1 год (частичная предоплата)
    /// </summary>
    public decimal? Reserved1YearPartialUpfront { get; set; }
    
    /// <summary>
    /// Цена Reserved 1 год (полная предоплата)
    /// </summary>
    public decimal? Reserved1YearAllUpfront { get; set; }
    
    /// <summary>
    /// Цена Reserved 3 года (без предоплаты)
    /// </summary>
    public decimal? Reserved3YearNoUpfront { get; set; }
    
    /// <summary>
    /// Цена Reserved 3 года (частичная предоплата)
    /// </summary>
    public decimal? Reserved3YearPartialUpfront { get; set; }
    
    /// <summary>
    /// Цена Reserved 3 года (полная предоплата)
    /// </summary>
    public decimal? Reserved3YearAllUpfront { get; set; }
    
    /// <summary>
    /// Экономия Reserved 1 год (%)
    /// </summary>
    public decimal? Reserved1YearSavingsPercent { get; set; }
    
    /// <summary>
    /// Экономия Reserved 3 года (%)
    /// </summary>
    public decimal? Reserved3YearSavingsPercent { get; set; }
    
    /// <summary>
    /// Стоимость хранения (EBS) за GB/месяц
    /// </summary>
    public decimal? StorageGbMonthly { get; set; }
    
    /// <summary>
    /// Стоимость исходящего трафика за GB
    /// </summary>
    public decimal? DataTransferOutGb { get; set; }
    
    /// <summary>
    /// Стоимость входящего трафика за GB
    /// </summary>
    public decimal? DataTransferInGb { get; set; }
    
    /// <summary>
    /// Стоимость межрегионального трафика за GB
    /// </summary>
    public decimal? DataTransferInterRegionGb { get; set; }
    
    /// <summary>
    /// Стоимость статического IP
    /// </summary>
    public decimal? StaticIpMonthly { get; set; }
    
    /// <summary>
    /// Стоимость балансировщика нагрузки
    /// </summary>
    public decimal? LoadBalancerHourly { get; set; }
    
    /// <summary>
    /// Дата начала действия цены
    /// </summary>
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Дата окончания действия цены (null = действует)
    /// </summary>
    public DateTime? ExpirationDate { get; set; }
    
    /// <summary>
    /// Метаданные ценообразования (JSON)
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Навигационные свойства
    public virtual InstanceType InstanceType { get; set; } = null!;
}