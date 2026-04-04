using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity; // ДОБАВЛЕНО
using HybridCloudWorkloads.Core.Entities;
using HybridCloudWorkloads.Core.Interfaces;
using HybridCloudWorkloads.Infrastructure.Data;
using HybridCloudWorkloads.Infrastructure.Entities; // ДОБАВЛЕНО для User
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HybridCloudWorkloads.API.Controllers;

[Authorize]
[ApiController]
[Route("api/metrics")]
public class PerformanceMetricsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPerformanceMetricsRepository _metricsRepository;
    private readonly ILogger<PerformanceMetricsController> _logger;
    private readonly UserManager<User> _userManager; // ДОБАВЛЕНО

    public PerformanceMetricsController(
        ApplicationDbContext context,
        IPerformanceMetricsRepository metricsRepository,
        ILogger<PerformanceMetricsController> logger,
        UserManager<User> userManager) // ДОБАВЛЕНО
    {
        _context = context;
        _metricsRepository = metricsRepository;
        _logger = logger;
        _userManager = userManager; // ДОБАВЛЕНО
    }

    /// <summary>
    /// Получить последние метрики для workload
    /// </summary>
    [HttpGet("workload/{workloadId}/latest")]
    public async Task<ActionResult<List<PerformanceMetric>>> GetLatestMetrics(
        Guid workloadId, 
        [FromQuery] int count = 100)
    {
        var userId = GetCurrentUserId();
        var workload = await _context.Workloads
            .FirstOrDefaultAsync(w => w.Id == workloadId && w.UserId == userId);

        if (workload == null)
            return NotFound("Workload not found");

        var metrics = await _metricsRepository.GetLatestMetricsAsync(workloadId, count);
        return Ok(metrics);
    }

    /// <summary>
    /// Получить агрегированные метрики за период
    /// </summary>
    [HttpGet("workload/{workloadId}/aggregated")]
    public async Task<ActionResult<AggregatedMetricsDto>> GetAggregatedMetrics(
        Guid workloadId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string periodType = "Day")
    {
        var userId = GetCurrentUserId();
        var workload = await _context.Workloads
            .FirstOrDefaultAsync(w => w.Id == workloadId && w.UserId == userId);

        if (workload == null)
            return NotFound("Workload not found");

        var endDate = to ?? DateTime.UtcNow;
        var startDate = from ?? endDate.AddDays(-7); // По умолчанию за 7 дней

        var aggregated = await _metricsRepository.GetAggregatedMetricsAsync(
            workloadId, startDate, endDate, periodType);

        var timeSeries = await _metricsRepository.GetTimeSeriesAsync(
            workloadId, startDate, endDate, "1h");

        var dto = new AggregatedMetricsDto
        {
            WorkloadId = workloadId,
            WorkloadName = workload.Name,
            PeriodStart = startDate,
            PeriodEnd = endDate,
            PeriodType = periodType,
            
            AvgCpu = Math.Round(aggregated.AvgCpuUsagePercent, 2),
            AvgMemory = Math.Round(aggregated.AvgMemoryUsagePercent, 2),
            PeakCpu = Math.Round(aggregated.PeakCpuUsagePercent, 2),
            PeakMemory = Math.Round(aggregated.PeakMemoryUsagePercent, 2),
            P95Cpu = Math.Round(aggregated.P95CpuUsagePercent, 2),
            P95Memory = Math.Round(aggregated.P95MemoryUsagePercent, 2),
            
            AvgResponseTime = Math.Round(aggregated.AvgResponseTimeMs, 2),
            P95ResponseTime = Math.Round(aggregated.P95ResponseTimeMs, 2),
            P99ResponseTime = Math.Round(aggregated.P99ResponseTimeMs, 2),
            
            AvgRps = Math.Round(aggregated.AvgRequestsPerSecond, 2),
            PeakRps = Math.Round(aggregated.PeakRequestsPerSecond, 2),
            
            TotalNetworkIn = aggregated.TotalNetworkInBytes,
            TotalNetworkOut = aggregated.TotalNetworkOutBytes,
            
            Availability = Math.Round(aggregated.AvailabilityPercent, 2),
            ErrorCount = aggregated.TotalErrorCount,
            SampleCount = aggregated.SampleCount,
            
            TimeSeriesData = timeSeries
        };

        return Ok(dto);
    }

    /// <summary>
    /// Получить метрики для нескольких workloads (batch)
    /// </summary>
    [HttpPost("workloads/batch")]
    public async Task<ActionResult<Dictionary<Guid, AggregatedMetricsDto>>> GetBatchMetrics(
        [FromBody] BatchMetricsRequest request)
    {
        var userId = GetCurrentUserId();
        var workloads = await _context.Workloads
            .Where(w => w.UserId == userId && request.WorkloadIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name);

        if (!workloads.Any())
            return NotFound("No workloads found");

        var endDate = request.To ?? DateTime.UtcNow;
        var startDate = request.From ?? endDate.AddDays(-7);

        var aggregated = await _metricsRepository.GetBatchAggregatedMetricsAsync(
            request.WorkloadIds, startDate, endDate);

        var result = new Dictionary<Guid, AggregatedMetricsDto>();
        
        foreach (var kvp in aggregated)
        {
            if (workloads.TryGetValue(kvp.Key, out var workloadName))
            {
                result[kvp.Key] = new AggregatedMetricsDto
                {
                    WorkloadId = kvp.Key,
                    WorkloadName = workloadName,
                    PeriodStart = startDate,
                    PeriodEnd = endDate,
                    PeriodType = "Day",
                    
                    AvgCpu = Math.Round(kvp.Value.AvgCpuUsagePercent, 2),
                    AvgMemory = Math.Round(kvp.Value.AvgMemoryUsagePercent, 2),
                    PeakCpu = Math.Round(kvp.Value.PeakCpuUsagePercent, 2),
                    PeakMemory = Math.Round(kvp.Value.PeakMemoryUsagePercent, 2),
                    
                    AvgResponseTime = Math.Round(kvp.Value.AvgResponseTimeMs, 2),
                    P95ResponseTime = Math.Round(kvp.Value.P95ResponseTimeMs, 2),
                    
                    Availability = Math.Round(kvp.Value.AvailabilityPercent, 2),
                    SampleCount = kvp.Value.SampleCount
                };
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// Получить временной ряд для графика
    /// </summary>
    [HttpGet("workload/{workloadId}/timeseries")]
    public async Task<ActionResult<ChartDataPoint[]>> GetTimeSeries(
        Guid workloadId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string interval = "1h")
    {
        var userId = GetCurrentUserId();
        var workload = await _context.Workloads
            .FirstOrDefaultAsync(w => w.Id == workloadId && w.UserId == userId);

        if (workload == null)
            return NotFound("Workload not found");

        var timeSeries = await _metricsRepository.GetTimeSeriesAsync(
            workloadId, from, to, interval);

        return Ok(timeSeries);
    }

    /// <summary>
    /// Рассчитать профиль производительности
    /// </summary>
    [HttpGet("workload/{workloadId}/profile")]
    public async Task<ActionResult<PerformanceProfile>> GetPerformanceProfile(Guid workloadId)
    {
        var userId = GetCurrentUserId();
        var workload = await _context.Workloads
            .FirstOrDefaultAsync(w => w.Id == workloadId && w.UserId == userId);

        if (workload == null)
            return NotFound("Workload not found");

        var profile = await _metricsRepository.CalculatePerformanceProfileAsync(workloadId);
        
        // Сохраняем профиль в workload
        workload.BaselinePerformance = JsonSerializer.Serialize(profile);
        workload.LastProfiledAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(profile);
    }

    /// <summary>
    /// Добавить метрику вручную (для тестирования)
    /// </summary>
    [HttpPost("workload/{workloadId}")]
    public async Task<IActionResult> AddMetric(Guid workloadId, [FromBody] AddMetricRequest request)
    {
        var userId = GetCurrentUserId();
        var workload = await _context.Workloads
            .FirstOrDefaultAsync(w => w.Id == workloadId && w.UserId == userId);

        if (workload == null)
            return NotFound("Workload not found");

        var metric = new PerformanceMetric
        {
            WorkloadId = workloadId,
            CpuUsagePercent = request.CpuUsagePercent,
            MemoryUsagePercent = request.MemoryUsagePercent,
            MemoryUsageMB = request.MemoryUsageMB,
            NetworkInBytesPerSec = request.NetworkInBytesPerSec,
            NetworkOutBytesPerSec = request.NetworkOutBytesPerSec,
            ResponseTimeMs = request.ResponseTimeMs,
            RequestsPerSecond = request.RequestsPerSecond,
            ContainerStatus = request.ContainerStatus
        };

        await _metricsRepository.AddMetricAsync(metric);
        return Ok(new { message = "Metric added successfully" });
    }

    private Guid GetCurrentUserId()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User not found");
        return Guid.Parse(userId);
    }
}

public class BatchMetricsRequest
{
    public Guid[] WorkloadIds { get; set; } = Array.Empty<Guid>();
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class AddMetricRequest
{
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
    public double MemoryUsageMB { get; set; }
    public long NetworkInBytesPerSec { get; set; }
    public long NetworkOutBytesPerSec { get; set; }
    public double ResponseTimeMs { get; set; }
    public double RequestsPerSecond { get; set; }
    public string? ContainerStatus { get; set; }
}