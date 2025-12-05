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
    
    // Новые поля для деплоя
    public string? ContainerImage { get; set; }
    public int ExposedPort { get; set; } = 80;
    public string? EnvironmentVariables { get; set; }
    public string? DeploymentStatus { get; set; }
    public string? ContainerId { get; set; }
    public string? AccessUrl { get; set; }
    public DateTime? DeployedAt { get; set; }
}

public class CreateWorkloadRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public int RequiredCpu { get; set; }
    public double RequiredMemory { get; set; }
    public double RequiredStorage { get; set; }
    
    // Новые поля для деплоя
    public string? ContainerImage { get; set; }
    public int ExposedPort { get; set; } = 80;
    public string? EnvironmentVariables { get; set; }
}

public class UpdateWorkloadRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public int RequiredCpu { get; set; }
    public double RequiredMemory { get; set; }
    public double RequiredStorage { get; set; }
    
    // Новые поля для деплоя
    public string? ContainerImage { get; set; }
    public int ExposedPort { get; set; } = 80;
    public string? EnvironmentVariables { get; set; }
}