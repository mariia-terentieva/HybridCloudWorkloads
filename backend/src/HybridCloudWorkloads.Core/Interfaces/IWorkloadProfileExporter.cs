using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HybridCloudWorkloads.Core.Entities;

namespace HybridCloudWorkloads.Core.Interfaces;

/// <summary>
/// Интерфейс для экспорта профилей workloads в различные форматы
/// </summary>
public interface IWorkloadProfileExporter
{
    /// <summary>
    /// Экспорт профиля одного workload в JSON
    /// </summary>
    Task<string> ExportToJsonAsync(Guid workloadId);
    
    /// <summary>
    /// Экспорт профилей нескольких workloads в JSON
    /// </summary>
    Task<string> ExportBatchToJsonAsync(IEnumerable<Guid> workloadIds);
    
    /// <summary>
    /// Экспорт профиля в CSV
    /// </summary>
    Task<byte[]> ExportToCsvAsync(Guid workloadId);
    
    /// <summary>
    /// Экспорт профилей в CSV (для нескольких workloads)
    /// </summary>
    Task<byte[]> ExportBatchToCsvAsync(IEnumerable<Guid> workloadIds);
    
    /// <summary>
    /// Экспорт профиля в Excel
    /// </summary>
    Task<byte[]> ExportToExcelAsync(Guid workloadId);
    
    /// <summary>
    /// Экспорт профилей в Excel
    /// </summary>
    Task<byte[]> ExportBatchToExcelAsync(IEnumerable<Guid> workloadIds);
    
    /// <summary>
    /// Получить компактный профиль для модуля оптимизации
    /// </summary>
    Task<OptimizationReadyProfile> GetOptimizationReadyProfileAsync(Guid workloadId);
    
    /// <summary>
    /// Получить компактные профили для модуля оптимизации (batch)
    /// </summary>
    Task<List<OptimizationReadyProfile>> GetBatchOptimizationReadyProfilesAsync(IEnumerable<Guid> workloadIds);
    
    /// <summary>
    /// Экспорт в формате Prometheus (для интеграции с мониторингом)
    /// </summary>
    Task<string> ExportToPrometheusFormatAsync(Guid workloadId);
}

/// <summary>
/// Компактный профиль для модуля оптимизации
/// </summary>
public class OptimizationReadyProfile
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Требования к ресурсам
    public int RequiredCpu { get; set; }
    public double RequiredMemory { get; set; }
    public double RequiredStorage { get; set; }
    
    // Классификация
    public string UsagePattern { get; set; } = string.Empty;
    public string Criticality { get; set; } = string.Empty;
    public string BudgetTier { get; set; } = string.Empty;
    
    // SLA требования
    public int MaxResponseTimeMs { get; set; }
    public double AvailabilityTarget { get; set; }
    public bool RequiresRedundancy { get; set; }
    
    // Метрики производительности (усредненные за последние 7 дней)
    public double AvgCpuPercent { get; set; }
    public double AvgMemoryPercent { get; set; }
    public double PeakCpuPercent { get; set; }
    public double PeakMemoryPercent { get; set; }
    public double AvgResponseTimeMs { get; set; }
    public double P95ResponseTimeMs { get; set; }
    public double AvgRequestsPerSecond { get; set; }
    
    // Бизнес-часы
    public string? Timezone { get; set; }
    public double WeekendLoadPercent { get; set; }
    
    // Теги и метаданные
    public string[]? Tags { get; set; }
    public DateTime LastProfiledAt { get; set; }
    
    // Рекомендации от системы
    public string[]? Recommendations { get; set; }
    
    // Дополнительные данные для оптимизации
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Результат экспорта профиля
/// </summary>
public class ExportResult
{
    public bool Success { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public byte[]? Data { get; set; }
    public string? ErrorMessage { get; set; }
}