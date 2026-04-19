namespace HybridCloudWorkloads.Core.Entities;

/// <summary>
/// Модель ценообразования (класс для сериализации JSON)
/// </summary>
public class PricingModelInfo
{
    /// <summary>
    /// Тип модели (OnDemand, Spot, Reserved, SavingsPlan)
    /// </summary>
    public string ModelType { get; set; } = "OnDemand";
    
    /// <summary>
    /// Доступные опции оплаты
    /// </summary>
    public string[]? PaymentOptions { get; set; }
    
    /// <summary>
    /// Минимальный срок (в месяцах)
    /// </summary>
    public int? MinimumTermMonths { get; set; }
    
    /// <summary>
    /// Возможность отмены
    /// </summary>
    public bool Cancellable { get; set; } = true;
    
    /// <summary>
    /// Штраф за досрочное расторжение
    /// </summary>
    public decimal? EarlyTerminationFee { get; set; }
    
    /// <summary>
    /// Автоматическое продление
    /// </summary>
    public bool AutoRenew { get; set; } = true;
    
    /// <summary>
    /// Дополнительные параметры
    /// </summary>
    public Dictionary<string, object>? AdditionalParams { get; set; }
}

/// <summary>
/// Информация о бесплатном тире
/// </summary>
public class FreeTierInfo
{
    /// <summary>
    /// Доступен ли бесплатный тир
    /// </summary>
    public bool Available { get; set; }
    
    /// <summary>
    /// Тип бесплатного тира (AlwaysFree, Trial, OneTime)
    /// </summary>
    public string TierType { get; set; } = "Trial";
    
    /// <summary>
    /// Длительность пробного периода (в месяцах)
    /// </summary>
    public int? TrialDurationMonths { get; set; }
    
    /// <summary>
    /// Лимиты бесплатного тира
    /// </summary>
    public Dictionary<string, object>? Limits { get; set; }
    
    /// <summary>
    /// Услуги, включенные в бесплатный тир
    /// </summary>
    public string[]? IncludedServices { get; set; }
}

/// <summary>
/// Информация о SLA
/// </summary>
public class SlaInfo
{
    /// <summary>
    /// Уровень доступности (%)
    /// </summary>
    public double Availability { get; set; } = 99.9;
    
    /// <summary>
    /// Период измерения SLA
    /// </summary>
    public string MeasurementPeriod { get; set; } = "Monthly";
    
    /// <summary>
    /// Кредит при нарушении SLA (%)
    /// </summary>
    public double? ServiceCreditPercent { get; set; }
    
    /// <summary>
    /// Исключения из SLA
    /// </summary>
    public string[]? Exclusions { get; set; }
    
    /// <summary>
    /// URL с деталями SLA
    /// </summary>
    public string? DetailsUrl { get; set; }
}