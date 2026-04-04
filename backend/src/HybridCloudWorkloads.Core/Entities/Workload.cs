/*namespace HybridCloudWorkloads.Core.Entities;

public class Workload
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkloadType Type { get; set; }
    public int RequiredCpu { get; set; }
    public double RequiredMemory { get; set; }
    public double RequiredStorage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid UserId { get; set; }
    
    // Новые поля для деплоя
    public string? ContainerImage { get; set; }
    public int ExposedPort { get; set; } = 80;
    public string? EnvironmentVariables { get; set; }
    public string? DeploymentStatus { get; set; }
    public string? ContainerId { get; set; }
    public string? AccessUrl { get; set; }
    public DateTime? DeployedAt { get; set; }
}

public enum WorkloadType
{
    VirtualMachine,
    Database,
    WebService,
    Container,
    BatchJob
}*/

namespace HybridCloudWorkloads.Core.Entities;

public class Workload
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkloadType Type { get; set; }
    public int RequiredCpu { get; set; }
    public double RequiredMemory { get; set; }
    public double RequiredStorage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid UserId { get; set; }
    
    // Существующие поля для деплоя
    public string? ContainerImage { get; set; }
    public int ExposedPort { get; set; } = 80;
    public string? EnvironmentVariables { get; set; }
    public string? DeploymentStatus { get; set; }
    public string? ContainerId { get; set; }
    public string? AccessUrl { get; set; }
    public DateTime? DeployedAt { get; set; }
    
    // НОВЫЕ ПОЛЯ: Многофакторная классификация
    
    /// Паттерн использования нагрузки
    public UsagePattern UsagePattern { get; set; } = UsagePattern.Constant;
    
    /// Класс критичности для бизнеса
    public CriticalityClass Criticality { get; set; } = CriticalityClass.NonCritical;
    
    /// Уровень допустимого бюджета
    public BudgetTier BudgetTier { get; set; } = BudgetTier.Medium;
    
    /// Требования к SLA (хранятся в JSONB формате)
    public string? SlaRequirements { get; set; }
    
    /// Бизнес-часы работы (JSONB)
    public string? BusinessHours { get; set; }
    
    /// Теги для дополнительной классификации
    public string[]? Tags { get; set; }
    
    /// Дата последнего профилирования
    public DateTime? LastProfiledAt { get; set; }
    
    /// Базовая производительность (JSONB с метриками)
    public string? BaselinePerformance { get; set; }
}

    public enum WorkloadType
    {
        VirtualMachine,
        Database,
        WebService,
        Container,
        BatchJob
    }

/// Паттерны использования нагрузки
public enum UsagePattern
{
    /// Постоянная нагрузка 24/7
    Constant = 0,
    
    /// Периодическая нагрузка (например, по часам/дням)
    Periodic = 1,
    
    /// Пиковая нагрузка (bursts)
    Burst = 2,
    
    /// Непредсказуемая нагрузка
    Unpredictable = 3
}

/// Классы критичности для бизнеса
public enum CriticalityClass
{
    /// Критически важные для бизнеса системы
    MissionCritical = 0,

    /// Важные для бизнеса, но допустимы кратковременные перерывы
    BusinessEssential = 1,
    
    /// Некритичные нагрузки, тестовые среды
    NonCritical = 2
}

/// Уровни допустимого бюджета
public enum BudgetTier
{
    /// Высокий бюджет, приоритет производительности
    High = 0,
    
    /// Средний бюджет, баланс стоимости и производительности
    Medium = 1,
    
    /// Низкий бюджет, приоритет экономии
    Low = 2
}

/// Класс для хранения требований SLA
public class SlaRequirement
{
    /// Максимальное время отклика в миллисекундах
    public int MaxResponseTimeMs { get; set; } = 1000;
    
    /// Допустимое время простоя в месяц (в минутах)
    public int AllowedDowntimePerMonth { get; set; } = 60;
    
    /// Целевая доступность (99.9, 99.99 и т.д.)
    public double AvailabilityTarget { get; set; } = 99.9;
    
    /// Требуется ли резервирование
    public bool RequiresRedundancy { get; set; } = false;
    
    /// Минимальное количество реплик
    public int MinReplicas { get; set; } = 1;
    
    /// Максимальное время восстановления (минуты)
    public int MaxRecoveryTimeMinutes { get; set; } = 60;
}

/// Класс для хранения информации о бизнес-часах
public class BusinessHoursInfo
{
    /// Часовой пояс (IANA format)
    public string Timezone { get; set; } = "UTC";
    
    /// Пиковые часы работы
    public List<TimeRange> PeakHours { get; set; } = new();
    
    /// Нагрузка в выходные (% от пиковой)
    public double WeekendLoadPercent { get; set; } = 30;
    
    /// Рабочие дни недели (1-7, где 1=Monday)
    public int[] WorkingDays { get; set; } = { 1, 2, 3, 4, 5 };
}

/// Временной диапазон
public class TimeRange
{
    public string Start { get; set; } = "09:00";
    public string End { get; set; } = "18:00";
}

/// Базовая производительность нагрузки
public class BaselinePerformance
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