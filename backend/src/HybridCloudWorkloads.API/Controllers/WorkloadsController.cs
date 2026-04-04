/* using HybridCloudWorkloads.API.Models;
using HybridCloudWorkloads.Core.Entities;
using HybridCloudWorkloads.Infrastructure.Data;
using HybridCloudWorkloads.Infrastructure.Entities;
using HybridCloudWorkloads.Infrastructure.Services; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HybridCloudWorkloads.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorkloadsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
        private readonly DockerService _dockerService;
    private readonly ILogger<WorkloadsController> _logger; 

    public WorkloadsController(ApplicationDbContext context,
        UserManager<User> userManager,
        DockerService dockerService,
        ILogger<WorkloadsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _dockerService = dockerService;
        _logger = logger; 
    }

[HttpGet]
public async Task<ActionResult<IEnumerable<WorkloadDto>>> GetWorkloads([FromQuery] string? search = null)
{
    var userId = GetCurrentUserId();
    var query = _context.Workloads
        .Where(w => w.UserId == userId);

    if (!string.IsNullOrEmpty(search))
    {
        query = query.Where(w => w.Name.Contains(search));
    }

    var workloads = await query
        .OrderByDescending(w => w.UpdatedAt)
        .Select(w => new WorkloadDto
        {
            Id = w.Id,
            Name = w.Name,
            Description = w.Description,
            Type = w.Type.ToString(),
            RequiredCpu = w.RequiredCpu,
            RequiredMemory = w.RequiredMemory,
            RequiredStorage = w.RequiredStorage,
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt,
            
            // Существующие поля деплоя
            ContainerImage = w.ContainerImage,
            ExposedPort = w.ExposedPort,
            EnvironmentVariables = w.EnvironmentVariables,
            DeploymentStatus = w.DeploymentStatus,
            ContainerId = w.ContainerId,
            AccessUrl = w.AccessUrl,
            DeployedAt = w.DeployedAt,
            
            // НОВЫЕ ПОЛЯ
            UsagePattern = w.UsagePattern.ToString(),
            Criticality = w.Criticality.ToString(),
            BudgetTier = w.BudgetTier.ToString(),
            SlaRequirements = w.SlaRequirements != null 
                ? JsonSerializer.Deserialize<SlaRequirementDto>(w.SlaRequirements) 
                : null,
            BusinessHours = w.BusinessHours != null 
                ? JsonSerializer.Deserialize<BusinessHoursDto>(w.BusinessHours) 
                : null,
            Tags = w.Tags,
            LastProfiledAt = w.LastProfiledAt,
            BaselinePerformance = w.BaselinePerformance != null 
                ? JsonSerializer.Deserialize<BaselinePerformanceDto>(w.BaselinePerformance) 
                : null
        })
        .ToListAsync();

    return Ok(workloads);
}

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkloadDto>> GetWorkload(Guid id)
    {
        var userId = GetCurrentUserId();
        var workload = await _context.Workloads
            .Where(w => w.Id == id && w.UserId == userId)
            .Select(w => new WorkloadDto
            {
                Id = w.Id,
                Name = w.Name,
                Description = w.Description,
                Type = w.Type.ToString(),
                RequiredCpu = w.RequiredCpu,
                RequiredMemory = w.RequiredMemory,
                RequiredStorage = w.RequiredStorage,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt,
                //
                ContainerImage = w.ContainerImage,
                ExposedPort = w.ExposedPort,
                EnvironmentVariables = w.EnvironmentVariables,
                DeploymentStatus = w.DeploymentStatus,
                ContainerId = w.ContainerId,
                AccessUrl = w.AccessUrl,
                DeployedAt = w.DeployedAt,

                // НОВЫЕ ПОЛЯ
                UsagePattern = w.UsagePattern.ToString(),
                Criticality = w.Criticality.ToString(),
                BudgetTier = w.BudgetTier.ToString(),
                SlaRequirements = w.SlaRequirements != null 
                    ? JsonSerializer.Deserialize<SlaRequirementDto>(w.SlaRequirements) 
                    : null,
                BusinessHours = w.BusinessHours != null 
                    ? JsonSerializer.Deserialize<BusinessHoursDto>(w.BusinessHours) 
                    : null,
                Tags = w.Tags,
                LastProfiledAt = w.LastProfiledAt,
                BaselinePerformance = w.BaselinePerformance != null 
                    ? JsonSerializer.Deserialize<BaselinePerformanceDto>(w.BaselinePerformance) 
                    : null
            })
            .FirstOrDefaultAsync();

        if (workload == null)
        {
            return NotFound();
        }

        return workload;
    }


    [HttpPost]
public async Task<ActionResult<WorkloadDto>> CreateWorkload([FromBody] CreateWorkloadRequest request)
{
    if (!Enum.TryParse<WorkloadType>(request.Type, out var workloadType))
    {
        return BadRequest("Invalid workload type");
    }

    if (!Enum.TryParse<UsagePattern>(request.UsagePattern, out var usagePattern))
    {
        usagePattern = UsagePattern.Constant;
    }

    if (!Enum.TryParse<CriticalityClass>(request.Criticality, out var criticality))
    {
        criticality = CriticalityClass.NonCritical;
    }

    if (!Enum.TryParse<BudgetTier>(request.BudgetTier, out var budgetTier))
    {
        budgetTier = BudgetTier.Medium;
    }

    var userId = GetCurrentUserId();
    var workload = new Workload
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        Description = request.Description,
        Type = workloadType,
        RequiredCpu = request.RequiredCpu,
        RequiredMemory = request.RequiredMemory,
        RequiredStorage = request.RequiredStorage,
        UserId = userId,
        
        // Существующие поля деплоя
        ContainerImage = request.ContainerImage,
        ExposedPort = request.ExposedPort,
        EnvironmentVariables = request.EnvironmentVariables,
        DeploymentStatus = "NotDeployed",
        
        // НОВЫЕ ПОЛЯ
        UsagePattern = usagePattern,
        Criticality = criticality,
        BudgetTier = budgetTier,
        SlaRequirements = request.SlaRequirements != null 
            ? JsonSerializer.Serialize(request.SlaRequirements) 
            : null,
        BusinessHours = request.BusinessHours != null 
            ? JsonSerializer.Serialize(request.BusinessHours) 
            : null,
        Tags = request.Tags,
        LastProfiledAt = null,
        BaselinePerformance = null
    };

    _context.Workloads.Add(workload);
    await _context.SaveChangesAsync();

    // Создаем DTO для ответа
    var workloadDto = new WorkloadDto
    {
        Id = workload.Id,
        Name = workload.Name,
        Description = workload.Description,
        Type = workload.Type.ToString(),
        RequiredCpu = workload.RequiredCpu,
        RequiredMemory = workload.RequiredMemory,
        RequiredStorage = workload.RequiredStorage,
        CreatedAt = workload.CreatedAt,
        UpdatedAt = workload.UpdatedAt,
        
        // Существующие поля деплоя
        ContainerImage = workload.ContainerImage,
        ExposedPort = workload.ExposedPort,
        EnvironmentVariables = workload.EnvironmentVariables,
        DeploymentStatus = workload.DeploymentStatus,
        ContainerId = workload.ContainerId,
        AccessUrl = workload.AccessUrl,
        DeployedAt = workload.DeployedAt,
        
        // НОВЫЕ ПОЛЯ
        UsagePattern = workload.UsagePattern.ToString(),
        Criticality = workload.Criticality.ToString(),
        BudgetTier = workload.BudgetTier.ToString(),
        SlaRequirements = request.SlaRequirements,
        BusinessHours = request.BusinessHours,
        Tags = workload.Tags,
        LastProfiledAt = workload.LastProfiledAt
    };

    return CreatedAtAction(nameof(GetWorkload), new { id = workload.Id }, workloadDto);
}

    [HttpPut("{id}")]
public async Task<IActionResult> UpdateWorkload(Guid id, [FromBody] UpdateWorkloadRequest request)
{
    if (!Enum.TryParse<WorkloadType>(request.Type, out var workloadType))
    {
        return BadRequest("Invalid workload type");
    }

    if (!Enum.TryParse<UsagePattern>(request.UsagePattern, out var usagePattern))
    {
        usagePattern = UsagePattern.Constant;
    }

    if (!Enum.TryParse<CriticalityClass>(request.Criticality, out var criticality))
    {
        criticality = CriticalityClass.NonCritical;
    }

    if (!Enum.TryParse<BudgetTier>(request.BudgetTier, out var budgetTier))
    {
        budgetTier = BudgetTier.Medium;
    }

    var userId = GetCurrentUserId();
    var workload = await _context.Workloads
        .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

    if (workload == null)
    {
        return NotFound();
    }

    workload.Name = request.Name;
    workload.Description = request.Description;
    workload.Type = workloadType;
    workload.RequiredCpu = request.RequiredCpu;
    workload.RequiredMemory = request.RequiredMemory;
    workload.RequiredStorage = request.RequiredStorage;
    
    // Существующие поля деплоя
    workload.ContainerImage = request.ContainerImage;
    workload.ExposedPort = request.ExposedPort;
    workload.EnvironmentVariables = request.EnvironmentVariables;
    
    // НОВЫЕ ПОЛЯ
    workload.UsagePattern = usagePattern;
    workload.Criticality = criticality;
    workload.BudgetTier = budgetTier;
    workload.SlaRequirements = request.SlaRequirements != null 
        ? JsonSerializer.Serialize(request.SlaRequirements) 
        : null;
    workload.BusinessHours = request.BusinessHours != null 
        ? JsonSerializer.Serialize(request.BusinessHours) 
        : null;
    workload.Tags = request.Tags;

    await _context.SaveChangesAsync();

    return NoContent();
}

[HttpDelete("{id}")]
public async Task<IActionResult> DeleteWorkload(Guid id)
{
    var userId = GetCurrentUserId();
    var workload = await _context.Workloads
        .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

    if (workload == null)
    {
        return NotFound();
    }

    try
    {
        if (!string.IsNullOrEmpty(workload.ContainerId))
        {
            try
            {
                // Сначала останавливаем (если запущен)
                await _dockerService.StopContainerAsync(workload.ContainerId);
                // Затем удаляем
                await _dockerService.ForceRemoveContainerAsync(workload.ContainerId);
            }
            catch (Exception dockerEx)
            {
                _logger.LogWarning(dockerEx, 
                    "Container cleanup failed for {ContainerId} during workload deletion", 
                    workload.ContainerId);
                // Продолжаем удаление из БД
            }
        }

        // Удаляем из БД
        _context.Workloads.Remove(workload);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to delete workload {WorkloadId}", id);
        return StatusCode(500, $"Failed to delete workload: {ex.Message}");
    }
}

/// <summary>
/// Получить расширенный профиль workload для модуля оптимизации
/// </summary>
[HttpGet("profile/{id}/for-optimization")]
public async Task<ActionResult<WorkloadProfileForOptimization>> GetWorkloadProfileForOptimization(Guid id)
{
    var userId = GetCurrentUserId();
    var workload = await _context.Workloads
        .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

    if (workload == null)
    {
        return NotFound();
    }

    var profile = new WorkloadProfileForOptimization
    {
        Id = workload.Id,
        Name = workload.Name,
        RequiredCpu = workload.RequiredCpu,
        RequiredMemory = workload.RequiredMemory,
        RequiredStorage = workload.RequiredStorage,
        UsagePattern = workload.UsagePattern.ToString(),
        Criticality = workload.Criticality.ToString(),
        BudgetTier = workload.BudgetTier.ToString(),
        SlaRequirements = workload.SlaRequirements != null 
            ? JsonSerializer.Deserialize<SlaRequirementDto>(workload.SlaRequirements) 
            : null,
        BusinessHours = workload.BusinessHours != null 
            ? JsonSerializer.Deserialize<BusinessHoursDto>(workload.BusinessHours) 
            : null,
        Tags = workload.Tags,
        LastProfiledAt = workload.LastProfiledAt,
        BaselinePerformance = workload.BaselinePerformance != null 
            ? JsonSerializer.Deserialize<BaselinePerformanceDto>(workload.BaselinePerformance) 
            : null
    };

    return Ok(profile);
}

/// Получить профили нескольких workloads для оптимизации (batch)
[HttpPost("profiles/batch")]
public async Task<ActionResult<List<WorkloadProfileForOptimization>>> GetBatchProfiles([FromBody] Guid[] ids)
{
    var userId = GetCurrentUserId();
    var workloads = await _context.Workloads
        .Where(w => w.UserId == userId && ids.Contains(w.Id))
        .ToListAsync();

    var profiles = workloads.Select(w => new WorkloadProfileForOptimization
    {
        Id = w.Id,
        Name = w.Name,
        RequiredCpu = w.RequiredCpu,
        RequiredMemory = w.RequiredMemory,
        RequiredStorage = w.RequiredStorage,
        UsagePattern = w.UsagePattern.ToString(),
        Criticality = w.Criticality.ToString(),
        BudgetTier = w.BudgetTier.ToString(),
        SlaRequirements = w.SlaRequirements != null 
            ? JsonSerializer.Deserialize<SlaRequirementDto>(w.SlaRequirements) 
            : null,
        BusinessHours = w.BusinessHours != null 
            ? JsonSerializer.Deserialize<BusinessHoursDto>(w.BusinessHours) 
            : null,
        Tags = w.Tags,
        LastProfiledAt = w.LastProfiledAt,
        BaselinePerformance = w.BaselinePerformance != null 
            ? JsonSerializer.Deserialize<BaselinePerformanceDto>(w.BaselinePerformance) 
            : null
    }).ToList();

    return Ok(profiles);
}

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(_userManager.GetUserId(User)!);
    }
} */

using System.Text.Json;
using HybridCloudWorkloads.API.Models;
using HybridCloudWorkloads.Core.Entities;
using HybridCloudWorkloads.Infrastructure.Data;
using HybridCloudWorkloads.Infrastructure.Entities;
using HybridCloudWorkloads.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HybridCloudWorkloads.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorkloadsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly DockerService _dockerService;
    private readonly ILogger<WorkloadsController> _logger;

    public WorkloadsController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        DockerService dockerService,
        ILogger<WorkloadsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _dockerService = dockerService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkloadDto>>> GetWorkloads([FromQuery] string? search = null)
    {
        var userId = GetCurrentUserId();
        var query = _context.Workloads
            .Where(w => w.UserId == userId);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(w => w.Name.Contains(search));
        }

        var workloads = await query
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync(); // Сначала получаем данные из БД

        // Затем преобразуем в DTO на стороне клиента (в памяти)
        var result = workloads.Select(w => MapToDto(w)).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkloadDto>> GetWorkload(Guid id)
    {
        var userId = GetCurrentUserId();
        var workload = await _context.Workloads
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (workload == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(workload));
    }

    [HttpPost]
    public async Task<ActionResult<WorkloadDto>> CreateWorkload([FromBody] CreateWorkloadRequest request)
    {
        if (!Enum.TryParse<WorkloadType>(request.Type, out var workloadType))
        {
            return BadRequest("Invalid workload type");
        }

        if (!Enum.TryParse<UsagePattern>(request.UsagePattern, out var usagePattern))
        {
            usagePattern = UsagePattern.Constant;
        }

        if (!Enum.TryParse<CriticalityClass>(request.Criticality, out var criticality))
        {
            criticality = CriticalityClass.NonCritical;
        }

        if (!Enum.TryParse<BudgetTier>(request.BudgetTier, out var budgetTier))
        {
            budgetTier = BudgetTier.Medium;
        }

        var userId = GetCurrentUserId();
        var workload = new Workload
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Type = workloadType,
            RequiredCpu = request.RequiredCpu,
            RequiredMemory = request.RequiredMemory,
            RequiredStorage = request.RequiredStorage,
            UserId = userId,

            // Существующие поля деплоя
            ContainerImage = request.ContainerImage,
            ExposedPort = request.ExposedPort,
            EnvironmentVariables = request.EnvironmentVariables,
            DeploymentStatus = "NotDeployed",

            // НОВЫЕ ПОЛЯ
            UsagePattern = usagePattern,
            Criticality = criticality,
            BudgetTier = budgetTier,
            SlaRequirements = request.SlaRequirements != null
                ? JsonSerializer.Serialize(request.SlaRequirements)
                : null,
            BusinessHours = request.BusinessHours != null
                ? JsonSerializer.Serialize(request.BusinessHours)
                : null,
            Tags = request.Tags,
            LastProfiledAt = null,
            BaselinePerformance = null
        };

        _context.Workloads.Add(workload);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWorkload), new { id = workload.Id }, MapToDto(workload));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWorkload(Guid id, [FromBody] UpdateWorkloadRequest request)
    {
        if (!Enum.TryParse<WorkloadType>(request.Type, out var workloadType))
        {
            return BadRequest("Invalid workload type");
        }

        if (!Enum.TryParse<UsagePattern>(request.UsagePattern, out var usagePattern))
        {
            usagePattern = UsagePattern.Constant;
        }

        if (!Enum.TryParse<CriticalityClass>(request.Criticality, out var criticality))
        {
            criticality = CriticalityClass.NonCritical;
        }

        if (!Enum.TryParse<BudgetTier>(request.BudgetTier, out var budgetTier))
        {
            budgetTier = BudgetTier.Medium;
        }

        var userId = GetCurrentUserId();
        var workload = await _context.Workloads
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (workload == null)
        {
            return NotFound();
        }

        workload.Name = request.Name;
        workload.Description = request.Description;
        workload.Type = workloadType;
        workload.RequiredCpu = request.RequiredCpu;
        workload.RequiredMemory = request.RequiredMemory;
        workload.RequiredStorage = request.RequiredStorage;

        // Существующие поля деплоя
        workload.ContainerImage = request.ContainerImage;
        workload.ExposedPort = request.ExposedPort;
        workload.EnvironmentVariables = request.EnvironmentVariables;

        // НОВЫЕ ПОЛЯ
        workload.UsagePattern = usagePattern;
        workload.Criticality = criticality;
        workload.BudgetTier = budgetTier;
        workload.SlaRequirements = request.SlaRequirements != null
            ? JsonSerializer.Serialize(request.SlaRequirements)
            : null;
        workload.BusinessHours = request.BusinessHours != null
            ? JsonSerializer.Serialize(request.BusinessHours)
            : null;
        workload.Tags = request.Tags;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkload(Guid id)
    {
        var userId = GetCurrentUserId();
        var workload = await _context.Workloads
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (workload == null)
        {
            return NotFound();
        }

        try
        {
            if (!string.IsNullOrEmpty(workload.ContainerId))
            {
                try
                {
                    // Сначала останавливаем (если запущен)
                    await _dockerService.StopContainerAsync(workload.ContainerId);
                    // Затем удаляем
                    await _dockerService.ForceRemoveContainerAsync(workload.ContainerId);
                }
                catch (Exception dockerEx)
                {
                    _logger.LogWarning(dockerEx,
                        "Container cleanup failed for {ContainerId} during workload deletion",
                        workload.ContainerId);
                    // Продолжаем удаление из БД
                }
            }

            // Удаляем из БД
            _context.Workloads.Remove(workload);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete workload {WorkloadId}", id);
            return StatusCode(500, $"Failed to delete workload: {ex.Message}");
        }
    }

    /// <summary>
    /// Получить расширенный профиль workload для модуля оптимизации
    /// </summary>
    [HttpGet("profile/{id}/for-optimization")]
    public async Task<ActionResult<WorkloadProfileForOptimization>> GetWorkloadProfileForOptimization(Guid id)
    {
        var userId = GetCurrentUserId();
        var workload = await _context.Workloads
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (workload == null)
        {
            return NotFound();
        }

        return Ok(MapToOptimizationProfile(workload));
    }

    /// <summary>
    /// Получить профили нескольких workloads для оптимизации (batch)
    /// </summary>
    [HttpPost("profiles/batch")]
    public async Task<ActionResult<List<WorkloadProfileForOptimization>>> GetBatchProfiles([FromBody] Guid[] ids)
    {
        var userId = GetCurrentUserId();
        var workloads = await _context.Workloads
            .Where(w => w.UserId == userId && ids.Contains(w.Id))
            .ToListAsync();

        var profiles = workloads.Select(w => MapToOptimizationProfile(w)).ToList();
        return Ok(profiles);
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(_userManager.GetUserId(User)!);
    }

    // Вспомогательный метод для маппинга Workload -> WorkloadDto
    private WorkloadDto MapToDto(Workload w)
    {
        return new WorkloadDto
        {
            Id = w.Id,
            Name = w.Name,
            Description = w.Description,
            Type = w.Type.ToString(),
            RequiredCpu = w.RequiredCpu,
            RequiredMemory = w.RequiredMemory,
            RequiredStorage = w.RequiredStorage,
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt,

            // Существующие поля деплоя
            ContainerImage = w.ContainerImage,
            ExposedPort = w.ExposedPort,
            EnvironmentVariables = w.EnvironmentVariables,
            DeploymentStatus = w.DeploymentStatus,
            ContainerId = w.ContainerId,
            AccessUrl = w.AccessUrl,
            DeployedAt = w.DeployedAt,

            // НОВЫЕ ПОЛЯ
            UsagePattern = w.UsagePattern.ToString(),
            Criticality = w.Criticality.ToString(),
            BudgetTier = w.BudgetTier.ToString(),
            SlaRequirements = w.SlaRequirements != null
                ? JsonSerializer.Deserialize<SlaRequirementDto>(w.SlaRequirements)
                : null,
            BusinessHours = w.BusinessHours != null
                ? JsonSerializer.Deserialize<BusinessHoursDto>(w.BusinessHours)
                : null,
            Tags = w.Tags,
            LastProfiledAt = w.LastProfiledAt,
            BaselinePerformance = w.BaselinePerformance != null
                ? JsonSerializer.Deserialize<BaselinePerformanceDto>(w.BaselinePerformance)
                : null
        };
    }

    // Вспомогательный метод для маппинга Workload -> WorkloadProfileForOptimization
    private WorkloadProfileForOptimization MapToOptimizationProfile(Workload w)
    {
        return new WorkloadProfileForOptimization
        {
            Id = w.Id,
            Name = w.Name,
            RequiredCpu = w.RequiredCpu,
            RequiredMemory = w.RequiredMemory,
            RequiredStorage = w.RequiredStorage,
            UsagePattern = w.UsagePattern.ToString(),
            Criticality = w.Criticality.ToString(),
            BudgetTier = w.BudgetTier.ToString(),
            SlaRequirements = w.SlaRequirements != null
                ? JsonSerializer.Deserialize<SlaRequirementDto>(w.SlaRequirements)
                : null,
            BusinessHours = w.BusinessHours != null
                ? JsonSerializer.Deserialize<BusinessHoursDto>(w.BusinessHours)
                : null,
            Tags = w.Tags,
            LastProfiledAt = w.LastProfiledAt,
            BaselinePerformance = w.BaselinePerformance != null
                ? JsonSerializer.Deserialize<BaselinePerformanceDto>(w.BaselinePerformance)
                : null
        };
    }
}