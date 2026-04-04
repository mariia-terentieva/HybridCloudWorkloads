using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HybridCloudWorkloads.Core.Entities;

namespace HybridCloudWorkloads.Core.Interfaces;

public interface IPerformanceMetricsRepository
{
    /// Добавить метрику
    Task AddMetricAsync(PerformanceMetric metric);
    
    /// Добавить несколько метрик (batch insert)
    Task AddMetricsBatchAsync(IEnumerable<PerformanceMetric> metrics);
    
    /// Получить метрики за период
    Task<List<PerformanceMetric>> GetMetricsAsync(
        Guid workloadId, 
        DateTime from, 
        DateTime to, 
        int? limit = null);
    
    /// Получить агрегированные метрики за период
    Task<AggregatedMetrics> GetAggregatedMetricsAsync(
        Guid workloadId,
        DateTime from,
        DateTime to,
        string periodType = "Day");
    
    /// Получить агрегированные метрики для нескольких workloads
    Task<Dictionary<Guid, AggregatedMetrics>> GetBatchAggregatedMetricsAsync(
        IEnumerable<Guid> workloadIds,
        DateTime from,
        DateTime to);
    
    /// Получить последние N метрик
    Task<List<PerformanceMetric>> GetLatestMetricsAsync(
        Guid workloadId, 
        int count = 100);
    
    /// Удалить метрики старше указанной даты
    Task DeleteOldMetricsAsync(DateTime olderThan);
    
    /// Получить временной ряд для графиков
    Task<ChartDataPoint[]> GetTimeSeriesAsync(
        Guid workloadId,
        DateTime from,
        DateTime to,
        string interval = "1h"); // 1m, 5m, 1h, 1d
    
    /// Рассчитать профиль производительности
    Task<PerformanceProfile> CalculatePerformanceProfileAsync(Guid workloadId);
}