using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HybridCloudWorkloads.Core.Entities;
using HybridCloudWorkloads.Core.Interfaces;
using HybridCloudWorkloads.Infrastructure.Data;
using System.Text.Json;

namespace HybridCloudWorkloads.Infrastructure.Repositories;

public class PerformanceMetricsRepository : IPerformanceMetricsRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PerformanceMetricsRepository> _logger;

    public PerformanceMetricsRepository(
        ApplicationDbContext context,
        ILogger<PerformanceMetricsRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

public async Task AddMetricAsync(PerformanceMetric metric)
{
    metric.Id = Guid.NewGuid();
    metric.Timestamp = DateTime.UtcNow;
    
    // Санитизация всех числовых значений
    metric.CpuUsagePercent = SanitizeDouble(metric.CpuUsagePercent);
    metric.MemoryUsagePercent = SanitizeDouble(metric.MemoryUsagePercent);
    metric.MemoryUsageMB = SanitizeDouble(metric.MemoryUsageMB);
    metric.ResponseTimeMs = SanitizeDouble(metric.ResponseTimeMs);
    metric.RequestsPerSecond = SanitizeDouble(metric.RequestsPerSecond);
    metric.NetworkInBytesPerSec = SanitizeLong(metric.NetworkInBytesPerSec);
    metric.NetworkOutBytesPerSec = SanitizeLong(metric.NetworkOutBytesPerSec);
    metric.DiskReadOpsPerSec = SanitizeInt(metric.DiskReadOpsPerSec);
    metric.DiskWriteOpsPerSec = SanitizeInt(metric.DiskWriteOpsPerSec);
    metric.ErrorCount = SanitizeInt(metric.ErrorCount);
    
    await _context.PerformanceMetrics.AddAsync(metric);
    await _context.SaveChangesAsync();
}

private double SanitizeDouble(double value)
{
    if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        return 0;
    if (value > 100 && (value < 1000)) // CPU/память не должны быть >100%
        return Math.Min(value, 100);
    return Math.Round(value, 2);
}

private long SanitizeLong(long value)
{
    return value < 0 ? 0 : value;
}

private int SanitizeInt(int value)
{
    return value < 0 ? 0 : value;
}

    public async Task AddMetricsBatchAsync(IEnumerable<PerformanceMetric> metrics)
    {
        var metricList = metrics.ToList();
        foreach (var metric in metricList)
        {
            metric.Id = Guid.NewGuid();
            metric.Timestamp = DateTime.UtcNow;
        }
        
        await _context.PerformanceMetrics.AddRangeAsync(metricList);
        await _context.SaveChangesAsync();
    }

    public async Task<List<PerformanceMetric>> GetMetricsAsync(
        Guid workloadId, 
        DateTime from, 
        DateTime to, 
        int? limit = null)
    {
        var query = _context.PerformanceMetrics
            .Where(m => m.WorkloadId == workloadId)
            .Where(m => m.Timestamp >= from && m.Timestamp <= to)
            .OrderByDescending(m => m.Timestamp);
        
        if (limit.HasValue)
        {
            query = (IOrderedQueryable<PerformanceMetric>)query.Take(limit.Value);
        }
        
        return await query.ToListAsync();
    }

public async Task<AggregatedMetrics> GetAggregatedMetricsAsync(
    Guid workloadId,
    DateTime from,
    DateTime to,
    string periodType = "Day")
{
    var metrics = await _context.PerformanceMetrics
        .Where(m => m.WorkloadId == workloadId)
        .Where(m => m.Timestamp >= from && m.Timestamp <= to)
        .ToListAsync();

    if (!metrics.Any())
    {
        return new AggregatedMetrics
        {
            WorkloadId = workloadId,
            PeriodStart = from,
            PeriodEnd = to,
            PeriodType = periodType,
            SampleCount = 0
        };
    }

    // Фильтруем валидные значения для CPU и памяти (0-100%)
    var validCpuMetrics = metrics.Where(m => m.CpuUsagePercent >= 0 && m.CpuUsagePercent <= 100).ToList();
    var validMemoryMetrics = metrics.Where(m => m.MemoryUsagePercent >= 0 && m.MemoryUsagePercent <= 100).ToList();
    
    var aggregated = new AggregatedMetrics
    {
        WorkloadId = workloadId,
        PeriodStart = from,
        PeriodEnd = to,
        PeriodType = periodType,
        SampleCount = metrics.Count,
        
        // Используем валидные данные или 0 по умолчанию
        AvgCpuUsagePercent = validCpuMetrics.Any() ? validCpuMetrics.Average(m => m.CpuUsagePercent) : 0,
        AvgMemoryUsagePercent = validMemoryMetrics.Any() ? validMemoryMetrics.Average(m => m.MemoryUsagePercent) : 0,
        AvgMemoryUsageMB = metrics.Average(m => m.MemoryUsageMB),
        AvgResponseTimeMs = metrics.Average(m => m.ResponseTimeMs),
        AvgRequestsPerSecond = metrics.Average(m => m.RequestsPerSecond),
        
        PeakCpuUsagePercent = validCpuMetrics.Any() ? validCpuMetrics.Max(m => m.CpuUsagePercent) : 0,
        PeakMemoryUsagePercent = validMemoryMetrics.Any() ? validMemoryMetrics.Max(m => m.MemoryUsagePercent) : 0,
        PeakMemoryUsageMB = metrics.Max(m => m.MemoryUsageMB),
        PeakResponseTimeMs = metrics.Max(m => m.ResponseTimeMs),
        PeakRequestsPerSecond = metrics.Max(m => m.RequestsPerSecond),
        
        MinCpuUsagePercent = validCpuMetrics.Any() ? validCpuMetrics.Min(m => m.CpuUsagePercent) : 0,
        MinMemoryUsagePercent = validMemoryMetrics.Any() ? validMemoryMetrics.Min(m => m.MemoryUsagePercent) : 0,
        
        P95CpuUsagePercent = CalculatePercentile(validCpuMetrics.Select(m => m.CpuUsagePercent).ToList(), 95),
        P95MemoryUsagePercent = CalculatePercentile(validMemoryMetrics.Select(m => m.MemoryUsagePercent).ToList(), 95),
        P95ResponseTimeMs = CalculatePercentile(metrics.Select(m => m.ResponseTimeMs).ToList(), 95),
        P99ResponseTimeMs = CalculatePercentile(metrics.Select(m => m.ResponseTimeMs).ToList(), 99),
        
        TotalNetworkInBytes = metrics.Sum(m => m.NetworkInBytesPerSec),
        TotalNetworkOutBytes = metrics.Sum(m => m.NetworkOutBytesPerSec),
        TotalDiskReadOps = metrics.Sum(m => m.DiskReadOpsPerSec),
        TotalDiskWriteOps = metrics.Sum(m => m.DiskWriteOpsPerSec),
        TotalErrorCount = metrics.Sum(m => m.ErrorCount),
        
        UptimeSeconds = metrics.Count(m => m.ContainerStatus == "Running") * 60,
        AvailabilityPercent = metrics.Any() 
            ? (double)metrics.Count(m => m.ContainerStatus == "Running") / metrics.Count * 100 
            : 100
    };

    return aggregated;
}

    public async Task<Dictionary<Guid, AggregatedMetrics>> GetBatchAggregatedMetricsAsync(
        IEnumerable<Guid> workloadIds,
        DateTime from,
        DateTime to)
    {
        var result = new Dictionary<Guid, AggregatedMetrics>();
        
        foreach (var workloadId in workloadIds)
        {
            var metrics = await GetAggregatedMetricsAsync(workloadId, from, to);
            result[workloadId] = metrics;
        }
        
        return result;
    }

    public async Task<List<PerformanceMetric>> GetLatestMetricsAsync(
        Guid workloadId, 
        int count = 100)
    {
        return await _context.PerformanceMetrics
            .Where(m => m.WorkloadId == workloadId)
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task DeleteOldMetricsAsync(DateTime olderThan)
    {
        var oldMetrics = await _context.PerformanceMetrics
            .Where(m => m.Timestamp < olderThan)
            .ToListAsync();
        
        if (oldMetrics.Any())
        {
            _context.PerformanceMetrics.RemoveRange(oldMetrics);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted {Count} old metrics", oldMetrics.Count);
        }
    }

    public async Task<ChartDataPoint[]> GetTimeSeriesAsync(
        Guid workloadId,
        DateTime from,
        DateTime to,
        string interval = "1h")
    {
        var metrics = await _context.PerformanceMetrics
            .Where(m => m.WorkloadId == workloadId)
            .Where(m => m.Timestamp >= from && m.Timestamp <= to)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        if (!metrics.Any())
            return Array.Empty<ChartDataPoint>();

        // Группируем по интервалам
        var intervalMinutes = interval switch
        {
            "1m" => 1,
            "5m" => 5,
            "15m" => 15,
            "30m" => 30,
            "1h" => 60,
            "6h" => 360,
            "12h" => 720,
            "1d" => 1440,
            _ => 60
        };

        var grouped = metrics
            .GroupBy(m => new DateTime(
                m.Timestamp.Year,
                m.Timestamp.Month,
                m.Timestamp.Day,
                m.Timestamp.Hour,
                m.Timestamp.Minute / intervalMinutes * intervalMinutes,
                0))
            .Select(g => new ChartDataPoint
            {
                Timestamp = g.Key,
                Cpu = Math.Round(g.Average(m => m.CpuUsagePercent), 2),
                Memory = Math.Round(g.Average(m => m.MemoryUsagePercent), 2),
                ResponseTime = Math.Round(g.Average(m => m.ResponseTimeMs), 2),
                Requests = Math.Round(g.Average(m => m.RequestsPerSecond), 2)
            })
            .ToArray();

        return grouped;
    }

    public async Task<PerformanceProfile> CalculatePerformanceProfileAsync(Guid workloadId)
    {
        var now = DateTime.UtcNow;
        var monthAgo = now.AddDays(-30);
        
        var metrics = await _context.PerformanceMetrics
            .Where(m => m.WorkloadId == workloadId)
            .Where(m => m.Timestamp >= monthAgo)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        if (!metrics.Any())
        {
            return new PerformanceProfile
            {
                CalculatedAt = now,
                DaysOfData = 0,
                TotalSamples = 0,
                BaselineCpu = 0,
                BaselineMemory = 0,
                BaselineResponseTime = 0,
                HasDailyPattern = false,
                HasWeeklyPattern = false,
                PeakPeriods = new List<PeakPeriod>(),
                Recommendations = Array.Empty<string>()
            };
        }

        var profile = new PerformanceProfile
        {
            CalculatedAt = now,
            DaysOfData = (int)(now - metrics.Min(m => m.Timestamp)).TotalDays,
            TotalSamples = metrics.Count,
            
            BaselineCpu = Math.Round(metrics.Average(m => m.CpuUsagePercent), 2),
            BaselineMemory = Math.Round(metrics.Average(m => m.MemoryUsagePercent), 2),
            BaselineResponseTime = Math.Round(metrics.Average(m => m.ResponseTimeMs), 2),
            
            HasDailyPattern = DetectDailyPattern(metrics),
            HasWeeklyPattern = DetectWeeklyPattern(metrics),
            PeakPeriods = DetectPeakPeriods(metrics),
            
            Recommendations = GenerateRecommendations(metrics)
        };

        return profile;
    }

    #region Private Methods

    private double CalculatePercentile(List<double> values, int percentile)
    {
        if (!values.Any())
            return 0;

        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }

    private bool DetectDailyPattern(List<PerformanceMetric> metrics)
    {
        // Группируем по часу дня
        var byHour = metrics
            .GroupBy(m => m.Timestamp.Hour)
            .Select(g => new { Hour = g.Key, AvgCpu = g.Average(m => m.CpuUsagePercent) })
            .ToList();

        if (!byHour.Any()) return false;

        // Проверяем, есть ли стабильные пики в определенные часы
        var average = byHour.Average(b => b.AvgCpu);
        var peakHours = byHour.Where(h => h.AvgCpu > average * 1.5).ToList();
        return peakHours.Count >= 2 && peakHours.Count <= 4;
    }

    private bool DetectWeeklyPattern(List<PerformanceMetric> metrics)
    {
        // Группируем по дню недели
        var byDay = metrics
            .GroupBy(m => m.Timestamp.DayOfWeek)
            .Select(g => new { Day = g.Key, AvgCpu = g.Average(m => m.CpuUsagePercent) })
            .ToList();

        if (!byDay.Any()) return false;

        // Проверяем разницу между буднями и выходными
        var weekdayAvg = byDay.Where(d => d.Day >= DayOfWeek.Monday && d.Day <= DayOfWeek.Friday)
                              .Select(d => d.AvgCpu)
                              .DefaultIfEmpty(0)
                              .Average();
                              
        var weekendAvg = byDay.Where(d => d.Day == DayOfWeek.Saturday || d.Day == DayOfWeek.Sunday)
                              .Select(d => d.AvgCpu)
                              .DefaultIfEmpty(0)
                              .Average();

        return weekendAvg < weekdayAvg * 0.7; // Выходные на 30% меньше
    }

    private List<PeakPeriod> DetectPeakPeriods(List<PerformanceMetric> metrics)
    {
        var peaks = new List<PeakPeriod>();
        
        // Дневные пики
        var hourlyAvg = metrics
            .GroupBy(m => m.Timestamp.Hour)
            .Select(g => new { Hour = g.Key, AvgLoad = g.Average(m => m.CpuUsagePercent) })
            .ToList();

        if (!hourlyAvg.Any()) return peaks;

        var threshold = hourlyAvg.Average(h => h.AvgLoad) * 1.3;
        var peakHours = hourlyAvg.Where(h => h.AvgLoad > threshold).ToList();

        if (peakHours.Any())
        {
            peaks.Add(new PeakPeriod
            {
                Pattern = "daily",
                TimeRange = $"{peakHours.Min(h => h.Hour):00}:00-{peakHours.Max(h => h.Hour):00}:00",
                AvgLoad = Math.Round(peakHours.Average(h => h.AvgLoad), 2),
                PeakLoad = Math.Round(peakHours.Max(h => h.AvgLoad), 2)
            });
        }

        return peaks;
    }

    private string[] GenerateRecommendations(List<PerformanceMetric> metrics)
    {
        var recommendations = new List<string>();
        
        var avgCpu = metrics.Average(m => m.CpuUsagePercent);
        var avgMemory = metrics.Average(m => m.MemoryUsagePercent);
        var peakCpu = metrics.Max(m => m.CpuUsagePercent);
        var peakMemory = metrics.Max(m => m.MemoryUsagePercent);

        if (avgCpu < 20 && peakCpu < 50)
        {
            recommendations.Add("Нагрузка на CPU низкая. Рассмотрите возможность уменьшения выделенных ресурсов");
        }
        else if (avgCpu > 70 || peakCpu > 90)
        {
            recommendations.Add("Высокая нагрузка на CPU. Рекомендуется увеличить выделенные ресурсы");
        }

        if (avgMemory < 30 && peakMemory < 60)
        {
            recommendations.Add("Использование памяти низкое. Можно уменьшить выделенную память");
        }
        else if (avgMemory > 80 || peakMemory > 95)
        {
            recommendations.Add("Высокое использование памяти. Рекомендуется увеличить память");
        }

        if (DetectDailyPattern(metrics))
        {
            recommendations.Add("Обнаружен дневной паттерн. Рассмотрите возможность использования Spot-инстансов в непиковые часы");
        }

        return recommendations.ToArray();
    }

    #endregion
}