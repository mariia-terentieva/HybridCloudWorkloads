using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HybridCloudWorkloads.Core.Entities;
using HybridCloudWorkloads.Core.Interfaces;
using HybridCloudWorkloads.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HybridCloudWorkloads.Infrastructure.Services;

public class MetricsCollectorService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MetricsCollectorService> _logger;
    private readonly TimeSpan _collectionInterval = TimeSpan.FromSeconds(30); // Сбор каждые 30 секунд

    public MetricsCollectorService(
        IServiceProvider services,
        ILogger<MetricsCollectorService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MetricsCollectorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectMetricsAsync();
                await Task.Delay(_collectionInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting metrics");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task CollectMetricsAsync()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var metricsRepository = scope.ServiceProvider.GetRequiredService<IPerformanceMetricsRepository>();

        // Получаем все workloads с запущенными контейнерами
        var runningWorkloads = await context.Workloads
            .Where(w => w.DeploymentStatus == "Running" && !string.IsNullOrEmpty(w.ContainerId))
            .ToListAsync();

        if (!runningWorkloads.Any())
        {
            _logger.LogDebug("No running containers found");
            return;
        }

        _logger.LogInformation("Collecting metrics for {Count} running containers", runningWorkloads.Count);

        foreach (var workload in runningWorkloads)
        {
            try
            {
                var metrics = await GetContainerMetricsAsync(workload.ContainerId);
                
                if (metrics != null)
                {
                    var metric = new PerformanceMetric
                    {
                        WorkloadId = workload.Id,
                        Timestamp = DateTime.UtcNow,
                        CpuUsagePercent = metrics.CpuUsagePercent,
                        MemoryUsagePercent = metrics.MemoryUsagePercent,
                        MemoryUsageMB = metrics.MemoryUsageMB,
                        NetworkInBytesPerSec = metrics.NetworkInBytesPerSec,
                        NetworkOutBytesPerSec = metrics.NetworkOutBytesPerSec,
                        ResponseTimeMs = metrics.ResponseTimeMs,
                        RequestsPerSecond = metrics.RequestsPerSecond,
                        ContainerStatus = "Running"
                    };

                    await metricsRepository.AddMetricAsync(metric);
                    _logger.LogDebug("Metrics saved for workload {WorkloadName}", workload.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect metrics for workload {WorkloadId}", workload.Id);
            }
        }
    }

private async Task<ContainerMetrics?> GetContainerMetricsAsync(string containerId)
{
    try
    {
        var stats = await GetDockerStatsAsync(containerId);
        if (stats == null) return null;

        // Получаем CPU usage с защитой от деления на ноль
        var cpuDelta = stats.CpuStats.CpuUsage.TotalUsage - stats.PreCpuStats.CpuUsage.TotalUsage;
        var systemDelta = stats.CpuStats.SystemCpuUsage - stats.PreCpuStats.SystemCpuUsage;
        
        double cpuPercent = 0;
        if (systemDelta > 0 && cpuDelta > 0)
        {
            cpuPercent = (double)cpuDelta / systemDelta * 100 * stats.CpuStats.OnlineCpus;
            // Проверка на бесконечность
            if (double.IsInfinity(cpuPercent) || double.IsNaN(cpuPercent))
                cpuPercent = 0;
        }

        // Получаем Memory usage с защитой от деления на ноль
        double memoryPercent = 0;
        double memoryMB = 0;
        if (stats.MemoryStats.Limit > 0)
        {
            memoryPercent = stats.MemoryStats.Usage / (double)stats.MemoryStats.Limit * 100;
            memoryMB = stats.MemoryStats.Usage / 1024.0 / 1024.0;
            
            if (double.IsInfinity(memoryPercent) || double.IsNaN(memoryPercent))
                memoryPercent = 0;
            if (double.IsInfinity(memoryMB) || double.IsNaN(memoryMB))
                memoryMB = 0;
        }

        // Получаем Network usage
        long networkIn = 0, networkOut = 0;
        if (stats.Networks != null)
        {
            foreach (var net in stats.Networks)
            {
                networkIn += net.Value.RxBytes;
                networkOut += net.Value.TxBytes;
            }
        }

        return new ContainerMetrics
        {
            CpuUsagePercent = Math.Round(cpuPercent, 2),
            MemoryUsagePercent = Math.Round(memoryPercent, 2),
            MemoryUsageMB = Math.Round(memoryMB, 2),
            NetworkInBytesPerSec = networkIn,
            NetworkOutBytesPerSec = networkOut,
            ResponseTimeMs = 0,
            RequestsPerSecond = 0
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting Docker stats for container {ContainerId}", containerId);
        return null;
    }
}

    private async Task<DockerStats?> GetDockerStatsAsync(string containerId)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"stats --no-stream --format \"{{{{json .}}}}\" {containerId}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
            {
                _logger.LogWarning("Failed to get stats for container {ContainerId}: {Error}", containerId, error);
                return null;
            }

            return JsonSerializer.Deserialize<DockerStats>(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing docker stats");
            return null;
        }
    }

    private double EstimateRps(DockerStats stats)
    {
        // Это приблизительная оценка. Для точного RPS нужно собирать из приложения
        // через API или логи
        return 0;
    }

    private class ContainerMetrics
    {
        public double CpuUsagePercent { get; set; }
        public double MemoryUsagePercent { get; set; }
        public double MemoryUsageMB { get; set; }
        public long NetworkInBytesPerSec { get; set; }
        public long NetworkOutBytesPerSec { get; set; }
        public double ResponseTimeMs { get; set; }
        public double RequestsPerSecond { get; set; }
    }

    // Классы для парсинга Docker stats JSON
    private class DockerStats
    {
        public string Name { get; set; } = string.Empty;
        public CpuStatsData CpuStats { get; set; } = new();
        public CpuStatsData PreCpuStats { get; set; } = new();
        public MemoryStatsData MemoryStats { get; set; } = new();
        public Dictionary<string, NetworkStatsData>? Networks { get; set; }
    }

    private class CpuStatsData
    {
        public CpuUsageData CpuUsage { get; set; } = new();
        public long SystemCpuUsage { get; set; }
        public long OnlineCpus { get; set; }
    }

    private class CpuUsageData
    {
        public long TotalUsage { get; set; }
    }

    private class MemoryStatsData
    {
        public long Usage { get; set; }
        public long Limit { get; set; }
    }

    private class NetworkStatsData
    {
        public long RxBytes { get; set; }
        public long TxBytes { get; set; }
    }
}