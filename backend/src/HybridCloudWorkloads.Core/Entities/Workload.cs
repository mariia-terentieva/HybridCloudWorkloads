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
}