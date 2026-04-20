using System;
using System.Collections.Generic;

namespace HybridCloudWorkloads.API.Models;

#region Provider DTOs

public class ProviderDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool SyncEnabled { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string[] SupportedFeatures { get; set; } = Array.Empty<string>();
    public int SyncIntervalMinutes { get; set; }
}

public class ProviderDetailDto : ProviderDto
{
    public string? ApiEndpoint { get; set; }
    public string AuthType { get; set; } = string.Empty;
    public int SyncIntervalMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int RegionsCount { get; set; }
    public int ServicesCount { get; set; }
    public SyncStatusDto? SyncStatus { get; set; }
}

public class AvailabilityResponse
{
    public string ProviderCode { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public DateTime CheckedAt { get; set; }
}

#endregion

#region Region DTOs

public class RegionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Continent { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? City { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AvailabilityZones { get; set; }
    public string[]? Compliance { get; set; }
}

public class RegionDetailDto : RegionDto
{
    public string? Coordinates { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalInstanceTypes { get; set; }
    public List<CategoryStatDto> CategoryStats { get; set; } = new();
}

public class RegionInfoDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int AvailabilityZones { get; set; }
}

public class CategoryStatDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public int MinVcpu { get; set; }
    public int MaxVcpu { get; set; }
    public double MinMemory { get; set; }
    public double MaxMemory { get; set; }
}

#endregion

#region Instance Type DTOs

public class InstanceTypesResponse
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<InstanceTypeDto> Items { get; set; } = new();
    public FilterOptionsDto AvailableFilters { get; set; } = new();
}

public class InstanceTypeDto
{
    public Guid Id { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public double VcpuCount { get; set; }
    public double MemoryGb { get; set; }
    public string CpuArchitecture { get; set; } = string.Empty;
    public string? CpuModel { get; set; }
    public double NetworkBandwidthGbps { get; set; }
    public string? NetworkPerformance { get; set; }
    public string StorageType { get; set; } = string.Empty;
    public double? LocalStorageGb { get; set; }
    public bool HasGpu { get; set; }
    public string? GpuModel { get; set; }
    public int? GpuCount { get; set; }
    public string Availability { get; set; } = string.Empty;
    public string? RegionCode { get; set; }
    public string? RegionName { get; set; }
    public PricingDto? Pricing { get; set; }
}

public class InstanceTypeDetailDto : InstanceTypeDto
{
    public string? Description { get; set; }
    public int Generation { get; set; }
    public double? CpuClockSpeedGhz { get; set; }
    public string CpuType { get; set; } = string.Empty;
    public string? PhysicalProcessor { get; set; }
    public bool EnhancedNetworking { get; set; }
    public int? LocalStorageDisks { get; set; }
    public bool EbsOptimized { get; set; }
    public double? MaxEbsBandwidthMbps { get; set; }
    public int? MaxIops { get; set; }
    public int? GpuMemoryGb { get; set; }
    public bool HasFpga { get; set; }
    public string VirtualizationType { get; set; } = string.Empty;
    public bool PlacementGroupSupported { get; set; }
    public bool DedicatedHostSupported { get; set; }
    public RegionInfoDto? Region { get; set; }
    public new PricingDetailDto? Pricing { get; set; }
}

public class FilterOptionsDto
{
    public List<string> Categories { get; set; } = new();
    public List<string?> Regions { get; set; } = new();
    public RangeDto CpuRange { get; set; } = new();
    public RangeDto MemoryRange { get; set; } = new();
}

public class RangeDto
{
    public double Min { get; set; }
    public double Max { get; set; }
}

#endregion

#region Pricing DTOs

public class PricingDto
{
    public string Currency { get; set; } = "USD";
    public decimal OnDemandHourly { get; set; }
    public decimal OnDemandMonthly { get; set; }
    public decimal? SpotCurrentPrice { get; set; }
    public decimal? SpotSavingsPercent { get; set; }
    public decimal? Reserved1YearHourly { get; set; }
    public decimal? Reserved3YearHourly { get; set; }
}

public class PricingDetailDto
{
    public string Currency { get; set; } = "USD";
    public OnDemandPricingDto OnDemand { get; set; } = new();
    public SpotPricingDto? Spot { get; set; }
    public ReservedPricingDto Reserved { get; set; } = new();
    public AdditionalCostsDto AdditionalCosts { get; set; } = new();
}

public class OnDemandPricingDto
{
    public decimal Hourly { get; set; }
    public decimal Monthly { get; set; }
}

public class SpotPricingDto
{
    public decimal CurrentPrice { get; set; }
    public decimal? AveragePrice { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? SavingsPercent { get; set; }
    public double? InterruptionRate { get; set; }
}

public class ReservedPricingDto
{
    public ReservedOptionDto? OneYear { get; set; }
    public ReservedOptionDto? ThreeYear { get; set; }
}

public class ReservedOptionDto
{
    public decimal Upfront { get; set; }
    public decimal HourlyEquivalent { get; set; }
    public decimal? SavingsPercent { get; set; }
}

public class AdditionalCostsDto
{
    public decimal? StorageGbMonthly { get; set; }
    public decimal? DataTransferOutGb { get; set; }
    public decimal? DataTransferInGb { get; set; }
    public decimal? DataTransferInterRegionGb { get; set; }
    public decimal? StaticIpMonthly { get; set; }
    public decimal? LoadBalancerHourly { get; set; }
}

#endregion

#region Service DTOs

public class ServiceDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DocumentationUrl { get; set; }
    public bool HasFreeTier { get; set; }
}

#endregion

#region Comparison DTOs

public class CompareInstanceTypesRequest
{
    public List<CompareItem> Items { get; set; } = new();
}

public class CompareItem
{
    public string ProviderCode { get; set; } = string.Empty;
    public string TypeCode { get; set; } = string.Empty;
    public string? RegionCode { get; set; }
}

public class InstanceTypesComparisonDto
{
    public List<InstanceTypeDetailDto> Items { get; set; } = new();
    public ComparisonSummaryDto? Comparison { get; set; }
}

public class ComparisonSummaryDto
{
    public string? BestValue { get; set; }
    public string? BestPerformance { get; set; }
    public string? BestMemory { get; set; }
    public string? BestNetwork { get; set; }
}

#endregion

#region Recommendations DTOs

public class RecommendationsRequest
{
    public int Cpu { get; set; }
    public double Memory { get; set; }
    public string[]? Providers { get; set; }
    public string? PreferredRegion { get; set; }
    public string? PreferredCategory { get; set; }
    public decimal? MaxBudget { get; set; }
    public bool IncludeSpot { get; set; } = true;
    public bool IncludeReserved { get; set; } = false;
}

public class RecommendationsResponse
{
    public RecommendationsRequest Request { get; set; } = new();
    public List<InstanceRecommendationDto> Recommendations { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class InstanceRecommendationDto
{
    public string ProviderCode { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public string InstanceType { get; set; } = string.Empty;
    public int Vcpu { get; set; }
    public double MemoryGb { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal OnDemandHourly { get; set; }
    public decimal OnDemandMonthly { get; set; }
    public decimal? SpotHourly { get; set; }
    public string Currency { get; set; } = "USD";
    public double MatchScore { get; set; }
    public List<string> Features { get; set; } = new();
}

#endregion

#region Sync DTOs

public class SyncStatusDto
{
    public string ProviderCode { get; set; } = string.Empty;
    public DateTime? LastSyncAt { get; set; }
    public bool LastSyncSuccess { get; set; }
    public string? LastSyncError { get; set; }
    public bool IsRunning { get; set; }
    public DateTime? NextSyncAt { get; set; }
    public SyncStatisticsDto? LastStatistics { get; set; }
}

public class SyncStatisticsDto
{
    public int RegionsAdded { get; set; }
    public int RegionsUpdated { get; set; }
    public int ServicesAdded { get; set; }
    public int ServicesUpdated { get; set; }
    public int InstanceTypesAdded { get; set; }
    public int InstanceTypesUpdated { get; set; }
    public int PricingsUpdated { get; set; }
    public int TotalChanges { get; set; }
}

#endregion