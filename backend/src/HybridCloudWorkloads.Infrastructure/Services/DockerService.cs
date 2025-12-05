using System.Diagnostics;
using System.Text.Json;
using HybridCloudWorkloads.Core.Entities;
using Microsoft.Extensions.Logging;

namespace HybridCloudWorkloads.Infrastructure.Services;

public class DockerService : IDisposable
{
    private readonly ILogger<DockerService> _logger;

    public DockerService(ILogger<DockerService> logger)
    {
        _logger = logger;
    }

    public async Task<(string AccessUrl, string ContainerId)> DeployContainerAsync(Workload workload)
    {
        try
        {
            _logger.LogInformation("Deploying container for workload {WorkloadId}", workload.Id);

            var containerName = $"workload-{workload.Id.ToString().Substring(0, 8)}";
            var hostPort = GetAvailablePort();
            
            // Запускаем контейнер и получаем его ID
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"run -d --name {containerName} -p {hostPort}:{workload.ExposedPort} " +
                               $"--memory {workload.RequiredMemory * 1024}m --cpus {workload.RequiredCpu} " +
                               $"{workload.ContainerImage ?? "nginx:alpine"}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var containerId = (await process.StandardOutput.ReadToEndAsync()).Trim();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new ApplicationException($"Docker failed: {error}");
            }

            _logger.LogInformation("Container {ContainerId} deployed on port {Port}", 
                containerId, hostPort);

            return ($"http://localhost:{hostPort}", containerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy container for workload {WorkloadId}", workload.Id);
            throw new ApplicationException($"Failed to deploy container: {ex.Message}", ex);
        }
    }

    public async Task<ContainerStatus> GetContainerStatusAsync(string containerId)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"inspect {containerId}",
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

            if (process.ExitCode != 0)
            {
                return new ContainerStatus
                {
                    Id = containerId,
                    State = "NotFound",
                    Status = "Container not found or removed"
                };
            }

            // Парсим JSON вывод
            using var doc = JsonDocument.Parse(output);
            var container = doc.RootElement[0];

            return new ContainerStatus
            {
                Id = containerId,
                Name = container.GetProperty("Name").GetString()?.TrimStart('/') ?? "unknown",
                State = container.GetProperty("State").GetProperty("Status").GetString() ?? "unknown",
                Status = container.GetProperty("State").GetProperty("Status").GetString() ?? "unknown",
                Created = DateTime.Parse(container.GetProperty("Created").GetString() ?? DateTime.UtcNow.ToString()),
                StartedAt = container.GetProperty("State").TryGetProperty("StartedAt", out var startedAt) 
                    ? DateTime.Parse(startedAt.GetString() ?? "") 
                    : null,
                FinishedAt = container.GetProperty("State").TryGetProperty("FinishedAt", out var finishedAt) 
                    ? DateTime.Parse(finishedAt.GetString() ?? "") 
                    : null,
                ExitCode = container.GetProperty("State").TryGetProperty("ExitCode", out var exitCode) 
                    ? exitCode.GetInt64() 
                    : null,
                Error = container.GetProperty("State").TryGetProperty("Error", out var errorProp) 
                    ? errorProp.GetString() 
                    : null,
                Ports = GetPortsFromInspect(container)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get container status for {ContainerId}", containerId);
            return new ContainerStatus
            {
                Id = containerId,
                State = "Error",
                Status = $"Failed to get status: {ex.Message}"
            };
        }
    }

    private List<string> GetPortsFromInspect(JsonElement container)
    {
        var ports = new List<string>();
        
        if (container.TryGetProperty("NetworkSettings", out var networkSettings) &&
            networkSettings.TryGetProperty("Ports", out var portsElement))
        {
            foreach (var port in portsElement.EnumerateObject())
            {
                if (port.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var binding in port.Value.EnumerateArray())
                    {
                        if (binding.TryGetProperty("HostPort", out var hostPort))
                        {
                            ports.Add($"{hostPort.GetString()}");
                        }
                    }
                }
            }
        }
        
        return ports;
    }

   public async Task StopContainerAsync(string containerId)
    {
        if (string.IsNullOrEmpty(containerId))
        {
            throw new ArgumentException("Container ID cannot be null or empty", nameof(containerId));
        }

        try
        {
            _logger.LogInformation("Stopping container: {ContainerId}", containerId);
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"stop {containerId}",
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

            if (process.ExitCode != 0)
            {
                _logger.LogError("Failed to stop container {ContainerId}: {Error}", containerId, error);
                throw new ApplicationException($"Failed to stop container: {error}");
            }

            _logger.LogInformation("Container {ContainerId} stopped successfully", containerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while stopping container {ContainerId}", containerId);
            throw;
        }
    }

    public async Task RemoveContainerAsync(string containerId)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"rm -f {containerId}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            
            _logger.LogInformation("Container {ContainerId} removed", containerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove container {ContainerId}", containerId);
            throw new ApplicationException($"Failed to remove container: {ex.Message}", ex);
        }
    }

    public async Task ForceRemoveContainerAsync(string containerId)
{
    try
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"rm -f {containerId}",
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

        if (process.ExitCode != 0)
        {
            _logger.LogWarning("Failed to force remove container {ContainerId}: {Error}", 
                containerId, error);
        }
        else
        {
            _logger.LogInformation("Container {ContainerId} force removed", containerId);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Exception while force removing container {ContainerId}", 
            containerId);
    }
}

public async Task ForceRemoveContainerByName(Guid workloadId)
{
    try
    {
        var containerName = $"workload-{workloadId.ToString().Substring(0, 8)}";
        _logger.LogInformation("Force removing container by name: {ContainerName}", containerName);
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"rm -f {containerName}",
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

        if (process.ExitCode != 0)
        {
            _logger.LogWarning("Failed to force remove container by name {ContainerName}: {Error}", 
                containerName, error);
        }
        else
        {
            _logger.LogInformation("Container {ContainerName} force removed by name", containerName);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Exception while force removing container by name for workload {WorkloadId}", 
            workloadId);
        // Не пробрасываем исключение
    }
}
    public Task<IList<string>> GetUserContainersAsync(Guid userId)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"ps -a --filter name=workload-{userId.ToString().Substring(0, 8)} --format \"{{{{.ID}}}}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var containers = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => id.Trim())
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

            return Task.FromResult<IList<string>>(containers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list containers for user {UserId}", userId);
            return Task.FromResult<IList<string>>(new List<string>());
        }
    }

    private int GetAvailablePort()
    {
        try
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get available port, using random");
            var random = new Random();
            return random.Next(10000, 60000);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

public class ContainerStatus
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public long? ExitCode { get; set; }
    public string? Error { get; set; }
    public List<string> Ports { get; set; } = new();
}