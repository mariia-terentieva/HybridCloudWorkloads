using HybridCloudWorkloads.API.Models;
using HybridCloudWorkloads.Core.Entities;
using HybridCloudWorkloads.Infrastructure.Data;
using HybridCloudWorkloads.Infrastructure.Entities;
using HybridCloudWorkloads.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace HybridCloudWorkloads.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DeploymentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly DockerService _dockerService;
    private readonly ILogger<DeploymentController> _logger;

    public DeploymentController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        DockerService dockerService,
        ILogger<DeploymentController> logger)
    {
        _context = context;
        _userManager = userManager;
        _dockerService = dockerService;
        _logger = logger;
    }

[HttpPost("deploy/{workloadId}")]
public async Task<ActionResult<DeploymentResponse>> DeployWorkload(Guid workloadId)
{
    var userId = GetCurrentUserId();
    var workload = await _context.Workloads
        .FirstOrDefaultAsync(w => w.Id == workloadId && w.UserId == userId);

    if (workload == null)
        return NotFound("Workload not found");

    if (string.IsNullOrEmpty(workload.ContainerImage))
        return BadRequest("Container image is required for deployment");

    try
    {
        // Обновляем статус
        workload.DeploymentStatus = "Deploying";
        workload.DeployedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // ВАЖНО: Если уже есть контейнер, удаляем его перед созданием нового
        if (!string.IsNullOrEmpty(workload.ContainerId))
        {
            try
            {
                _logger.LogInformation("Removing old container {ContainerId} before redeploy", 
                    workload.ContainerId);
                await _dockerService.ForceRemoveContainerAsync(workload.ContainerId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove old container {ContainerId}, trying force remove", 
                    workload.ContainerId);
                // Пробуем принудительное удаление
                await _dockerService.ForceRemoveContainerByName(workload.Id);
            }
            
            // Сбрасываем ContainerId для создания нового
            workload.ContainerId = null;
        }

        // Деплоим контейнер и получаем реальный ContainerId
        var (accessUrl, containerId) = await DeployContainerAndGetId(workload);
        
        // ВАЖНО: Сохраняем ContainerId
        workload.ContainerId = containerId;
        workload.AccessUrl = accessUrl;
        workload.DeploymentStatus = "Running";
        await _context.SaveChangesAsync();

        return Ok(new DeploymentResponse
        {
            Success = true,
            Message = "Workload deployed successfully",
            AccessUrl = accessUrl,
            ContainerId = containerId,
            DeployedAt = workload.DeployedAt
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to deploy workload {WorkloadId}", workloadId);
        
        workload.DeploymentStatus = "Error";
        await _context.SaveChangesAsync();

        return StatusCode(500, new DeploymentResponse
        {
            Success = false,
            Message = $"Deployment failed: {ex.Message}"
        });
    }
}

private async Task<(string AccessUrl, string ContainerId)> DeployContainerAndGetId(Workload workload)
{
    try
    {
        var containerName = $"workload-{workload.Id.ToString().Substring(0, 8)}";
        var hostPort = GetAvailablePort();
        
        // ПОДГОТОВКА ПЕРЕМЕННЫХ ОКРУЖЕНИЯ
        var envVars = new StringBuilder();
        
        if (!string.IsNullOrEmpty(workload.EnvironmentVariables))
        {
            try
            {
                var envDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
                    workload.EnvironmentVariables);
                
                if (envDict != null)
                {
                    foreach (var kvp in envDict)
                    {
                        if (!string.IsNullOrEmpty(kvp.Key) && !string.IsNullOrEmpty(kvp.Value))
                        {
                            envVars.Append($" -e {kvp.Key}=\"{kvp.Value}\"");
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse environment variables for workload {WorkloadId}", 
                    workload.Id);
            }
        }
        
        // Запускаем контейнер через Process
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"run -d --name {containerName} -p {hostPort}:{workload.ExposedPort} " +
                           $"{envVars} " + // ДОБАВЛЯЕМ ПЕРЕМЕННЫЕ ОКРУЖЕНИЯ
                           $"{workload.ContainerImage ?? "nginx:alpine"}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _logger.LogInformation("Starting Docker container with command: docker {Args}", 
            process.StartInfo.Arguments);

        process.Start();
        var containerId = (await process.StandardOutput.ReadToEndAsync()).Trim();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new ApplicationException($"Docker failed: {error}");
        }

        _logger.LogInformation("Container started with ID: {ContainerId}", containerId);
        
        return ($"http://localhost:{hostPort}", containerId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to deploy container");
        throw;
    }
}

private int GetAvailablePort()
{
    var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
    listener.Start();
    var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}

    [HttpGet("status/{workloadId}")]
    public async Task<ActionResult<ContainerStatus>> GetDeploymentStatus(Guid workloadId)
    {
        var userId = GetCurrentUserId();
        var workload = await _context.Workloads
            .FirstOrDefaultAsync(w => w.Id == workloadId && w.UserId == userId);

        if (workload == null || string.IsNullOrEmpty(workload.ContainerId))
            return NotFound();

        try
        {
            var status = await _dockerService.GetContainerStatusAsync(workload.ContainerId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get status for container {ContainerId}", workload.ContainerId);
            return StatusCode(500, $"Failed to get status: {ex.Message}");
        }
    }

[HttpPost("stop/{workloadId}")]
public async Task<IActionResult> StopDeployment(Guid workloadId)
{
    var userId = GetCurrentUserId();
    var workload = await _context.Workloads
        .FirstOrDefaultAsync(w => w.Id == workloadId && w.UserId == userId);

    if (workload == null || string.IsNullOrEmpty(workload.ContainerId))
        return NotFound();

    try
    {
        await _dockerService.StopContainerAsync(workload.ContainerId);
        workload.DeploymentStatus = "Stopped";
        workload.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Workload stopped successfully" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to stop container {ContainerId}", workload.ContainerId);
        return StatusCode(500, $"Failed to stop: {ex.Message}");
    }
}

    [HttpDelete("remove/{workloadId}")]
    public async Task<IActionResult> RemoveDeployment(Guid workloadId)
    {
        var userId = GetCurrentUserId();
        var workload = await _context.Workloads
            .FirstOrDefaultAsync(w => w.Id == workloadId && w.UserId == userId);

        if (workload == null || string.IsNullOrEmpty(workload.ContainerId))
            return NotFound();

        try
        {
            await _dockerService.RemoveContainerAsync(workload.ContainerId);
            
            // Сбрасываем информацию о деплое
            workload.ContainerId = null;
            workload.AccessUrl = null;
            workload.DeploymentStatus = "NotDeployed";
            workload.DeployedAt = null;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Workload removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove container {ContainerId}", workload.ContainerId);
            return StatusCode(500, $"Failed to remove: {ex.Message}");
        }
    }

    [HttpGet("my-deployments")]
    public async Task<ActionResult<IEnumerable<WorkloadDto>>> GetMyDeployments()
    {
        var userId = GetCurrentUserId();
        var workloads = await _context.Workloads
            .Where(w => w.UserId == userId && 
                   (w.DeploymentStatus == "Running" || 
                    w.DeploymentStatus == "Deploying" || 
                    w.DeploymentStatus == "Stopped" || 
                    w.DeploymentStatus == "Error"))
            .Select(w => new WorkloadDto
            {
            Id = w.Id,
            Name = w.Name,
            Description = w.Description,
            Type = w.Type.ToString(),
            RequiredCpu = w.RequiredCpu,
            RequiredMemory = w.RequiredMemory,
            RequiredStorage = w.RequiredStorage,
            ContainerImage = w.ContainerImage,
            AccessUrl = w.AccessUrl,
            DeploymentStatus = w.DeploymentStatus,
            DeployedAt = w.DeployedAt,
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt
            })
            .ToListAsync();

        return Ok(workloads);
    }

    [HttpGet("containers")]
    public async Task<ActionResult<IList<string>>> GetMyContainers()
    {
        var userId = GetCurrentUserId();
        var containers = await _dockerService.GetUserContainersAsync(userId);
        return Ok(containers);
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(_userManager.GetUserId(User)!);
    }
}

public class DeploymentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AccessUrl { get; set; }
    public string? ContainerId { get; set; }
    public DateTime? DeployedAt { get; set; }
}