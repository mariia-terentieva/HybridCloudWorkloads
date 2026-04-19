using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Pricing;
using Amazon.Pricing.Model;
using Amazon.Runtime;
using HybridCloudWorkloads.Core.Entities;
using HybridCloudWorkloads.Core.Interfaces;
using Microsoft.Extensions.Logging;

// Алиасы для разрешения конфликтов имен
using CloudInstanceType = HybridCloudWorkloads.Core.Entities.InstanceType;
using PricingFilter = Amazon.Pricing.Model.Filter;
using AwsInstanceType = Amazon.EC2.InstanceType;

namespace HybridCloudWorkloads.Infrastructure.Services.CloudProviders;

public class AwsProviderSync : ICloudProviderSync
{
    private readonly ILogger<AwsProviderSync> _logger;
    private IAmazonEC2? _ec2Client;
    private IAmazonPricing? _pricingClient;

    public string ProviderCode => "aws";
    public string ProviderName => "Amazon Web Services";

    public AwsProviderSync(ILogger<AwsProviderSync> logger)
    {
        _logger = logger;
    }

    private void InitializeClients(string credentials)
    {
        try
        {
            var creds = ParseAwsCredentials(credentials);
            _ec2Client = new AmazonEC2Client(creds, Amazon.RegionEndpoint.USEast1);
            _pricingClient = new AmazonPricingClient(creds, Amazon.RegionEndpoint.USEast1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AWS clients");
            throw;
        }
    }

    private AWSCredentials ParseAwsCredentials(string credentials)
    {
        try
        {
            var credsDict = JsonSerializer.Deserialize<Dictionary<string, string>>(credentials);
            
            if (credsDict != null && 
                credsDict.TryGetValue("accessKeyId", out var accessKey) &&
                credsDict.TryGetValue("secretAccessKey", out var secretKey))
            {
                return new BasicAWSCredentials(accessKey, secretKey);
            }
        }
        catch { }
        
        return FallbackCredentialsFactory.GetCredentials();
    }

    public async Task<bool> IsApiAvailableAsync()
    {
        try
        {
            using var client = new AmazonEC2Client(Amazon.RegionEndpoint.USEast1);
            var response = await client.DescribeRegionsAsync(new DescribeRegionsRequest());
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AWS API is not available");
            return false;
        }
    }

    public async Task<bool> ValidateCredentialsAsync(string credentials)
    {
        try
        {
            InitializeClients(credentials);
            var response = await _ec2Client!.DescribeRegionsAsync(new DescribeRegionsRequest());
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AWS credentials validation failed");
            return false;
        }
    }

    public async Task<List<CloudRegion>> SyncRegionsAsync(Guid providerId)
    {
        var regions = new List<CloudRegion>();
        
        try
        {
            var response = await _ec2Client!.DescribeRegionsAsync(new DescribeRegionsRequest
            {
                AllRegions = true
            });

            foreach (var region in response.Regions)
            {
                var cloudRegion = new CloudRegion
                {
                    Id = Guid.NewGuid(),
                    ProviderId = providerId,
                    Code = region.RegionName,
                    Name = region.RegionName,
                    DisplayName = GetRegionDisplayName(region.RegionName),
                    Continent = GetContinent(region.RegionName),
                    Status = region.OptInStatus == "opted-in" ? RegionStatus.Available : RegionStatus.Limited,
                    AvailabilityZones = await GetAvailabilityZoneCountAsync(region.RegionName),
                    Compliance = JsonSerializer.Serialize(GetComplianceForRegion(region.RegionName)),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                regions.Add(cloudRegion);
            }

            _logger.LogInformation("Synced {Count} AWS regions", regions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync AWS regions");
            throw;
        }

        return regions;
    }

    private async Task<int> GetAvailabilityZoneCountAsync(string regionName)
    {
        try
        {
            using var regionalClient = new AmazonEC2Client(Amazon.RegionEndpoint.GetBySystemName(regionName));
            var response = await regionalClient.DescribeAvailabilityZonesAsync(new DescribeAvailabilityZonesRequest());
            return response.AvailabilityZones.Count;
        }
        catch
        {
            return 3;
        }
    }

    public Task<List<CloudService>> SyncServicesAsync(Guid providerId)
    {
        var services = new List<CloudService>
        {
            new CloudService
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Code = "ec2",
                Name = "Amazon EC2",
                ServiceType = ServiceTypes.Compute,
                Description = "Elastic Compute Cloud - виртуальные серверы в облаке",
                DocumentationUrl = "https://aws.amazon.com/ec2/",
                PricingModel = JsonSerializer.Serialize(new { models = new[] { "OnDemand", "Spot", "Reserved", "SavingsPlan" } }),
                FreeTier = JsonSerializer.Serialize(new { available = true, hours = 750, instanceType = "t2.micro" }),
                SlaInfo = JsonSerializer.Serialize(new { availability = 99.99, creditPercent = 10 }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CloudService
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Code = "s3",
                Name = "Amazon S3",
                ServiceType = ServiceTypes.Storage,
                Description = "Simple Storage Service - объектное хранилище",
                DocumentationUrl = "https://aws.amazon.com/s3/",
                PricingModel = JsonSerializer.Serialize(new { models = new[] { "Standard", "IntelligentTiering", "Glacier" } }),
                SlaInfo = JsonSerializer.Serialize(new { availability = 99.99 }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CloudService
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Code = "rds",
                Name = "Amazon RDS",
                ServiceType = ServiceTypes.Database,
                Description = "Relational Database Service - управляемые реляционные БД",
                DocumentationUrl = "https://aws.amazon.com/rds/",
                SlaInfo = JsonSerializer.Serialize(new { availability = 99.95 }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CloudService
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Code = "lambda",
                Name = "AWS Lambda",
                ServiceType = ServiceTypes.Serverless,
                Description = "Serverless вычисления",
                DocumentationUrl = "https://aws.amazon.com/lambda/",
                FreeTier = JsonSerializer.Serialize(new { available = true, requests = 1_000_000, computeSeconds = 400_000 }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _logger.LogInformation("Synced {Count} AWS services", services.Count);
        return Task.FromResult(services);
    }

public async Task<List<CloudInstanceType>> SyncInstanceTypesAsync(Guid providerId, Guid regionId, string regionCode)
{
    var instanceTypes = new List<CloudInstanceType>();
    
    try
    {
        using var regionalClient = new AmazonEC2Client(Amazon.RegionEndpoint.GetBySystemName(regionCode));
        
        var request = new DescribeInstanceTypesRequest();
        DescribeInstanceTypesResponse response;
        
        do
        {
            response = await regionalClient.DescribeInstanceTypesAsync(request);
            
            foreach (var instanceType in response.InstanceTypes)
            {
                var category = GetInstanceCategory(instanceType.InstanceType);
                var family = GetInstanceFamily(instanceType.InstanceType);
                
                // Безопасное получение значений
                int vcpuCount = instanceType.VCpuInfo?.DefaultVCpus ?? 0;
                
                string cpuArchitecture = "x86_64";
                if (instanceType.ProcessorInfo?.SupportedArchitectures != null && 
                    instanceType.ProcessorInfo.SupportedArchitectures.Count > 0)
                {
                    cpuArchitecture = instanceType.ProcessorInfo.SupportedArchitectures[0];
                }
                
                // === ИСПРАВЛЕНО: Безопасное получение SustainedClockSpeedInGhz ===
                string? cpuModel = null;
                try
                {
                    if (instanceType.ProcessorInfo != null)
                    {
                        var clockSpeed = instanceType.ProcessorInfo.SustainedClockSpeedInGhz;
                        if (clockSpeed > 0)
                        {
                            cpuModel = clockSpeed.ToString();
                        }
                    }
                }
                catch
                {
                    // Свойство может отсутствовать в текущей версии SDK
                    cpuModel = null;
                }
                
                double memoryGb = (instanceType.MemoryInfo?.SizeInMiB ?? 0) / 1024.0;
                
                string? networkPerformance = instanceType.NetworkInfo?.NetworkPerformance;
                double networkBandwidthGbps = ParseNetworkBandwidth(networkPerformance);
                
                bool ebsOptimized = instanceType.EbsInfo?.EbsOptimizedSupport == "supported";
                double? maxEbsBandwidthMbps = instanceType.EbsInfo?.EbsOptimizedInfo?.BaselineBandwidthInMbps;
                int? maxIops = instanceType.EbsInfo?.EbsOptimizedInfo?.BaselineIops;
                
                bool hasGpu = (instanceType.GpuInfo?.Gpus?.Count ?? 0) > 0;
                string? gpuModel = instanceType.GpuInfo?.Gpus?.FirstOrDefault()?.Name;
                int? gpuCount = instanceType.GpuInfo?.Gpus?.Count;
                
                int? gpuMemoryGb = (instanceType.GpuInfo?.TotalGpuMemoryInMiB ?? 0) > 0 
                    ? (instanceType.GpuInfo.TotalGpuMemoryInMiB / 1024) 
                    : null;
                
                string virtualizationType = !string.IsNullOrEmpty(instanceType.Hypervisor) 
                    ? (instanceType.Hypervisor == "nitro" ? "HVM" : instanceType.Hypervisor) 
                    : "HVM";
                
                bool enhancedNetworking = instanceType.NetworkInfo?.EnaSupport == "required";
                bool placementGroupSupported = (instanceType.PlacementGroupInfo?.SupportedStrategies?.Count ?? 0) > 0;
                bool dedicatedHostSupported = instanceType.SupportedUsageClasses?.Contains("on-demand") ?? false;
                
                var cloudInstanceType = new CloudInstanceType
                {
                    Id = Guid.NewGuid(),
                    ProviderId = providerId,
                    RegionId = regionId,
                    TypeCode = instanceType.InstanceType,
                    DisplayName = instanceType.InstanceType,
                    Category = category,
                    Family = family,
                    VcpuCount = vcpuCount,
                    CpuArchitecture = cpuArchitecture,
                    CpuModel = cpuModel,
                    MemoryGb = memoryGb,
                    NetworkBandwidthGbps = networkBandwidthGbps,
                    NetworkPerformance = networkPerformance,
                    StorageType = instanceType.InstanceStorageSupported == true ? "Instance Store" : "EBS",
                    LocalStorageGb = instanceType.InstanceStorageInfo?.TotalSizeInGB,
                    LocalStorageDisks = instanceType.InstanceStorageInfo?.Disks?.Count,
                    EbsOptimized = ebsOptimized,
                    MaxEbsBandwidthMbps = maxEbsBandwidthMbps,
                    MaxIops = maxIops,
                    HasGpu = hasGpu,
                    GpuModel = gpuModel,
                    GpuCount = gpuCount,
                    GpuMemoryGb = gpuMemoryGb,
                    VirtualizationType = virtualizationType,
                    EnhancedNetworking = enhancedNetworking,
                    PlacementGroupSupported = placementGroupSupported,
                    DedicatedHostSupported = dedicatedHostSupported,
                    PhysicalProcessor = null,
                    Availability = instanceType.CurrentGeneration == true 
                        ? InstanceAvailability.Available 
                        : InstanceAvailability.Deprecated,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                instanceTypes.Add(cloudInstanceType);
            }
            
            request.NextToken = response.NextToken;
        } 
        while (!string.IsNullOrEmpty(response.NextToken));

        _logger.LogInformation("Synced {Count} AWS instance types for region {Region}", 
            instanceTypes.Count, regionCode);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to sync AWS instance types for region {Region}", regionCode);
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
            var onDemandRequest = new GetProductsRequest
            {
                ServiceCode = "AmazonEC2",
                Filters = new List<PricingFilter>
                {
                    new PricingFilter { Type = "TERM_MATCH", Field = "instanceType", Value = instanceTypeCode },
                    new PricingFilter { Type = "TERM_MATCH", Field = "location", Value = GetLocationFromRegion(regionCode) },
                    new PricingFilter { Type = "TERM_MATCH", Field = "operatingSystem", Value = "Linux" },
                    new PricingFilter { Type = "TERM_MATCH", Field = "tenancy", Value = "Shared" },
                    new PricingFilter { Type = "TERM_MATCH", Field = "capacitystatus", Value = "Used" }
                }
            };

            if (_pricingClient != null)
            {
                var onDemandResponse = await _pricingClient.GetProductsAsync(onDemandRequest);
                
                if (onDemandResponse.PriceList.Count > 0)
                {
                    var priceJson = onDemandResponse.PriceList[0];
                    var priceData = JsonSerializer.Deserialize<JsonElement>(priceJson);
                    
                    if (priceData.TryGetProperty("terms", out var terms) &&
                        terms.TryGetProperty("OnDemand", out var onDemandTerms))
                    {
                        foreach (var term in onDemandTerms.EnumerateObject())
                        {
                            if (term.Value.TryGetProperty("priceDimensions", out var dimensions))
                            {
                                foreach (var dim in dimensions.EnumerateObject())
                                {
                                    if (dim.Value.TryGetProperty("pricePerUnit", out var pricePerUnit) &&
                                        pricePerUnit.TryGetProperty("USD", out var usdPrice))
                                    {
                                        pricing.OnDemandHourly = decimal.Parse(usdPrice.GetString() ?? "0");
                                        pricing.OnDemandMonthly = pricing.OnDemandHourly * 730;
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }

            var spotPrice = await GetAverageSpotPriceAsync(instanceTypeCode, regionCode);
            pricing.SpotCurrentPrice = spotPrice.current;
            pricing.SpotAveragePrice = spotPrice.average;
            pricing.SpotMinPrice = spotPrice.min;
            pricing.SpotMaxPrice = spotPrice.max;
            pricing.SpotSavingsPercent = pricing.OnDemandHourly > 0 
                ? (1 - pricing.SpotCurrentPrice / pricing.OnDemandHourly) * 100 
                : 0;
            pricing.SpotInterruptionRate = GetSpotInterruptionRate(instanceTypeCode);

            pricing.Reserved1YearAllUpfront = pricing.OnDemandMonthly * 12 * 0.6m;
            pricing.Reserved1YearSavingsPercent = 40;
            pricing.Reserved3YearAllUpfront = pricing.OnDemandMonthly * 36 * 0.4m;
            pricing.Reserved3YearSavingsPercent = 60;

            pricing.StorageGbMonthly = 0.10m;
            pricing.DataTransferOutGb = 0.09m;
            pricing.DataTransferInGb = 0;
            pricing.DataTransferInterRegionGb = 0.02m;

            _logger.LogDebug("Synced pricing for {InstanceType} in {Region}", instanceTypeCode, regionCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync pricing for {InstanceType}", instanceTypeCode);
        }

        return pricing;
    }

    private async Task<(decimal current, decimal average, decimal min, decimal max)> GetAverageSpotPriceAsync(
        string instanceTypeCode, string regionCode)
    {
        try
        {
            using var regionalClient = new AmazonEC2Client(Amazon.RegionEndpoint.GetBySystemName(regionCode));
            
            var request = new DescribeSpotPriceHistoryRequest
            {
                InstanceTypes = new List<string> { instanceTypeCode },
                ProductDescriptions = new List<string> { "Linux/UNIX" },
                StartTimeUtc = DateTime.UtcNow.AddDays(-30),
                EndTimeUtc = DateTime.UtcNow
            };

            var response = await regionalClient.DescribeSpotPriceHistoryAsync(request);
            
            if (response.SpotPriceHistory.Count > 0)
            {
                var prices = response.SpotPriceHistory
                    .Select(p => decimal.Parse(p.Price))
                    .ToList();
                
                return (
                    current: prices.FirstOrDefault(),
                    average: prices.Average(),
                    min: prices.Min(),
                    max: prices.Max()
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get spot prices for {InstanceType}", instanceTypeCode);
        }
        
        return (0, 0, 0, 0);
    }

    private double GetSpotInterruptionRate(string instanceTypeCode)
    {
        var category = GetInstanceCategory(instanceTypeCode);
        return category switch
        {
            "Burstable" => 0.15,
            "General Purpose" => 0.10,
            "Compute Optimized" => 0.08,
            "Memory Optimized" => 0.05,
            "Accelerated Computing" => 0.20,
            _ => 0.10
        };
    }

    public async Task<List<SpotPriceHistory>> SyncSpotPriceHistoryAsync(
        string instanceTypeCode, 
        string regionCode, 
        DateTime startDate, 
        DateTime endDate)
    {
        var history = new List<SpotPriceHistory>();
        
        try
        {
            using var regionalClient = new AmazonEC2Client(Amazon.RegionEndpoint.GetBySystemName(regionCode));
            
            var request = new DescribeSpotPriceHistoryRequest
            {
                InstanceTypes = new List<string> { instanceTypeCode },
                ProductDescriptions = new List<string> { "Linux/UNIX" },
                StartTimeUtc = startDate,
                EndTimeUtc = endDate
            };

            var response = await regionalClient.DescribeSpotPriceHistoryAsync(request);
            
            foreach (var price in response.SpotPriceHistory)
            {
                history.Add(new SpotPriceHistory
                {
                    Timestamp = price.Timestamp,
                    Price = decimal.Parse(price.Price),
                    AvailabilityZone = price.AvailabilityZone,
                    InstanceType = price.InstanceType
                });
            }
            
            _logger.LogInformation("Synced {Count} spot price history entries for {InstanceType}", 
                history.Count, instanceTypeCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync spot price history for {InstanceType}", instanceTypeCode);
        }

        return history;
    }

    public Task<List<Discount>> SyncDiscountsAsync(Guid providerId)
    {
        var discounts = new List<Discount>
        {
            new Discount
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Name = "AWS Free Tier",
                Description = "Бесплатное использование в течение 12 месяцев",
                DiscountType = DiscountTypes.Promotional,
                DiscountPercent = 100,
                AppliesTo = "t2.micro, t3.micro",
                ValidFrom = DateTime.UtcNow,
                ValidUntil = DateTime.UtcNow.AddYears(1),
                Status = DiscountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Discount
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Name = "Savings Plan 1 Year",
                Description = "Скидка при обязательстве на 1 год",
                DiscountType = DiscountTypes.CommitmentDiscount,
                DiscountPercent = 30,
                AppliesTo = "All EC2, Fargate, Lambda",
                Conditions = JsonSerializer.Serialize(new { commitment = "1 year", payment = "All upfront" }),
                Status = DiscountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Discount
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Name = "Savings Plan 3 Year",
                Description = "Скидка при обязательстве на 3 года",
                DiscountType = DiscountTypes.CommitmentDiscount,
                DiscountPercent = 50,
                AppliesTo = "All EC2, Fargate, Lambda",
                Conditions = JsonSerializer.Serialize(new { commitment = "3 years", payment = "All upfront" }),
                Status = DiscountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Discount
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Name = "Reserved Instances 1 Year",
                Description = "Скидка на резервирование инстансов на 1 год",
                DiscountType = DiscountTypes.CommitmentDiscount,
                DiscountPercent = 40,
                AppliesTo = "EC2, RDS, Redshift, ElastiCache",
                Status = DiscountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Discount
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                Name = "Reserved Instances 3 Year",
                Description = "Скидка на резервирование инстансов на 3 года",
                DiscountType = DiscountTypes.CommitmentDiscount,
                DiscountPercent = 60,
                AppliesTo = "EC2, RDS, Redshift, ElastiCache",
                Status = DiscountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        return Task.FromResult(discounts);
    }

    #region Helper Methods

    private string GetRegionDisplayName(string regionCode)
    {
        var displayNames = new Dictionary<string, string>
        {
            ["us-east-1"] = "Северная Вирджиния",
            ["us-east-2"] = "Огайо",
            ["us-west-1"] = "Северная Калифорния",
            ["us-west-2"] = "Орегон",
            ["eu-west-1"] = "Ирландия",
            ["eu-west-2"] = "Лондон",
            ["eu-west-3"] = "Париж",
            ["eu-central-1"] = "Франкфурт",
            ["eu-north-1"] = "Стокгольм",
            ["ap-southeast-1"] = "Сингапур",
            ["ap-southeast-2"] = "Сидней",
            ["ap-northeast-1"] = "Токио",
            ["ap-northeast-2"] = "Сеул",
            ["ap-south-1"] = "Мумбаи",
            ["sa-east-1"] = "Сан-Паулу"
        };
        
        return displayNames.GetValueOrDefault(regionCode, regionCode);
    }

    private string GetContinent(string regionCode)
    {
        var prefix = regionCode.Split('-')[0];
        return prefix switch
        {
            "us" or "ca" => "North America",
            "sa" => "South America",
            "eu" => "Europe",
            "ap" => "Asia",
            "af" => "Africa",
            "me" => "Middle East",
            _ => "Unknown"
        };
    }

    private string[] GetComplianceForRegion(string regionCode)
    {
        var baseCompliance = new[] { "SOC", "PCI DSS" };
        
        var additional = regionCode switch
        {
            var r when r.StartsWith("us") => new[] { "HIPAA", "FedRAMP" },
            var r when r.StartsWith("eu") => new[] { "GDPR" },
            _ => Array.Empty<string>()
        };
        
        return baseCompliance.Concat(additional).ToArray();
    }

    private string GetInstanceCategory(string instanceType)
    {
        if (instanceType.StartsWith("t")) return "Burstable";
        if (instanceType.StartsWith("m")) return "General Purpose";
        if (instanceType.StartsWith("c")) return "Compute Optimized";
        if (instanceType.StartsWith("r") || instanceType.StartsWith("x") || instanceType.StartsWith("z")) 
            return "Memory Optimized";
        if (instanceType.StartsWith("i") || instanceType.StartsWith("d") || instanceType.StartsWith("h")) 
            return "Storage Optimized";
        if (instanceType.StartsWith("p") || instanceType.StartsWith("g") || instanceType.StartsWith("inf")) 
            return "Accelerated Computing";
        
        return "General Purpose";
    }

    private string GetInstanceFamily(string instanceType)
    {
        var match = System.Text.RegularExpressions.Regex.Match(instanceType, @"^([a-z]+)\d");
        return match.Success ? match.Groups[1].Value : instanceType;
    }

    private double ParseNetworkBandwidth(string? networkPerformance)
    {
        if (string.IsNullOrEmpty(networkPerformance)) return 5;
        
        if (networkPerformance.Contains("Up to 10")) return 10;
        if (networkPerformance.Contains("Up to 25")) return 25;
        if (networkPerformance.Contains("Up to 50")) return 50;
        if (networkPerformance.Contains("Up to 100")) return 100;
        
        return 5;
    }

    private string GetLocationFromRegion(string regionCode)
    {
        var locations = new Dictionary<string, string>
        {
            ["us-east-1"] = "US East (N. Virginia)",
            ["us-east-2"] = "US East (Ohio)",
            ["us-west-1"] = "US West (N. California)",
            ["us-west-2"] = "US West (Oregon)",
            ["eu-west-1"] = "EU (Ireland)",
            ["eu-west-2"] = "EU (London)",
            ["eu-central-1"] = "EU (Frankfurt)",
            ["ap-southeast-1"] = "Asia Pacific (Singapore)",
            ["ap-northeast-1"] = "Asia Pacific (Tokyo)"
        };
        
        return locations.GetValueOrDefault(regionCode, regionCode);
    }

    #endregion
}