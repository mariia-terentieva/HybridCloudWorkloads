using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HybridCloudWorkloads.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HybridCloudWorkloads.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeCloudProvidersAsync(
        ApplicationDbContext context, 
        ILogger logger)
    {
        if (await context.CloudProviders.AnyAsync())
        {
            logger.LogInformation("Cloud providers already seeded");
            return;
        }

        logger.LogInformation("Seeding cloud providers...");

        // AWS Provider
        var aws = new CloudProvider
        {
            Id = Guid.NewGuid(),
            Code = "aws",
            DisplayName = "Amazon Web Services",
            Description = "Leading cloud provider with global infrastructure",
            LogoUrl = "https://upload.wikimedia.org/wikipedia/commons/9/93/Amazon_Web_Services_Logo.svg",
            ApiEndpoint = "https://ec2.amazonaws.com",
            AuthType = "api-key",
            SyncEnabled = true,
            SyncIntervalMinutes = 60
        };
        context.CloudProviders.Add(aws);

        // AWS Regions
        var awsRegions = new[]
        {
            new CloudRegion 
            { 
                Id = Guid.NewGuid(), 
                ProviderId = aws.Id, 
                Code = "us-east-1", 
                Name = "US East (N. Virginia)", 
                DisplayName = "Северная Вирджиния",
                Continent = "North America", 
                Country = "USA",
                AvailabilityZones = 6,
                Compliance = JsonSerializer.Serialize(new[] { "SOC", "PCI DSS", "HIPAA" })
            },
            new CloudRegion 
            { 
                Id = Guid.NewGuid(), 
                ProviderId = aws.Id, 
                Code = "eu-west-1", 
                Name = "EU (Ireland)", 
                DisplayName = "Ирландия",
                Continent = "Europe", 
                Country = "Ireland",
                AvailabilityZones = 3,
                Compliance = JsonSerializer.Serialize(new[] { "GDPR", "SOC", "PCI DSS" })
            },
            new CloudRegion 
            { 
                Id = Guid.NewGuid(), 
                ProviderId = aws.Id, 
                Code = "ap-southeast-1", 
                Name = "Asia Pacific (Singapore)", 
                DisplayName = "Сингапур",
                Continent = "Asia", 
                Country = "Singapore",
                AvailabilityZones = 3,
                Compliance = JsonSerializer.Serialize(new[] { "SOC", "PCI DSS" })
            }
        };
        context.CloudRegions.AddRange(awsRegions);

        // AWS Services
        var awsEc2 = new CloudService
        {
            Id = Guid.NewGuid(),
            ProviderId = aws.Id,
            Code = "ec2",
            Name = "Amazon EC2",
            ServiceType = ServiceTypes.Compute,
            Description = "Elastic Compute Cloud - виртуальные серверы в облаке",
            DocumentationUrl = "https://aws.amazon.com/ec2/",
            PricingModel = JsonSerializer.Serialize(new { models = new[] { "OnDemand", "Spot", "Reserved", "SavingsPlan" } }),
            FreeTier = JsonSerializer.Serialize(new { available = true, hours = 750, instanceType = "t2.micro" }),
            SlaInfo = JsonSerializer.Serialize(new { availability = 99.99, creditPercent = 10 })
        };
        context.CloudServices.Add(awsEc2);

        // Azure Provider
        var azure = new CloudProvider
        {
            Id = Guid.NewGuid(),
            Code = "azure",
            DisplayName = "Microsoft Azure",
            Description = "Microsoft's cloud computing platform",
            LogoUrl = "https://upload.wikimedia.org/wikipedia/commons/f/fa/Microsoft_Azure.svg",
            ApiEndpoint = "https://management.azure.com",
            AuthType = "oauth",
            SyncEnabled = true,
            SyncIntervalMinutes = 60
        };
        context.CloudProviders.Add(azure);

        // Azure Regions
        var azureRegions = new[]
        {
            new CloudRegion 
            { 
                Id = Guid.NewGuid(), 
                ProviderId = azure.Id, 
                Code = "eastus", 
                Name = "East US", 
                DisplayName = "Восток США",
                Continent = "North America", 
                Country = "USA",
                AvailabilityZones = 3,
                Compliance = JsonSerializer.Serialize(new[] { "SOC", "PCI DSS", "HIPAA", "FedRAMP" })
            },
            new CloudRegion 
            { 
                Id = Guid.NewGuid(), 
                ProviderId = azure.Id, 
                Code = "westeurope", 
                Name = "West Europe", 
                DisplayName = "Западная Европа",
                Continent = "Europe", 
                Country = "Netherlands",
                AvailabilityZones = 3,
                Compliance = JsonSerializer.Serialize(new[] { "GDPR", "SOC", "PCI DSS" })
            }
        };
        context.CloudRegions.AddRange(azureRegions);

        // Azure Services
        var azureVm = new CloudService
        {
            Id = Guid.NewGuid(),
            ProviderId = azure.Id,
            Code = "virtual-machines",
            Name = "Azure Virtual Machines",
            ServiceType = ServiceTypes.Compute,
            Description = "Виртуальные машины Azure",
            DocumentationUrl = "https://azure.microsoft.com/services/virtual-machines/",
            PricingModel = JsonSerializer.Serialize(new { models = new[] { "PayAsYouGo", "Spot", "Reserved", "SavingsPlan" } }),
            FreeTier = JsonSerializer.Serialize(new { available = true, hours = 750, instanceType = "B1s" }),
            SlaInfo = JsonSerializer.Serialize(new { availability = 99.99, creditPercent = 10 })
        };
        context.CloudServices.Add(azureVm);

        // GCP Provider
        var gcp = new CloudProvider
        {
            Id = Guid.NewGuid(),
            Code = "gcp",
            DisplayName = "Google Cloud Platform",
            Description = "Google's cloud computing services",
            LogoUrl = "https://upload.wikimedia.org/wikipedia/commons/5/51/Google_Cloud_logo.svg",
            ApiEndpoint = "https://compute.googleapis.com",
            AuthType = "oauth",
            SyncEnabled = true,
            SyncIntervalMinutes = 60
        };
        context.CloudProviders.Add(gcp);

        // GCP Regions
        var gcpRegions = new[]
        {
            new CloudRegion 
            { 
                Id = Guid.NewGuid(), 
                ProviderId = gcp.Id, 
                Code = "us-central1", 
                Name = "Iowa", 
                DisplayName = "Айова",
                Continent = "North America", 
                Country = "USA",
                AvailabilityZones = 4,
                Compliance = JsonSerializer.Serialize(new[] { "SOC", "PCI DSS", "HIPAA" })
            },
            new CloudRegion 
            { 
                Id = Guid.NewGuid(), 
                ProviderId = gcp.Id, 
                Code = "europe-west4", 
                Name = "Netherlands", 
                DisplayName = "Нидерланды",
                Continent = "Europe", 
                Country = "Netherlands",
                AvailabilityZones = 3,
                Compliance = JsonSerializer.Serialize(new[] { "GDPR", "SOC", "PCI DSS" })
            }
        };
        context.CloudRegions.AddRange(gcpRegions);

        // GCP Services
        var gcpCompute = new CloudService
        {
            Id = Guid.NewGuid(),
            ProviderId = gcp.Id,
            Code = "compute-engine",
            Name = "Compute Engine",
            ServiceType = ServiceTypes.Compute,
            Description = "Виртуальные машины Google Cloud",
            DocumentationUrl = "https://cloud.google.com/compute",
            PricingModel = JsonSerializer.Serialize(new { models = new[] { "OnDemand", "Spot", "CommittedUse" } }),
            FreeTier = JsonSerializer.Serialize(new { available = true, instanceType = "e2-micro" }),
            SlaInfo = JsonSerializer.Serialize(new { availability = 99.99, creditPercent = 10 })
        };
        context.CloudServices.Add(gcpCompute);

        // Yandex Cloud Provider
        var yandex = new CloudProvider
        {
            Id = Guid.NewGuid(),
            Code = "yandex",
            DisplayName = "Yandex Cloud",
            Description = "Российская облачная платформа",
            LogoUrl = "https://upload.wikimedia.org/wikipedia/commons/d/d1/Yandex_Cloud_logo.svg",
            ApiEndpoint = "https://compute.api.cloud.yandex.net",
            AuthType = "oauth",
            SyncEnabled = true,
            SyncIntervalMinutes = 60
        };
        context.CloudProviders.Add(yandex);

        // Yandex Regions
        var yandexRegions = new[]
        {
            new CloudRegion 
            { 
                Id = Guid.NewGuid(), 
                ProviderId = yandex.Id, 
                Code = "ru-central1", 
                Name = "Россия, Москва", 
                DisplayName = "Москва",
                Continent = "Europe", 
                Country = "Russia",
                AvailabilityZones = 3,
                Compliance = JsonSerializer.Serialize(new[] { "152-ФЗ", "GDPR" })
            },
            new CloudRegion 
            { 
                Id = Guid.NewGuid(), 
                ProviderId = yandex.Id, 
                Code = "ru-central2", 
                Name = "Россия, Рязань", 
                DisplayName = "Рязань",
                Continent = "Europe", 
                Country = "Russia",
                AvailabilityZones = 1,
                Compliance = JsonSerializer.Serialize(new[] { "152-ФЗ" })
            }
        };
        context.CloudRegions.AddRange(yandexRegions);

        // Yandex Services
        var yandexCompute = new CloudService
        {
            Id = Guid.NewGuid(),
            ProviderId = yandex.Id,
            Code = "compute",
            Name = "Yandex Compute Cloud",
            ServiceType = ServiceTypes.Compute,
            Description = "Виртуальные машины Yandex Cloud",
            DocumentationUrl = "https://cloud.yandex.ru/services/compute",
            PricingModel = JsonSerializer.Serialize(new { models = new[] { "OnDemand", "Preemptible", "Committed" } }),
            FreeTier = JsonSerializer.Serialize(new { available = true }),
            SlaInfo = JsonSerializer.Serialize(new { availability = 99.9, creditPercent = 10 })
        };
        context.CloudServices.Add(yandexCompute);

        // VK Cloud Provider
        var vk = new CloudProvider
        {
            Id = Guid.NewGuid(),
            Code = "vk",
            DisplayName = "VK Cloud",
            Description = "Облачная платформа VK (бывший Mail.ru Cloud)",
            LogoUrl = "https://upload.wikimedia.org/wikipedia/commons/2/21/VK_logo.svg",
            ApiEndpoint = "https://mcs.mail.ru",
            AuthType = "api-key",
            SyncEnabled = true,
            SyncIntervalMinutes = 60
        };
        context.CloudProviders.Add(vk);

        // VK Regions
        var vkRegions = new[]
        {
            new CloudRegion 
            { 
                Id = Guid.NewGuid(), 
                ProviderId = vk.Id, 
                Code = "msk1", 
                Name = "Москва", 
                DisplayName = "Москва",
                Continent = "Europe", 
                Country = "Russia",
                AvailabilityZones = 2,
                Compliance = JsonSerializer.Serialize(new[] { "152-ФЗ" })
            }
        };
        context.CloudRegions.AddRange(vkRegions);

        // VK Services
        var vkCompute = new CloudService
        {
            Id = Guid.NewGuid(),
            ProviderId = vk.Id,
            Code = "compute",
            Name = "VK Cloud Compute",
            ServiceType = ServiceTypes.Compute,
            Description = "Виртуальные машины VK Cloud",
            DocumentationUrl = "https://mcs.mail.ru/docs/",
            PricingModel = JsonSerializer.Serialize(new { models = new[] { "OnDemand" } }),
            FreeTier = JsonSerializer.Serialize(new { available = false }),
            SlaInfo = JsonSerializer.Serialize(new { availability = 99.9, creditPercent = 10 })
        };
        context.CloudServices.Add(vkCompute);

        // On-Premise (для гибридных сценариев)
        var onPremise = new CloudProvider
        {
            Id = Guid.NewGuid(),
            Code = "on-premise",
            DisplayName = "Собственная инфраструктура",
            Description = "Локальные серверы и ЦОД",
            LogoUrl = null,
            ApiEndpoint = null,
            AuthType = "none",
            SyncEnabled = false,
            SyncIntervalMinutes = 0
        };
        context.CloudProviders.Add(onPremise);

        await context.SaveChangesAsync();
        logger.LogInformation("Cloud providers seeded successfully");

        // Seed sample instance types for AWS
        await SeedAwsInstanceTypesAsync(context, aws, awsRegions, awsEc2, logger);
        await SeedAzureInstanceTypesAsync(context, azure, azureRegions, azureVm, logger);
        await SeedGcpInstanceTypesAsync(context, gcp, gcpRegions, gcpCompute, logger);
        await SeedYandexInstanceTypesAsync(context, yandex, yandexRegions, yandexCompute, logger);
    }

    private static async Task SeedAwsInstanceTypesAsync(
        ApplicationDbContext context,
        CloudProvider aws,
        CloudRegion[] regions,
        CloudService ec2,
        ILogger logger)
    {
        var usEast1 = regions.First(r => r.Code == "us-east-1");
        
        var instanceTypes = new[]
        {
            CreateAwsInstance(aws.Id, usEast1.Id, ec2.Id, "t3.micro", "t3.micro", 
                "Burstable", "t3", 2, 1, "Up to 5 Gbps", 0.0104m, 7.60m, 0.0031m, 70m),
            CreateAwsInstance(aws.Id, usEast1.Id, ec2.Id, "t3.small", "t3.small",
                "Burstable", "t3", 2, 2, "Up to 5 Gbps", 0.0208m, 15.20m, 0.0062m, 70m),
            CreateAwsInstance(aws.Id, usEast1.Id, ec2.Id, "t3.medium", "t3.medium",
                "Burstable", "t3", 2, 4, "Up to 5 Gbps", 0.0416m, 30.40m, 0.0125m, 70m),
            CreateAwsInstance(aws.Id, usEast1.Id, ec2.Id, "m5.large", "m5.large",
                "General Purpose", "m5", 2, 8, "Up to 10 Gbps", 0.096m, 70.08m, 0.0288m, 70m),
            CreateAwsInstance(aws.Id, usEast1.Id, ec2.Id, "m5.xlarge", "m5.xlarge",
                "General Purpose", "m5", 4, 16, "Up to 10 Gbps", 0.192m, 140.16m, 0.0576m, 70m),
            CreateAwsInstance(aws.Id, usEast1.Id, ec2.Id, "c5.large", "c5.large",
                "Compute Optimized", "c5", 2, 4, "Up to 10 Gbps", 0.085m, 62.05m, 0.0255m, 70m),
            CreateAwsInstance(aws.Id, usEast1.Id, ec2.Id, "c5.xlarge", "c5.xlarge",
                "Compute Optimized", "c5", 4, 8, "Up to 10 Gbps", 0.17m, 124.10m, 0.051m, 70m),
            CreateAwsInstance(aws.Id, usEast1.Id, ec2.Id, "r5.large", "r5.large",
                "Memory Optimized", "r5", 2, 16, "Up to 10 Gbps", 0.126m, 91.98m, 0.0378m, 70m),
            CreateAwsInstance(aws.Id, usEast1.Id, ec2.Id, "r5.xlarge", "r5.xlarge",
                "Memory Optimized", "r5", 4, 32, "Up to 10 Gbps", 0.252m, 183.96m, 0.0756m, 70m),
        };

        foreach (var instance in instanceTypes)
        {
            context.InstanceTypes.Add(instance.instanceType);
            context.InstancePricing.Add(instance.pricing);
        }

        await context.SaveChangesAsync();
        logger.LogInformation("AWS instance types seeded");
    }

    private static (InstanceType instanceType, InstancePricing pricing) CreateAwsInstance(
        Guid providerId, Guid regionId, Guid serviceId,
        string typeCode, string displayName, string category, string family,
        double vcpu, double memoryGb, string networkPerf,
        decimal onDemandHourly, decimal onDemandMonthly, decimal spotPrice, decimal spotSavingsPercent)
    {
        var instanceType = new InstanceType
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            RegionId = regionId,
            ServiceId = serviceId,
            TypeCode = typeCode,
            DisplayName = displayName,
            Category = category,
            Family = family,
            VcpuCount = vcpu,
            CpuArchitecture = "x86_64",
            MemoryGb = memoryGb,
            NetworkBandwidthGbps = 5,
            NetworkPerformance = networkPerf,
            StorageType = "EBS",
            EbsOptimized = true,
            VirtualizationType = "HVM",
            Availability = InstanceAvailability.Available
        };

        var pricing = new InstancePricing
        {
            Id = Guid.NewGuid(),
            InstanceTypeId = instanceType.Id,
            Currency = "USD",
            OnDemandHourly = onDemandHourly,
            OnDemandMonthly = onDemandMonthly,
            SpotCurrentPrice = spotPrice,
            SpotAveragePrice = spotPrice * 1.1m,
            SpotSavingsPercent = spotSavingsPercent,
            Reserved1YearAllUpfront = onDemandMonthly * 12 * 0.6m,
            Reserved1YearSavingsPercent = 40,
            Reserved3YearAllUpfront = onDemandMonthly * 36 * 0.4m,
            Reserved3YearSavingsPercent = 60,
            StorageGbMonthly = 0.10m,
            DataTransferOutGb = 0.09m
        };

        return (instanceType, pricing);
    }

    private static async Task SeedAzureInstanceTypesAsync(
        ApplicationDbContext context,
        CloudProvider azure,
        CloudRegion[] regions,
        CloudService vm,
        ILogger logger)
    {
        var eastUs = regions.First(r => r.Code == "eastus");
        
        var instanceTypes = new[]
        {
            CreateAzureInstance(azure.Id, eastUs.Id, vm.Id, "Standard_B1s", "B1s",
                "Burstable", "B", 1, 1, "Low", 0.0104m, 7.59m),
            CreateAzureInstance(azure.Id, eastUs.Id, vm.Id, "Standard_B2s", "B2s",
                "Burstable", "B", 2, 4, "Low", 0.0416m, 30.37m),
            CreateAzureInstance(azure.Id, eastUs.Id, vm.Id, "Standard_D2s_v3", "D2s v3",
                "General Purpose", "Dsv3", 2, 8, "Moderate", 0.096m, 70.08m),
            CreateAzureInstance(azure.Id, eastUs.Id, vm.Id, "Standard_D4s_v3", "D4s v3",
                "General Purpose", "Dsv3", 4, 16, "Moderate", 0.192m, 140.16m),
            CreateAzureInstance(azure.Id, eastUs.Id, vm.Id, "Standard_F2s_v2", "F2s v2",
                "Compute Optimized", "Fsv2", 2, 4, "Moderate", 0.085m, 62.05m),
            CreateAzureInstance(azure.Id, eastUs.Id, vm.Id, "Standard_E2s_v3", "E2s v3",
                "Memory Optimized", "Esv3", 2, 16, "Moderate", 0.126m, 91.98m),
        };

        foreach (var instance in instanceTypes)
        {
            context.InstanceTypes.Add(instance.instanceType);
            context.InstancePricing.Add(instance.pricing);
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Azure instance types seeded");
    }

    private static (InstanceType instanceType, InstancePricing pricing) CreateAzureInstance(
        Guid providerId, Guid regionId, Guid serviceId,
        string typeCode, string displayName, string category, string family,
        double vcpu, double memoryGb, string networkPerf,
        decimal onDemandHourly, decimal onDemandMonthly)
    {
        var instanceType = new InstanceType
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            RegionId = regionId,
            ServiceId = serviceId,
            TypeCode = typeCode,
            DisplayName = displayName,
            Category = category,
            Family = family,
            VcpuCount = vcpu,
            CpuArchitecture = "x86_64",
            MemoryGb = memoryGb,
            NetworkBandwidthGbps = 5,
            NetworkPerformance = networkPerf,
            StorageType = "Premium SSD",
            VirtualizationType = "HVM",
            Availability = InstanceAvailability.Available
        };

        var pricing = new InstancePricing
        {
            Id = Guid.NewGuid(),
            InstanceTypeId = instanceType.Id,
            Currency = "USD",
            OnDemandHourly = onDemandHourly,
            OnDemandMonthly = onDemandMonthly,
            SpotCurrentPrice = onDemandHourly * 0.3m,
            SpotSavingsPercent = 70,
            Reserved1YearAllUpfront = onDemandMonthly * 12 * 0.6m,
            Reserved1YearSavingsPercent = 40,
            Reserved3YearAllUpfront = onDemandMonthly * 36 * 0.4m,
            Reserved3YearSavingsPercent = 60,
            StorageGbMonthly = 0.15m,
            DataTransferOutGb = 0.087m
        };

        return (instanceType, pricing);
    }

    private static async Task SeedGcpInstanceTypesAsync(
        ApplicationDbContext context,
        CloudProvider gcp,
        CloudRegion[] regions,
        CloudService compute,
        ILogger logger)
    {
        var usCentral1 = regions.First(r => r.Code == "us-central1");
        
        var instanceTypes = new[]
        {
            CreateGcpInstance(gcp.Id, usCentral1.Id, compute.Id, "e2-micro", "e2-micro",
                "General Purpose", "e2", 0.25, 1, 0.0076m, 5.55m),
            CreateGcpInstance(gcp.Id, usCentral1.Id, compute.Id, "e2-small", "e2-small",
                "General Purpose", "e2", 0.5, 2, 0.0152m, 11.10m),
            CreateGcpInstance(gcp.Id, usCentral1.Id, compute.Id, "e2-medium", "e2-medium",
                "General Purpose", "e2", 1, 4, 0.0303m, 22.12m),
            CreateGcpInstance(gcp.Id, usCentral1.Id, compute.Id, "n2-standard-2", "n2-standard-2",
                "General Purpose", "n2", 2, 8, 0.097m, 70.81m),
            CreateGcpInstance(gcp.Id, usCentral1.Id, compute.Id, "c2-standard-4", "c2-standard-4",
                "Compute Optimized", "c2", 4, 16, 0.209m, 152.57m),
            CreateGcpInstance(gcp.Id, usCentral1.Id, compute.Id, "m2-megamem-96", "m2-megamem-96",
                "Memory Optimized", "m2", 96, 1433, 10.79m, 7876.70m),
        };

        foreach (var instance in instanceTypes)
        {
            context.InstanceTypes.Add(instance.instanceType);
            context.InstancePricing.Add(instance.pricing);
        }

        await context.SaveChangesAsync();
        logger.LogInformation("GCP instance types seeded");
    }

    private static (InstanceType instanceType, InstancePricing pricing) CreateGcpInstance(
        Guid providerId, Guid regionId, Guid serviceId,
        string typeCode, string displayName, string category, string family,
        double vcpu, double memoryGb, decimal onDemandHourly, decimal onDemandMonthly)
    {
        var instanceType = new InstanceType
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            RegionId = regionId,
            ServiceId = serviceId,
            TypeCode = typeCode,
            DisplayName = displayName,
            Category = category,
            Family = family,
            VcpuCount = vcpu,
            CpuArchitecture = "x86_64",
            MemoryGb = memoryGb,
            NetworkBandwidthGbps = 10,
            NetworkPerformance = "Up to 10 Gbps",
            StorageType = "Persistent Disk",
            VirtualizationType = "HVM",
            Availability = InstanceAvailability.Available
        };

        var pricing = new InstancePricing
        {
            Id = Guid.NewGuid(),
            InstanceTypeId = instanceType.Id,
            Currency = "USD",
            OnDemandHourly = onDemandHourly,
            OnDemandMonthly = onDemandMonthly,
            SpotCurrentPrice = onDemandHourly * 0.4m,
            SpotSavingsPercent = 60,
            StorageGbMonthly = 0.10m,
            DataTransferOutGb = 0.12m
        };

        return (instanceType, pricing);
    }

    private static async Task SeedYandexInstanceTypesAsync(
        ApplicationDbContext context,
        CloudProvider yandex,
        CloudRegion[] regions,
        CloudService compute,
        ILogger logger)
    {
        var moscow = regions.First(r => r.Code == "ru-central1");
        
        var instanceTypes = new[]
        {
            CreateYandexInstance(yandex.Id, moscow.Id, compute.Id, "standard-v1", "Standard v1",
                "General Purpose", "standard", 2, 4, 0.012m, 8.76m, "RUB"),
            CreateYandexInstance(yandex.Id, moscow.Id, compute.Id, "standard-v2", "Standard v2",
                "General Purpose", "standard", 4, 8, 0.024m, 17.52m, "RUB"),
            CreateYandexInstance(yandex.Id, moscow.Id, compute.Id, "standard-v3", "Standard v3",
                "General Purpose", "standard", 8, 16, 0.048m, 35.04m, "RUB"),
            CreateYandexInstance(yandex.Id, moscow.Id, compute.Id, "memory-optimized", "Memory Optimized",
                "Memory Optimized", "memory", 8, 64, 0.096m, 70.08m, "RUB"),
        };

        foreach (var instance in instanceTypes)
        {
            context.InstanceTypes.Add(instance.instanceType);
            context.InstancePricing.Add(instance.pricing);
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Yandex instance types seeded");
    }

    private static (InstanceType instanceType, InstancePricing pricing) CreateYandexInstance(
        Guid providerId, Guid regionId, Guid serviceId,
        string typeCode, string displayName, string category, string family,
        double vcpu, double memoryGb, decimal onDemandHourly, decimal onDemandMonthly, string currency)
    {
        var instanceType = new InstanceType
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            RegionId = regionId,
            ServiceId = serviceId,
            TypeCode = typeCode,
            DisplayName = displayName,
            Category = category,
            Family = family,
            VcpuCount = vcpu,
            CpuArchitecture = "x86_64",
            MemoryGb = memoryGb,
            NetworkBandwidthGbps = 5,
            NetworkPerformance = "Up to 5 Gbps",
            StorageType = "Network SSD",
            VirtualizationType = "HVM",
            Availability = InstanceAvailability.Available
        };

        var pricing = new InstancePricing
        {
            Id = Guid.NewGuid(),
            InstanceTypeId = instanceType.Id,
            Currency = currency,
            OnDemandHourly = onDemandHourly,
            OnDemandMonthly = onDemandMonthly,
            StorageGbMonthly = 0.012m,
            DataTransferOutGb = 0.015m
        };

        return (instanceType, pricing);
    }
}