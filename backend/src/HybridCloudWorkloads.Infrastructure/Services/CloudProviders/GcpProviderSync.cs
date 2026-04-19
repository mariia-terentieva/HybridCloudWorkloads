using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Cloud.Compute.V1;
using HybridCloudWorkloads.Core.Entities;
using HybridCloudWorkloads.Core.Interfaces;
using Microsoft.Extensions.Logging;
using CloudInstanceType = HybridCloudWorkloads.Core.Entities.InstanceType;

namespace HybridCloudWorkloads.Infrastructure.Services.CloudProviders;

/// <summary>
/// Синхронизация с Google Cloud Platform API
/// </summary>
public class GcpProviderSync : ICloudProviderSync
{
    private readonly ILogger<GcpProviderSync> _logger;
    private MachineTypesClient? _machineTypesClient;
    private RegionsClient? _regionsClient;
    private ZonesClient? _zonesClient;
    private string? _projectId;

    public string ProviderCode => "gcp";
    public string ProviderName => "Google Cloud Platform";

    public GcpProviderSync(ILogger<GcpProviderSync> logger)
    {
        _logger = logger;
    }

    private void InitializeClients(string credentials)
    {
        try
        {
            var credsDict = JsonSerializer.Deserialize<Dictionary<string, string>>(credentials);
            _projectId = credsDict?.GetValueOrDefault("projectId") ?? "default-project";
            
            // GCP использует переменные окружения для аутентификации
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", 
                credsDict?.GetValueOrDefault("credentialsPath"));
            
            _machineTypesClient = MachineTypesClient.Create();
            _regionsClient = RegionsClient.Create();
            _zonesClient = ZonesClient.Create();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize GCP clients");
            throw;
        }
    }

    public async Task<bool> IsApiAvailableAsync()
    {
        try
        {
            var client = RegionsClient.Create();
            await client.ListAsync("gcp-public-data").GetAsyncEnumerator().MoveNextAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GCP API is not available");
            return false;
        }
    }

    public async Task<bool> ValidateCredentialsAsync(string credentials)
    {
        try
        {
            InitializeClients(credentials);
            await _regionsClient!.ListAsync(_projectId).GetAsyncEnumerator().MoveNextAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GCP credentials validation failed");
            return false;
        }
    }

    public async Task<List<CloudRegion>> SyncRegionsAsync(Guid providerId)
    {
        var regions = new List<CloudRegion>();
        
        try
        {
            var gcpRegions = new[]
            {
                new { Code = "us-central1", Name = "Iowa", Display = "Айова", Continent = "North America", Country = "USA", Zones = 4 },
                new { Code = "us-east1", Name = "South Carolina", Display = "Южная Каролина", Continent = "North America", Country = "USA", Zones = 3 },
                new { Code = "us-east4", Name = "Northern Virginia", Display = "Северная Вирджиния", Continent = "North America", Country = "USA", Zones = 3 },
                new { Code = "us-west1", Name = "Oregon", Display = "Орегон", Continent = "North America", Country = "USA", Zones = 3 },
                new { Code = "us-west2", Name = "Los Angeles", Display = "Лос-Анджелес", Continent = "North America", Country = "USA", Zones = 3 },
                new { Code = "us-west3", Name = "Salt Lake City", Display = "Солт-Лейк-Сити", Continent = "North America", Country = "USA", Zones = 3 },
                new { Code = "us-west4", Name = "Las Vegas", Display = "Лас-Вегас", Continent = "North America", Country = "USA", Zones = 3 },
                
                new { Code = "europe-west1", Name = "Belgium", Display = "Бельгия", Continent = "Europe", Country = "Belgium", Zones = 3 },
                new { Code = "europe-west2", Name = "London", Display = "Лондон", Continent = "Europe", Country = "UK", Zones = 3 },
                new { Code = "europe-west3", Name = "Frankfurt", Display = "Франкфурт", Continent = "Europe", Country = "Germany", Zones = 3 },
                new { Code = "europe-west4", Name = "Netherlands", Display = "Нидерланды", Continent = "Europe", Country = "Netherlands", Zones = 3 },
                new { Code = "europe-west6", Name = "Zurich", Display = "Цюрих", Continent = "Europe", Country = "Switzerland", Zones = 3 },
                new { Code = "europe-north1", Name = "Finland", Display = "Финляндия", Continent = "Europe", Country = "Finland", Zones = 3 },
                new { Code = "europe-central2", Name = "Warsaw", Display = "Варшава", Continent = "Europe", Country = "Poland", Zones = 3 },
                
                new { Code = "asia-east1", Name = "Taiwan", Display = "Тайвань", Continent = "Asia", Country = "Taiwan", Zones = 3 },
                new { Code = "asia-east2", Name = "Hong Kong", Display = "Гонконг", Continent = "Asia", Country = "Hong Kong", Zones = 3 },
                new { Code = "asia-northeast1", Name = "Tokyo", Display = "Токио", Continent = "Asia", Country = "Japan", Zones = 3 },
                new { Code = "asia-northeast2", Name = "Osaka", Display = "Осака", Continent = "Asia", Country = "Japan", Zones = 3 },
                new { Code = "asia-northeast3", Name = "Seoul", Display = "Сеул", Continent = "Asia", Country = "South Korea", Zones = 3 },
                new { Code = "asia-southeast1", Name = "Singapore", Display = "Сингапур", Continent = "Asia", Country = "Singapore", Zones = 3 },
                new { Code = "asia-southeast2", Name = "Jakarta", Display = "Джакарта", Continent = "Asia", Country = "Indonesia", Zones = 3 },
                new { Code = "asia-south1", Name = "Mumbai", Display = "Мумбаи", Continent = "Asia", Country = "India", Zones = 3 },
                new { Code = "asia-south2", Name = "Delhi", Display = "Дели", Continent = "Asia", Country = "India", Zones = 3 },
                
                new { Code = "australia-southeast1", Name = "Sydney", Display = "Сидней", Continent = "Oceania", Country = "Australia", Zones = 3 },
                new { Code = "australia-southeast2", Name = "Melbourne", Display = "Мельбурн", Continent = "Oceania", Country = "Australia", Zones = 3 },
                
                new { Code = "southamerica-east1", Name = "Sao Paulo", Display = "Сан-Паулу", Continent = "South America", Country = "Brazil", Zones = 3 },
                new { Code = "southamerica-west1", Name = "Santiago", Display = "Сантьяго", Continent = "South America", Country = "Chile", Zones = 3 },
                
                new { Code = "northamerica-northeast1", Name = "Montreal", Display = "Монреаль", Continent = "North America", Country = "Canada", Zones = 3 },
                new { Code = "northamerica-northeast2", Name = "Toronto", Display = "Торонто", Continent = "North America", Country = "Canada", Zones = 3 },
                
                new { Code = "me-west1", Name = "Tel Aviv", Display = "Тель-Авив", Continent = "Middle East", Country = "Israel", Zones = 3 },
                new { Code = "me-central1", Name = "Doha", Display = "Доха", Continent = "Middle East", Country = "Qatar", Zones = 3 },
                new { Code = "me-central2", Name = "Dammam", Display = "Даммам", Continent = "Middle East", Country = "Saudi Arabia", Zones = 3 },
                
                new { Code = "africa-south1", Name = "Johannesburg", Display = "Йоханнесбург", Continent = "Africa", Country = "South Africa", Zones = 3 }
            };

            foreach (var region in gcpRegions)
            {
                var cloudRegion = new CloudRegion
                {
                    Id = Guid.NewGuid(),
                    ProviderId = providerId,
                    Code = region.Code,
                    Name = region.Name,
                    DisplayName = region.Display,
                    Continent = region.Continent,
                    Country = region.Country,
                    Status = RegionStatus.Available,
                    AvailabilityZones = region.Zones,
                    Compliance = JsonSerializer.Serialize(GetComplianceForRegion(region.Code)),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                regions.Add(cloudRegion);
            }

            _logger.LogInformation("Synced {Count} GCP regions", regions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync GCP regions");
            throw;
        }

        return regions;
    }

    public async Task<List<CloudService>> SyncServicesAsync(Guid providerId)
    {
        var services = new List<CloudService>
        {
            new CloudService
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Code = "compute-engine",
                Name = "Compute Engine",
                ServiceType = ServiceTypes.Compute,
                Description = "Виртуальные машины Google Cloud",
                DocumentationUrl = "https://cloud.google.com/compute",
                PricingModel = JsonSerializer.Serialize(new { models = new[] { "OnDemand", "Spot", "CommittedUse" } }),
                FreeTier = JsonSerializer.Serialize(new { available = true, instanceType = "e2-micro" }),
                SlaInfo = JsonSerializer.Serialize(new { availability = 99.99, creditPercent = 10 }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CloudService
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Code = "cloud-storage",
                Name = "Cloud Storage",
                ServiceType = ServiceTypes.Storage,
                Description = "Объектное хранилище",
                DocumentationUrl = "https://cloud.google.com/storage",
                FreeTier = JsonSerializer.Serialize(new { available = true, storage = "5 GB", operations = "50000" }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CloudService
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Code = "cloud-sql",
                Name = "Cloud SQL",
                ServiceType = ServiceTypes.Database,
                Description = "Управляемые реляционные базы данных",
                DocumentationUrl = "https://cloud.google.com/sql",
                SlaInfo = JsonSerializer.Serialize(new { availability = 99.95 }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CloudService
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Code = "cloud-functions",
                Name = "Cloud Functions",
                ServiceType = ServiceTypes.Serverless,
                Description = "Serverless вычисления",
                DocumentationUrl = "https://cloud.google.com/functions",
                FreeTier = JsonSerializer.Serialize(new { available = true, invocations = 2_000_000, computeTime = 400_000 }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CloudService
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Code = "kubernetes-engine",
                Name = "Google Kubernetes Engine (GKE)",
                ServiceType = ServiceTypes.Container,
                Description = "Управляемый Kubernetes",
                DocumentationUrl = "https://cloud.google.com/kubernetes-engine",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _logger.LogInformation("Synced {Count} GCP services", services.Count);
        return services;
    }

    public async Task<List<CloudInstanceType>> SyncInstanceTypesAsync(Guid providerId, Guid regionId, string regionCode)
    {
        var instanceTypes = new List<InstanceType>();
        
        try
        {
            // GCP Machine Types - статический список
var gcpMachineTypes = new (string Code, string Category, string Family, double VCPU, double RAM)[]
{
    ("e2-micro", "General Purpose", "e2", 0.25, 1),
    ("e2-small", "General Purpose", "e2", 0.5, 2),
    ("e2-medium", "General Purpose", "e2", 1, 4),
    ("e2-standard-2", "General Purpose", "e2", 2, 8),
    ("e2-standard-4", "General Purpose", "e2", 4, 16),
    ("e2-standard-8", "General Purpose", "e2", 8, 32),
    ("e2-standard-16", "General Purpose", "e2", 16, 64),
    ("e2-standard-32", "General Purpose", "e2", 32, 128),
    ("e2-highmem-2", "Memory Optimized", "e2", 2, 16),
    ("e2-highmem-4", "Memory Optimized", "e2", 4, 32),
    ("e2-highmem-8", "Memory Optimized", "e2", 8, 64),
    ("e2-highmem-16", "Memory Optimized", "e2", 16, 128),
    ("e2-highcpu-2", "Compute Optimized", "e2", 2, 2),
    ("e2-highcpu-4", "Compute Optimized", "e2", 4, 4),
    ("e2-highcpu-8", "Compute Optimized", "e2", 8, 8),
    ("e2-highcpu-16", "Compute Optimized", "e2", 16, 16),
    ("e2-highcpu-32", "Compute Optimized", "e2", 32, 32),
    ("n2-standard-2", "General Purpose", "n2", 2, 8),
    ("n2-standard-4", "General Purpose", "n2", 4, 16),
    ("n2-standard-8", "General Purpose", "n2", 8, 32),
    ("n2-standard-16", "General Purpose", "n2", 16, 64),
    ("n2-standard-32", "General Purpose", "n2", 32, 128),
    ("n2-standard-48", "General Purpose", "n2", 48, 192),
    ("n2-standard-64", "General Purpose", "n2", 64, 256),
    ("n2-standard-80", "General Purpose", "n2", 80, 320),
    ("n2-standard-96", "General Purpose", "n2", 96, 384),
    ("n2-standard-128", "General Purpose", "n2", 128, 512),
    ("n2-highmem-2", "Memory Optimized", "n2", 2, 16),
    ("n2-highmem-4", "Memory Optimized", "n2", 4, 32),
    ("n2-highmem-8", "Memory Optimized", "n2", 8, 64),
    ("n2-highmem-16", "Memory Optimized", "n2", 16, 128),
    ("n2-highmem-32", "Memory Optimized", "n2", 32, 256),
    ("n2-highmem-48", "Memory Optimized", "n2", 48, 384),
    ("n2-highmem-64", "Memory Optimized", "n2", 64, 512),
    ("n2-highmem-80", "Memory Optimized", "n2", 80, 640),
    ("n2-highmem-96", "Memory Optimized", "n2", 96, 768),
    ("n2-highmem-128", "Memory Optimized", "n2", 128, 864),
    ("n2-highcpu-2", "Compute Optimized", "n2", 2, 2),
    ("n2-highcpu-4", "Compute Optimized", "n2", 4, 4),
    ("n2-highcpu-8", "Compute Optimized", "n2", 8, 8),
    ("n2-highcpu-16", "Compute Optimized", "n2", 16, 16),
    ("n2-highcpu-32", "Compute Optimized", "n2", 32, 32),
    ("n2-highcpu-48", "Compute Optimized", "n2", 48, 48),
    ("n2-highcpu-64", "Compute Optimized", "n2", 64, 64),
    ("n2-highcpu-80", "Compute Optimized", "n2", 80, 80),
    ("n2-highcpu-96", "Compute Optimized", "n2", 96, 96),
    ("c2-standard-4", "Compute Optimized", "c2", 4, 16),
    ("c2-standard-8", "Compute Optimized", "c2", 8, 32),
    ("c2-standard-16", "Compute Optimized", "c2", 16, 64),
    ("c2-standard-30", "Compute Optimized", "c2", 30, 120),
    ("c2-standard-60", "Compute Optimized", "c2", 60, 240),
    ("c3-standard-4", "Compute Optimized", "c3", 4, 16),
    ("c3-standard-8", "Compute Optimized", "c3", 8, 32),
    ("c3-standard-22", "Compute Optimized", "c3", 22, 88),
    ("c3-standard-44", "Compute Optimized", "c3", 44, 176),
    ("c3-standard-88", "Compute Optimized", "c3", 88, 352),
    ("c3-standard-176", "Compute Optimized", "c3", 176, 704),
    ("m2-megamem-416", "Memory Optimized", "m2", 416, 5888),
    ("m2-ultramem-208", "Memory Optimized", "m2", 208, 5888),
    ("m2-ultramem-416", "Memory Optimized", "m2", 416, 11776),
    ("m3-megamem-64", "Memory Optimized", "m3", 64, 976),
    ("m3-megamem-128", "Memory Optimized", "m3", 128, 1952),
    ("g2-standard-4", "Accelerated Computing", "g2", 4, 16),
    ("g2-standard-8", "Accelerated Computing", "g2", 8, 32),
    ("g2-standard-12", "Accelerated Computing", "g2", 12, 48),
    ("g2-standard-16", "Accelerated Computing", "g2", 16, 64),
    ("g2-standard-24", "Accelerated Computing", "g2", 24, 96),
    ("g2-standard-32", "Accelerated Computing", "g2", 32, 128),
    ("g2-standard-48", "Accelerated Computing", "g2", 48, 192),
    ("g2-standard-96", "Accelerated Computing", "g2", 96, 384)
};

            foreach (var machineType in gcpMachineTypes)
            {
                var hasGpu = machineType.Family == "g2";
                var networkBandwidth = machineType.VCPU switch
                {
                    <= 2 => 10.0,
                    <= 8 => 16.0,
                    <= 16 => 32.0,
                    <= 32 => 50.0,
                    _ => 100.0
                };
                
                var cloudInstanceType = new InstanceType
                {
                    Id = Guid.NewGuid(),
                    ProviderId = providerId,
                    RegionId = regionId,
                    TypeCode = machineType.Code,
                    DisplayName = machineType.Code,
                    Category = machineType.Category,
                    Family = machineType.Family,
                    VcpuCount = machineType.VCPU,
                    CpuArchitecture = "x86_64",
                    MemoryGb = machineType.RAM,
                    NetworkBandwidthGbps = networkBandwidth,
                    NetworkPerformance = $"{networkBandwidth} Gbps",
                    StorageType = "Persistent Disk",
                    HasGpu = hasGpu,
                    GpuCount = hasGpu ? 1 : null,
                    VirtualizationType = "HVM",
                    Availability = InstanceAvailability.Available,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                instanceTypes.Add(cloudInstanceType);
            }

            _logger.LogInformation("Synced {Count} GCP instance types for region {Region}", 
                instanceTypes.Count, regionCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync GCP instance types for region {Region}", regionCode);
            throw;
        }

        return instanceTypes;
    }

    public async Task<InstancePricing> SyncPricingAsync(Guid instanceTypeId, string instanceTypeCode, string regionCode)
    {
        var pricing = new InstancePricing
        {
            Id = Guid.NewGuid(),
            InstanceTypeId = instanceTypeId,
            Currency = "USD",
            EffectiveDate = DateTime.UtcNow
        };

        try
        {
            var (onDemandHourly, onDemandMonthly) = GetEstimatedGcpPrice(instanceTypeCode);
            
            pricing.OnDemandHourly = onDemandHourly;
            pricing.OnDemandMonthly = onDemandMonthly;
            
            // Spot (Preemptible) цены - обычно 60-80% скидка
            pricing.SpotCurrentPrice = onDemandHourly * 0.3m;
            pricing.SpotAveragePrice = onDemandHourly * 0.2m;
            pricing.SpotSavingsPercent = 70;
            
            // Committed Use скидки
            pricing.Reserved1YearAllUpfront = onDemandMonthly * 12 * 0.63m;
            pricing.Reserved1YearSavingsPercent = 37;
            pricing.Reserved3YearAllUpfront = onDemandMonthly * 36 * 0.43m;
            pricing.Reserved3YearSavingsPercent = 57;
            
            // Дополнительные расходы
            pricing.StorageGbMonthly = 0.10m;
            pricing.DataTransferOutGb = 0.12m;

            _logger.LogDebug("Synced pricing for {InstanceType} in {Region}", instanceTypeCode, regionCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync pricing for {InstanceType}", instanceTypeCode);
        }

        return pricing;
    }

    private (decimal hourly, decimal monthly) GetEstimatedGcpPrice(string instanceTypeCode)
    {
        if (instanceTypeCode.Contains("e2-micro")) return (0.0076m, 5.55m);
        if (instanceTypeCode.Contains("e2-small")) return (0.0152m, 11.10m);
        if (instanceTypeCode.Contains("e2-medium")) return (0.0303m, 22.12m);
        if (instanceTypeCode.Contains("e2-standard-2")) return (0.063m, 46.01m);
        if (instanceTypeCode.Contains("e2-standard-4")) return (0.126m, 92.02m);
        if (instanceTypeCode.Contains("e2-standard-8")) return (0.252m, 184.04m);
        if (instanceTypeCode.Contains("n2-standard-2")) return (0.097m, 70.81m);
        if (instanceTypeCode.Contains("n2-standard-4")) return (0.194m, 141.62m);
        if (instanceTypeCode.Contains("c2-standard-4")) return (0.209m, 152.57m);
        
        return (0.10m, 73m);
    }

    public async Task<List<SpotPriceHistory>> SyncSpotPriceHistoryAsync(
        string instanceTypeCode, 
        string regionCode, 
        DateTime startDate, 
        DateTime endDate)
    {
        // GCP не предоставляет публичную историю preemptible цен
        return new List<SpotPriceHistory>();
    }

    public async Task<List<Discount>> SyncDiscountsAsync(Guid providerId)
    {
        var discounts = new List<Discount>
        {
            new Discount
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Name = "Google Cloud Free Tier",
                Description = "Бесплатное использование Always Free продуктов",
                DiscountType = DiscountTypes.Promotional,
                DiscountPercent = 100,
                AppliesTo = "e2-micro, Cloud Storage, Cloud Functions",
                Status = DiscountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Discount
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Name = "Free Trial Credits",
                Description = "$300 кредит на 90 дней",
                DiscountType = DiscountTypes.Promotional,
                DiscountPercent = 100,
                ValidFrom = DateTime.UtcNow,
                ValidUntil = DateTime.UtcNow.AddDays(90),
                Status = DiscountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Discount
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Name = "Committed Use Discount 1 Year",
                Description = "Скидка при обязательстве на 1 год",
                DiscountType = DiscountTypes.CommitmentDiscount,
                DiscountPercent = 37,
                AppliesTo = "Compute Engine",
                Status = DiscountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Discount
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Name = "Committed Use Discount 3 Year",
                Description = "Скидка при обязательстве на 3 года",
                DiscountType = DiscountTypes.CommitmentDiscount,
                DiscountPercent = 57,
                AppliesTo = "Compute Engine",
                Status = DiscountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Discount
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Name = "Sustained Use Discount",
                Description = "Автоматическая скидка за длительное использование",
                DiscountType = DiscountTypes.VolumeDiscount,
                DiscountPercent = 30,
                AppliesTo = "Compute Engine",
                Conditions = JsonSerializer.Serialize(new { minUsage = "25% of month" }),
                Status = DiscountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Discount
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Name = "Startup Program",
                Description = "Скидки для стартапов",
                DiscountType = DiscountTypes.StartupProgram,
                DiscountPercent = 20,
                Status = DiscountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        return discounts;
    }

    #region Helper Methods

    private string[] GetComplianceForRegion(string regionCode)
    {
        var baseCompliance = new[] { "ISO 27001", "SOC", "PCI DSS" };
        
        var additional = regionCode switch
        {
            var r when r.StartsWith("us") => new[] { "HIPAA", "FedRAMP" },
            var r when r.StartsWith("europe") => new[] { "GDPR" },
            var r when r.Contains("uk") => new[] { "G-Cloud" },
            var r when r.Contains("australia") => new[] { "IRAP" },
            var r when r.Contains("canada") => new[] { "PIPEDA" },
            _ => Array.Empty<string>()
        };
        
        return baseCompliance.Concat(additional).ToArray();
    }

    #endregion
}