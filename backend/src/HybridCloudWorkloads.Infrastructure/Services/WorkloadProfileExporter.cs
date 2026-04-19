using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HybridCloudWorkloads.Core.Entities;
using HybridCloudWorkloads.Core.Interfaces;
using HybridCloudWorkloads.Infrastructure.Data;

namespace HybridCloudWorkloads.Infrastructure.Services;

public class WorkloadProfileExporter : IWorkloadProfileExporter
{
    private readonly ApplicationDbContext _context;
    private readonly IPerformanceMetricsRepository _metricsRepository;
    private readonly ILogger<WorkloadProfileExporter> _logger;
    
    // Используем инвариантную культуру для чисел (точка вместо запятой)
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public WorkloadProfileExporter(
        ApplicationDbContext context,
        IPerformanceMetricsRepository metricsRepository,
        ILogger<WorkloadProfileExporter> logger)
    {
        _context = context;
        _metricsRepository = metricsRepository;
        _logger = logger;
    }

    public async Task<string> ExportToJsonAsync(Guid workloadId)
    {
        var profile = await BuildFullProfileAsync(workloadId);
        return JsonSerializer.Serialize(profile, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public async Task<string> ExportBatchToJsonAsync(IEnumerable<Guid> workloadIds)
    {
        var profiles = new List<object>();
        foreach (var id in workloadIds)
        {
            try
            {
                var profile = await BuildFullProfileAsync(id);
                profiles.Add(profile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to export profile for workload {WorkloadId}", id);
                profiles.Add(new { Id = id, Error = ex.Message });
            }
        }
        
        return JsonSerializer.Serialize(profiles, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

public async Task<byte[]> ExportToCsvAsync(Guid workloadId)
{
    var profile = await GetOptimizationReadyProfileAsync(workloadId);
    var csv = new StringBuilder();
    
    // Используем точку с запятой как разделитель (лучше для Excel)
    const string delimiter = ";";
    
    // Заголовки
    csv.AppendLine($"Поле{delimiter}Значение");
    
    // Основная информация
    csv.AppendLine($"ID{delimiter}{profile.Id}");
    csv.AppendLine($"Название{delimiter}{EscapeCsvField(profile.Name)}");
    csv.AppendLine($"Требуется CPU (ядер){delimiter}{profile.RequiredCpu}");
    csv.AppendLine($"Требуется памяти (ГБ){delimiter}{FormatDouble(profile.RequiredMemory)}");
    csv.AppendLine($"Требуется хранилища (ГБ){delimiter}{profile.RequiredStorage}");
    
    // Классификация
    csv.AppendLine($"Паттерн использования{delimiter}{profile.UsagePattern}");
    csv.AppendLine($"Класс критичности{delimiter}{profile.Criticality}");
    csv.AppendLine($"Уровень бюджета{delimiter}{profile.BudgetTier}");
    
    // SLA
    csv.AppendLine($"Макс. время отклика (мс){delimiter}{profile.MaxResponseTimeMs}");
    csv.AppendLine($"Целевая доступность (%){delimiter}{FormatDouble(profile.AvailabilityTarget)}");
    csv.AppendLine($"Требуется резервирование{delimiter}{(profile.RequiresRedundancy ? "Да" : "Нет")}");
    
    // Метрики
    csv.AppendLine($"Средний CPU (%){delimiter}{FormatDouble(profile.AvgCpuPercent)}");
    csv.AppendLine($"Средняя память (%){delimiter}{FormatDouble(profile.AvgMemoryPercent)}");
    csv.AppendLine($"Пиковый CPU (%){delimiter}{FormatDouble(profile.PeakCpuPercent)}");
    csv.AppendLine($"Пиковая память (%){delimiter}{FormatDouble(profile.PeakMemoryPercent)}");
    csv.AppendLine($"Среднее время отклика (мс){delimiter}{FormatDouble(profile.AvgResponseTimeMs)}");
    csv.AppendLine($"P95 время отклика (мс){delimiter}{FormatDouble(profile.P95ResponseTimeMs)}");
    csv.AppendLine($"Среднее RPS{delimiter}{FormatDouble(profile.AvgRequestsPerSecond)}");
    
    // Теги
    csv.AppendLine($"Теги{delimiter}{EscapeCsvField(profile.Tags != null ? string.Join("; ", profile.Tags) : "")}");
    csv.AppendLine($"Дата последнего профилирования{delimiter}{profile.LastProfiledAt:yyyy-MM-dd HH:mm:ss}");
    
    // Рекомендации
    if (profile.Recommendations != null && profile.Recommendations.Any())
    {
        csv.AppendLine($"Рекомендации{delimiter}{EscapeCsvField(string.Join("; ", profile.Recommendations))}");
    }
    
    // === ИСПРАВЛЕНИЕ: Используем Windows-1251 для кириллицы ===
    // Регистрируем провайдер кодировок для поддержки Windows-1251
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    var windows1251 = Encoding.GetEncoding(1251);
    return windows1251.GetBytes(csv.ToString());
}

public async Task<byte[]> ExportBatchToCsvAsync(IEnumerable<Guid> workloadIds)
{
    var profiles = new List<OptimizationReadyProfile>();
    foreach (var id in workloadIds)
    {
        try
        {
            var profile = await GetOptimizationReadyProfileAsync(id);
            profiles.Add(profile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to export profile for workload {WorkloadId}", id);
        }
    }
    
    var csv = new StringBuilder();
    
    // Используем точку с запятой как разделитель
    const string delimiter = ";";
    
    // Заголовки
    csv.AppendLine($"ID{delimiter}Название{delimiter}CPU(ядер){delimiter}Память(ГБ){delimiter}Хранилище(ГБ){delimiter}" +
                  $"Паттерн{delimiter}Критичность{delimiter}Бюджет{delimiter}Ср.CPU(%){delimiter}Пик.CPU(%){delimiter}" +
                  $"Ср.Память(%){delimiter}Пик.Память(%){delimiter}Ср.Отклик(мс){delimiter}P95(мс){delimiter}RPS{delimiter}Теги");
    
    // Данные
    foreach (var p in profiles)
    {
        csv.AppendLine($"{p.Id}{delimiter}{EscapeCsvField(p.Name)}{delimiter}{p.RequiredCpu}{delimiter}" +
                      $"{FormatDouble(p.RequiredMemory)}{delimiter}{p.RequiredStorage}{delimiter}" +
                      $"{p.UsagePattern}{delimiter}{p.Criticality}{delimiter}{p.BudgetTier}{delimiter}" +
                      $"{FormatDouble(p.AvgCpuPercent)}{delimiter}{FormatDouble(p.PeakCpuPercent)}{delimiter}" +
                      $"{FormatDouble(p.AvgMemoryPercent)}{delimiter}{FormatDouble(p.PeakMemoryPercent)}{delimiter}" +
                      $"{FormatDouble(p.AvgResponseTimeMs)}{delimiter}{FormatDouble(p.P95ResponseTimeMs)}{delimiter}" +
                      $"{FormatDouble(p.AvgRequestsPerSecond)}{delimiter}{EscapeCsvField(p.Tags != null ? string.Join(",", p.Tags) : "")}");
    }
    
    // === ИСПРАВЛЕНИЕ: Используем Windows-1251 ===
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    var windows1251 = Encoding.GetEncoding(1251);
    return windows1251.GetBytes(csv.ToString());
}

    public async Task<byte[]> ExportToExcelAsync(Guid workloadId)
    {
        // Для Excel используем CSV с расширением .xlsx (упрощенный вариант)
        return await ExportToCsvAsync(workloadId);
    }

    public async Task<byte[]> ExportBatchToExcelAsync(IEnumerable<Guid> workloadIds)
    {
        return await ExportBatchToCsvAsync(workloadIds);
    }

    public async Task<OptimizationReadyProfile> GetOptimizationReadyProfileAsync(Guid workloadId)
    {
        var workload = await _context.Workloads
            .FirstOrDefaultAsync(w => w.Id == workloadId);
        
        if (workload == null)
        {
            throw new ArgumentException($"Workload {workloadId} not found");
        }
        
        // Получаем метрики за последние 7 дней
        var weekAgo = DateTime.UtcNow.AddDays(-7);
        var metrics = await _metricsRepository.GetAggregatedMetricsAsync(workloadId, weekAgo, DateTime.UtcNow);
        
        // Парсим SLA требования
        SlaRequirement? slaRequirements = null;
        if (!string.IsNullOrEmpty(workload.SlaRequirements))
        {
            try
            {
                slaRequirements = JsonSerializer.Deserialize<SlaRequirement>(workload.SlaRequirements);
            }
            catch { }
        }
        
        // Парсим бизнес-часы
        BusinessHoursInfo? businessHours = null;
        if (!string.IsNullOrEmpty(workload.BusinessHours))
        {
            try
            {
                businessHours = JsonSerializer.Deserialize<BusinessHoursInfo>(workload.BusinessHours);
            }
            catch { }
        }
        
        // Получаем рекомендации из профиля
        string[]? recommendations = null;
        if (!string.IsNullOrEmpty(workload.BaselinePerformance))
        {
            try
            {
                var profile = JsonSerializer.Deserialize<PerformanceProfile>(workload.BaselinePerformance);
                recommendations = profile?.Recommendations;
            }
            catch { }
        }
        
        return new OptimizationReadyProfile
        {
            Id = workload.Id,
            Name = workload.Name,
            RequiredCpu = workload.RequiredCpu,
            RequiredMemory = workload.RequiredMemory,
            RequiredStorage = workload.RequiredStorage,
            UsagePattern = workload.UsagePattern.ToString(),
            Criticality = workload.Criticality.ToString(),
            BudgetTier = workload.BudgetTier.ToString(),
            MaxResponseTimeMs = slaRequirements?.MaxResponseTimeMs ?? 1000,
            AvailabilityTarget = slaRequirements?.AvailabilityTarget ?? 99.9,
            RequiresRedundancy = slaRequirements?.RequiresRedundancy ?? false,
            AvgCpuPercent = Math.Round(metrics.AvgCpuUsagePercent, 2),
            AvgMemoryPercent = Math.Round(metrics.AvgMemoryUsagePercent, 2),
            PeakCpuPercent = Math.Round(metrics.PeakCpuUsagePercent, 2),
            PeakMemoryPercent = Math.Round(metrics.PeakMemoryUsagePercent, 2),
            AvgResponseTimeMs = Math.Round(metrics.AvgResponseTimeMs, 2),
            P95ResponseTimeMs = Math.Round(metrics.P95ResponseTimeMs, 2),
            AvgRequestsPerSecond = Math.Round(metrics.AvgRequestsPerSecond, 2),
            Timezone = businessHours?.Timezone,
            WeekendLoadPercent = businessHours?.WeekendLoadPercent ?? 30,
            Tags = workload.Tags,
            LastProfiledAt = workload.LastProfiledAt ?? DateTime.UtcNow,
            Recommendations = recommendations,
            AdditionalData = new Dictionary<string, object>
            {
                ["ContainerImage"] = workload.ContainerImage ?? string.Empty,
                ["DeploymentStatus"] = workload.DeploymentStatus ?? "NotDeployed",
                ["DeployedAt"] = workload.DeployedAt ?? DateTime.MinValue,
                ["HasMetrics"] = metrics.SampleCount > 0
            }
        };
    }

    public async Task<List<OptimizationReadyProfile>> GetBatchOptimizationReadyProfilesAsync(IEnumerable<Guid> workloadIds)
    {
        var profiles = new List<OptimizationReadyProfile>();
        foreach (var id in workloadIds)
        {
            try
            {
                var profile = await GetOptimizationReadyProfileAsync(id);
                profiles.Add(profile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get optimization profile for workload {WorkloadId}", id);
            }
        }
        return profiles;
    }

    public async Task<string> ExportToPrometheusFormatAsync(Guid workloadId)
    {
        var profile = await GetOptimizationReadyProfileAsync(workloadId);
        var sb = new StringBuilder();
        
        // Метрики в формате Prometheus
        sb.AppendLine($"# HELP workload_cpu_usage_current Текущее использование CPU workload");
        sb.AppendLine($"# TYPE workload_cpu_usage_current gauge");
        sb.AppendLine($"workload_cpu_usage_current{{workload_id=\"{profile.Id}\", workload_name=\"{profile.Name}\"}} {FormatDouble(profile.AvgCpuPercent)}");
        
        sb.AppendLine($"# HELP workload_memory_usage_current Текущее использование памяти workload");
        sb.AppendLine($"# TYPE workload_memory_usage_current gauge");
        sb.AppendLine($"workload_memory_usage_current{{workload_id=\"{profile.Id}\", workload_name=\"{profile.Name}\"}} {FormatDouble(profile.AvgMemoryPercent)}");
        
        sb.AppendLine($"# HELP workload_response_time_ms Время отклика workload");
        sb.AppendLine($"# TYPE workload_response_time_ms gauge");
        sb.AppendLine($"workload_response_time_ms{{workload_id=\"{profile.Id}\", workload_name=\"{profile.Name}\"}} {FormatDouble(profile.AvgResponseTimeMs)}");
        
        sb.AppendLine($"# HELP workload_requests_per_second RPS workload");
        sb.AppendLine($"# TYPE workload_requests_per_second gauge");
        sb.AppendLine($"workload_requests_per_second{{workload_id=\"{profile.Id}\", workload_name=\"{profile.Name}\"}} {FormatDouble(profile.AvgRequestsPerSecond)}");
        
        sb.AppendLine($"# HELP workload_required_cpu Требуемое количество CPU ядер");
        sb.AppendLine($"# TYPE workload_required_cpu gauge");
        sb.AppendLine($"workload_required_cpu{{workload_id=\"{profile.Id}\", workload_name=\"{profile.Name}\"}} {profile.RequiredCpu}");
        
        sb.AppendLine($"# HELP workload_required_memory_gb Требуемый объем памяти в ГБ");
        sb.AppendLine($"# TYPE workload_required_memory_gb gauge");
        sb.AppendLine($"workload_required_memory_gb{{workload_id=\"{profile.Id}\", workload_name=\"{profile.Name}\"}} {FormatDouble(profile.RequiredMemory)}");
        
        // Информационные метки
        sb.AppendLine($"# HELP workload_info Информация о workload");
        sb.AppendLine($"# TYPE workload_info gauge");
        sb.AppendLine($"workload_info{{workload_id=\"{profile.Id}\", workload_name=\"{profile.Name}\", " +
                     $"criticality=\"{profile.Criticality}\", budget_tier=\"{profile.BudgetTier}\", " +
                     $"usage_pattern=\"{profile.UsagePattern}\"}} 1");
        
        return sb.ToString();
    }

    #region Private Methods

    private async Task<object> BuildFullProfileAsync(Guid workloadId)
    {
        var workload = await _context.Workloads
            .FirstOrDefaultAsync(w => w.Id == workloadId);
        
        if (workload == null)
        {
            return new { Id = workloadId, Error = "Workload not found" };
        }
        
        var optimizationProfile = await GetOptimizationReadyProfileAsync(workloadId);
        
        return new
        {
            optimizationProfile.Id,
            optimizationProfile.Name,
            Description = workload.Description,
            Type = workload.Type.ToString(),
            optimizationProfile.RequiredCpu,
            optimizationProfile.RequiredMemory,
            optimizationProfile.RequiredStorage,
            CreatedAt = workload.CreatedAt,
            UpdatedAt = workload.UpdatedAt,
            Classification = new
            {
                optimizationProfile.UsagePattern,
                optimizationProfile.Criticality,
                optimizationProfile.BudgetTier
            },
            Sla = new
            {
                optimizationProfile.MaxResponseTimeMs,
                optimizationProfile.AvailabilityTarget,
                optimizationProfile.RequiresRedundancy
            },
            Metrics = new
            {
                optimizationProfile.AvgCpuPercent,
                optimizationProfile.AvgMemoryPercent,
                optimizationProfile.PeakCpuPercent,
                optimizationProfile.PeakMemoryPercent,
                optimizationProfile.AvgResponseTimeMs,
                optimizationProfile.P95ResponseTimeMs,
                optimizationProfile.AvgRequestsPerSecond
            },
            BusinessHours = new
            {
                optimizationProfile.Timezone,
                optimizationProfile.WeekendLoadPercent
            },
            optimizationProfile.Tags,
            optimizationProfile.LastProfiledAt,
            Deployment = new
            {
                workload.ContainerImage,
                workload.ExposedPort,
                workload.DeploymentStatus,
                workload.AccessUrl
            },
            optimizationProfile.Recommendations
        };
    }

    private string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;
        
        // Если поле содержит разделитель, кавычки или перенос строки, оборачиваем в кавычки
        if (field.Contains(';') || field.Contains('"') || field.Contains('\n') || field.Contains(','))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        
        return field;
    }

    private string FormatDouble(double value)
    {
        // Используем инвариантную культуру для форматирования (точка вместо запятой)
        return value.ToString(InvariantCulture);
    }

    #endregion
}