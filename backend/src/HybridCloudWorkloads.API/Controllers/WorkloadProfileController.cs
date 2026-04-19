using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using HybridCloudWorkloads.Core.Interfaces;
using HybridCloudWorkloads.Infrastructure.Data;
using HybridCloudWorkloads.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;

namespace HybridCloudWorkloads.API.Controllers;

[Authorize]
[ApiController]
[Route("api/profile")]
public class WorkloadProfileController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkloadProfileExporter _profileExporter;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<WorkloadProfileController> _logger;

    public WorkloadProfileController(
        ApplicationDbContext context,
        IWorkloadProfileExporter profileExporter,
        UserManager<User> userManager,
        ILogger<WorkloadProfileController> logger)
    {
        _context = context;
        _profileExporter = profileExporter;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Экспорт профиля workload в JSON
    /// </summary>
    [HttpGet("{workloadId}/json")]
    public async Task<IActionResult> ExportToJson(Guid workloadId)
    {
        if (!await UserOwnsWorkloadAsync(workloadId))
            return Unauthorized();

        try
        {
            var json = await _profileExporter.ExportToJsonAsync(workloadId);
            return Content(json, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export profile to JSON for workload {WorkloadId}", workloadId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Экспорт профилей нескольких workloads в JSON
    /// </summary>
    [HttpPost("batch/json")]
    public async Task<IActionResult> ExportBatchToJson([FromBody] BatchProfileRequest request)
    {
        var ownedIds = await FilterUserWorkloadsAsync(request.WorkloadIds);
        if (ownedIds.Count == 0)
            return Unauthorized();

        try
        {
            var json = await _profileExporter.ExportBatchToJsonAsync(ownedIds);
            return Content(json, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export batch profiles to JSON");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Экспорт профиля workload в CSV
    /// </summary>
    [HttpGet("{workloadId}/csv")]
    public async Task<IActionResult> ExportToCsv(Guid workloadId)
    {
        if (!await UserOwnsWorkloadAsync(workloadId))
            return Unauthorized();

        try
        {
            var csv = await _profileExporter.ExportToCsvAsync(workloadId);
            return File(csv, "text/csv", $"workload_{workloadId}_profile.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export profile to CSV for workload {WorkloadId}", workloadId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Экспорт профилей нескольких workloads в CSV
    /// </summary>
    [HttpPost("batch/csv")]
    public async Task<IActionResult> ExportBatchToCsv([FromBody] BatchProfileRequest request)
    {
        var ownedIds = await FilterUserWorkloadsAsync(request.WorkloadIds);
        if (ownedIds.Count == 0)
            return Unauthorized();

        try
        {
            var csv = await _profileExporter.ExportBatchToCsvAsync(ownedIds);
            return File(csv, "text/csv", $"workloads_profiles_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export batch profiles to CSV");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить компактный профиль для модуля оптимизации
    /// </summary>
    [HttpGet("{workloadId}/optimization")]
    public async Task<IActionResult> GetOptimizationProfile(Guid workloadId)
    {
        if (!await UserOwnsWorkloadAsync(workloadId))
            return Unauthorized();

        try
        {
            var profile = await _profileExporter.GetOptimizationReadyProfileAsync(workloadId);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get optimization profile for workload {WorkloadId}", workloadId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить компактные профили для модуля оптимизации (batch)
    /// </summary>
    [HttpPost("batch/optimization")]
    public async Task<IActionResult> GetBatchOptimizationProfiles([FromBody] BatchProfileRequest request)
    {
        var ownedIds = await FilterUserWorkloadsAsync(request.WorkloadIds);
        if (ownedIds.Count == 0)
            return Unauthorized();

        try
        {
            var profiles = await _profileExporter.GetBatchOptimizationReadyProfilesAsync(ownedIds);
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get batch optimization profiles");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Экспорт в формате Prometheus
    /// </summary>
    [HttpGet("{workloadId}/prometheus")]
    public async Task<IActionResult> ExportToPrometheus(Guid workloadId)
    {
        if (!await UserOwnsWorkloadAsync(workloadId))
            return Unauthorized();

        try
        {
            var prometheus = await _profileExporter.ExportToPrometheusFormatAsync(workloadId);
            return Content(prometheus, "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export to Prometheus format for workload {WorkloadId}", workloadId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить все workloads пользователя в компактном формате
    /// </summary>
    [HttpGet("all/optimization")]
    public async Task<IActionResult> GetAllOptimizationProfiles()
    {
        var userId = GetCurrentUserId();
        var workloads = await _context.Workloads
            .Where(w => w.UserId == userId)
            .Select(w => w.Id)
            .ToListAsync();

        try
        {
            var profiles = await _profileExporter.GetBatchOptimizationReadyProfilesAsync(workloads);
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all optimization profiles");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    #region Private Methods

    private Guid GetCurrentUserId()
    {
        var userId = _userManager.GetUserId(User);
        return string.IsNullOrEmpty(userId) ? Guid.Empty : Guid.Parse(userId);
    }

    private async Task<bool> UserOwnsWorkloadAsync(Guid workloadId)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return false;
        
        return await _context.Workloads
            .AnyAsync(w => w.Id == workloadId && w.UserId == userId);
    }

    private async Task<List<Guid>> FilterUserWorkloadsAsync(Guid[] requestedIds)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return new List<Guid>();
        
        var ownedIds = await _context.Workloads
            .Where(w => w.UserId == userId && requestedIds.Contains(w.Id))
            .Select(w => w.Id)
            .ToListAsync();
        
        return ownedIds;
    }

    #endregion
}

public class BatchProfileRequest
{
    public Guid[] WorkloadIds { get; set; } = Array.Empty<Guid>();
}