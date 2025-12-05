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
                //
                ContainerImage = w.ContainerImage,
                ExposedPort = w.ExposedPort,
                EnvironmentVariables = w.EnvironmentVariables,
                DeploymentStatus = w.DeploymentStatus,
                ContainerId = w.ContainerId,
                AccessUrl = w.AccessUrl,
                DeployedAt = w.DeployedAt
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
                DeployedAt = w.DeployedAt
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
            //
            ContainerImage = request.ContainerImage,
            ExposedPort = request.ExposedPort,
            EnvironmentVariables = request.EnvironmentVariables,
            DeploymentStatus = "NotDeployed"
        };

        _context.Workloads.Add(workload);
        await _context.SaveChangesAsync();

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
            //
            ContainerImage = workload.ContainerImage,
            ExposedPort = workload.ExposedPort,
            EnvironmentVariables = workload.EnvironmentVariables,
            DeploymentStatus = workload.DeploymentStatus,
            ContainerId = workload.ContainerId,
            AccessUrl = workload.AccessUrl,
            DeployedAt = workload.DeployedAt
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
        //
        workload.ContainerImage = request.ContainerImage;
        workload.ExposedPort = request.ExposedPort;
        workload.EnvironmentVariables = request.EnvironmentVariables;

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

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(_userManager.GetUserId(User)!);
    }
}