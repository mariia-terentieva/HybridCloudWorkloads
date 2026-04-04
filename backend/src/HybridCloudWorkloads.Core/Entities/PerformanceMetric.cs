using System;

namespace HybridCloudWorkloads.Core.Entities;

/// Метрика производительности для workload
public class PerformanceMetric
{
    public Guid Id { get; set; }
    
    /// ID рабочей нагрузки
    public Guid WorkloadId { get; set; }
    
    /// Временная метка сбора метрики
    public DateTime Timestamp { get; set; }
    
    /// Использование CPU (%)
    public double CpuUsagePercent { get; set; }
    
    /// Использование памяти (%)
    public double MemoryUsagePercent { get; set; }
    
    /// Использование памяти (абсолютное, в МБ)
    public double MemoryUsageMB { get; set; }
    
    /// Входящий сетевой трафик (байт/сек)
    public long NetworkInBytesPerSec { get; set; }
    
    /// Исходящий сетевой трафик (байт/сек)
    public long NetworkOutBytesPerSec { get; set; }
    
    /// Операции чтения с диска (в секунду)
    public int DiskReadOpsPerSec { get; set; }
    
    /// Операции записи на диск (в секунду)
    public int DiskWriteOpsPerSec { get; set; }
    
    /// Время отклика (мс)
    public double ResponseTimeMs { get; set; }
    
    /// Количество запросов в секунду
    public double RequestsPerSecond { get; set; }
    
    /// Количество ошибок
    public int ErrorCount { get; set; }
    
    /// Статус контейнера (Running, Stopped, Error)
    public string? ContainerStatus { get; set; }
    
    /// Дополнительные метрики в формате JSON
    public string? AdditionalMetrics { get; set; }
    
    // Навигационное свойство
    public virtual Workload Workload { get; set; } = null!;
}

/// Агрегированные метрики за период
public class AggregatedMetrics
{
    public Guid WorkloadId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string PeriodType { get; set; } = "Hour"; // Hour, Day, Week, Month
    
    // Средние значения
    public double AvgCpuUsagePercent { get; set; }
    public double AvgMemoryUsagePercent { get; set; }
    public double AvgMemoryUsageMB { get; set; }
    public double AvgResponseTimeMs { get; set; }
    public double AvgRequestsPerSecond { get; set; }
    
    // Пиковые значения
    public double PeakCpuUsagePercent { get; set; }
    public double PeakMemoryUsagePercent { get; set; }
    public double PeakMemoryUsageMB { get; set; }
    public double PeakResponseTimeMs { get; set; }
    public double PeakRequestsPerSecond { get; set; }
    
    // Минимальные значения
    public double MinCpuUsagePercent { get; set; }
    public double MinMemoryUsagePercent { get; set; }
    
    // Процентили
    public double P95CpuUsagePercent { get; set; }
    public double P95MemoryUsagePercent { get; set; }
    public double P95ResponseTimeMs { get; set; }
    
    public double P99ResponseTimeMs { get; set; }
    
    // Суммарные значения
    public long TotalNetworkInBytes { get; set; }
    public long TotalNetworkOutBytes { get; set; }
    public long TotalDiskReadOps { get; set; }
    public long TotalDiskWriteOps { get; set; }
    public int TotalErrorCount { get; set; }
    
    // Количество семплов
    public int SampleCount { get; set; }
    
    // Время аптайма (секунд)
    public double UptimeSeconds { get; set; }
    
    // Доступность (%)
    public double AvailabilityPercent { get; set; }
}

/// DTO для передачи агрегированных метрик
public class AggregatedMetricsDto
{
    public Guid WorkloadId { get; set; }
    public string WorkloadName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string PeriodType { get; set; } = string.Empty;
    
    public double AvgCpu { get; set; }
    public double AvgMemory { get; set; }
    public double PeakCpu { get; set; }
    public double PeakMemory { get; set; }
    public double P95Cpu { get; set; }
    public double P95Memory { get; set; }
    
    public double AvgResponseTime { get; set; }
    public double P95ResponseTime { get; set; }
    public double P99ResponseTime { get; set; }
    
    public double AvgRps { get; set; }
    public double PeakRps { get; set; }
    
    public long TotalNetworkIn { get; set; }
    public long TotalNetworkOut { get; set; }
    
    public double Availability { get; set; }
    public int ErrorCount { get; set; }
    public int SampleCount { get; set; }
    
    public ChartDataPoint[] TimeSeriesData { get; set; } = Array.Empty<ChartDataPoint>();
}

/// Точка данных для графиков
public class ChartDataPoint
{
    public DateTime Timestamp { get; set; }
    public double Cpu { get; set; }
    public double Memory { get; set; }
    public double ResponseTime { get; set; }
    public double Requests { get; set; }
}

/// Профиль производительности workload (хранится в Workload.BaselinePerformance)
public class PerformanceProfile
{
    public DateTime CalculatedAt { get; set; }
    public int DaysOfData { get; set; }
    public int TotalSamples { get; set; }
    
    // Базовые метрики
    public double BaselineCpu { get; set; }
    public double BaselineMemory { get; set; }
    public double BaselineResponseTime { get; set; }
    
    // Паттерны
    public bool HasDailyPattern { get; set; }
    public bool HasWeeklyPattern { get; set; }
    public List<PeakPeriod> PeakPeriods { get; set; } = new();
    
    // Рекомендации
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

public class PeakPeriod
{
    public string Pattern { get; set; } = string.Empty; // "daily", "weekly", "monthly"
    public string TimeRange { get; set; } = string.Empty;
    public double AvgLoad { get; set; }
    public double PeakLoad { get; set; }
}