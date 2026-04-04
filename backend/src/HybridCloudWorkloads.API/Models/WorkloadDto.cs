using HybridCloudWorkloads.Core.Entities;

namespace HybridCloudWorkloads.API.Models;

public class WorkloadDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public int RequiredCpu { get; set; }
    public double RequiredMemory { get; set; }
    public double RequiredStorage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Существующие поля для деплоя
    public string? ContainerImage { get; set; }
    public int ExposedPort { get; set; } = 80;
    public string? EnvironmentVariables { get; set; }
    public string? DeploymentStatus { get; set; }
    public string? ContainerId { get; set; }
    public string? AccessUrl { get; set; }
    public DateTime? DeployedAt { get; set; }
    
    // НОВЫЕ ПОЛЯ: Многофакторная классификация
    public string UsagePattern { get; set; } = "Constant";
    public string Criticality { get; set; } = "NonCritical";
    public string BudgetTier { get; set; } = "Medium";
    public SlaRequirementDto? SlaRequirements { get; set; }
    public BusinessHoursDto? BusinessHours { get; set; }
    public string[]? Tags { get; set; }
    public DateTime? LastProfiledAt { get; set; }
    public BaselinePerformanceDto? BaselinePerformance { get; set; }
}

public class CreateWorkloadRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public int RequiredCpu { get; set; }
    public double RequiredMemory { get; set; }
    public double RequiredStorage { get; set; }
    
    // Существующие поля для деплоя
    public string? ContainerImage { get; set; }
    public int ExposedPort { get; set; } = 80;
    public string? EnvironmentVariables { get; set; }
    
    // НОВЫЕ ПОЛЯ
    public string UsagePattern { get; set; } = "Constant";
    public string Criticality { get; set; } = "NonCritical";
    public string BudgetTier { get; set; } = "Medium";
    public SlaRequirementDto? SlaRequirements { get; set; }
    public BusinessHoursDto? BusinessHours { get; set; }
    public string[]? Tags { get; set; }
}

public class UpdateWorkloadRequest : CreateWorkloadRequest
{
    // Наследует все поля из CreateWorkloadRequest
}

// DTO для требований SLA
public class SlaRequirementDto
{
    public int MaxResponseTimeMs { get; set; } = 1000;
    public int AllowedDowntimePerMonth { get; set; } = 60;
    public double AvailabilityTarget { get; set; } = 99.9;
    public bool RequiresRedundancy { get; set; } = false;
    public int MinReplicas { get; set; } = 1;
    public int MaxRecoveryTimeMinutes { get; set; } = 60;
}

// DTO для бизнес-часов
public class BusinessHoursDto
{
    public string Timezone { get; set; } = "UTC";
    public List<TimeRangeDto> PeakHours { get; set; } = new();
    public double WeekendLoadPercent { get; set; } = 30;
    public int[] WorkingDays { get; set; } = { 1, 2, 3, 4, 5 };
}

public class TimeRangeDto
{
    public string Start { get; set; } = "09:00";
    public string End { get; set; } = "18:00";
}

// DTO для базовой производительности
public class BaselinePerformanceDto
{
    public double AvgCpuPercent { get; set; }
    public double AvgMemoryPercent { get; set; }
    public double PeakCpuPercent { get; set; }
    public double PeakMemoryPercent { get; set; }
    public double AvgResponseTimeMs { get; set; }
    public double P95ResponseTimeMs { get; set; }
    public double RequestsPerSecond { get; set; }
    public DateTime MeasuredAt { get; set; }
    public int SampleCount { get; set; }
}

// DTO для экспорта в модуль оптимизации
public class WorkloadProfileForOptimization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RequiredCpu { get; set; }
    public double RequiredMemory { get; set; }
    public double RequiredStorage { get; set; }
    
    // Классификационные поля
    public string UsagePattern { get; set; } = string.Empty;
    public string Criticality { get; set; } = string.Empty;
    public string BudgetTier { get; set; } = string.Empty;
    
    // SLA требования
    public SlaRequirementDto? SlaRequirements { get; set; }
    
    // Бизнес-часы
    public BusinessHoursDto? BusinessHours { get; set; }
    
    // Метрики производительности (агрегированные)
    public BaselinePerformanceDto? BaselinePerformance { get; set; }
    
    // Метаданные
    public string[]? Tags { get; set; }
    public DateTime? LastProfiledAt { get; set; }
}