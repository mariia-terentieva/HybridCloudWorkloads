using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HybridCloudWorkloads.Infrastructure.src.HybridCloudWorkloads.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudProviderEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CloudProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApiEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AuthType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "api-key"),
                    AuthConfig = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    SyncEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SyncIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CloudRegions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Continent = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Coordinates = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AvailabilityZones = table.Column<int>(type: "integer", nullable: false),
                    Compliance = table.Column<string>(type: "jsonb", nullable: true),
                    AvailableServices = table.Column<string>(type: "jsonb", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudRegions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CloudRegions_CloudProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "CloudProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CloudServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Compute"),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DocumentationUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PricingModel = table.Column<string>(type: "jsonb", nullable: true),
                    FreeTier = table.Column<string>(type: "jsonb", nullable: true),
                    SlaInfo = table.Column<string>(type: "jsonb", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CloudServices_CloudProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "CloudProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Discounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DiscountType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Promotional"),
                    Conditions = table.Column<string>(type: "jsonb", nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    AppliesTo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MinimumSpend = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    MaximumDiscount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PromoCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Discounts_CloudProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "CloudProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InstanceTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    TypeCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "General Purpose"),
                    Family = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Generation = table.Column<int>(type: "integer", nullable: false),
                    VcpuCount = table.Column<double>(type: "double precision", nullable: false),
                    CpuModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CpuArchitecture = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "x86_64"),
                    CpuClockSpeedGhz = table.Column<double>(type: "double precision", nullable: true),
                    CpuType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Dedicated"),
                    MemoryGb = table.Column<double>(type: "double precision", nullable: false),
                    NetworkBandwidthGbps = table.Column<double>(type: "double precision", nullable: false),
                    NetworkPerformance = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StorageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "EBS"),
                    LocalStorageGb = table.Column<double>(type: "double precision", nullable: true),
                    LocalStorageDisks = table.Column<int>(type: "integer", nullable: true),
                    EbsOptimized = table.Column<bool>(type: "boolean", nullable: false),
                    MaxEbsBandwidthMbps = table.Column<double>(type: "double precision", nullable: true),
                    MaxIops = table.Column<int>(type: "integer", nullable: true),
                    HasGpu = table.Column<bool>(type: "boolean", nullable: false),
                    GpuModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GpuCount = table.Column<int>(type: "integer", nullable: true),
                    GpuMemoryGb = table.Column<int>(type: "integer", nullable: true),
                    HasFpga = table.Column<bool>(type: "boolean", nullable: false),
                    VirtualizationType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "HVM"),
                    EnhancedNetworking = table.Column<bool>(type: "boolean", nullable: false),
                    PlacementGroupSupported = table.Column<bool>(type: "boolean", nullable: false),
                    DedicatedHostSupported = table.Column<bool>(type: "boolean", nullable: false),
                    PhysicalProcessor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PerformanceCharacteristics = table.Column<string>(type: "jsonb", nullable: true),
                    Availability = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstanceTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstanceTypes_CloudProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "CloudProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InstanceTypes_CloudRegions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "CloudRegions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InstanceTypes_CloudServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "CloudServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InstancePricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    OnDemandHourly = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    OnDemandMonthly = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SpotCurrentPrice = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    SpotAveragePrice = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    SpotMinPrice = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    SpotMaxPrice = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    SpotSavingsPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    SpotInterruptionRate = table.Column<double>(type: "double precision", nullable: true),
                    Reserved1YearNoUpfront = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    Reserved1YearPartialUpfront = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    Reserved1YearAllUpfront = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    Reserved3YearNoUpfront = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    Reserved3YearPartialUpfront = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    Reserved3YearAllUpfront = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    Reserved1YearSavingsPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Reserved3YearSavingsPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    StorageGbMonthly = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    DataTransferOutGb = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    DataTransferInGb = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    DataTransferInterRegionGb = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    StaticIpMonthly = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    LoadBalancerHourly = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstancePricing", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstancePricing_InstanceTypes_InstanceTypeId",
                        column: x => x.InstanceTypeId,
                        principalTable: "InstanceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CloudProviders_Code",
                table: "CloudProviders",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CloudRegions_ProviderId_Code",
                table: "CloudRegions",
                columns: new[] { "ProviderId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CloudServices_ProviderId_Code",
                table: "CloudServices",
                columns: new[] { "ProviderId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_ProviderId",
                table: "Discounts",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_InstancePricing_EffectiveDate",
                table: "InstancePricing",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_InstancePricing_InstanceTypeId",
                table: "InstancePricing",
                column: "InstanceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_InstanceTypes_ProviderId_RegionId_TypeCode",
                table: "InstanceTypes",
                columns: new[] { "ProviderId", "RegionId", "TypeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstanceTypes_RegionId",
                table: "InstanceTypes",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_InstanceTypes_ServiceId",
                table: "InstanceTypes",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Discounts");

            migrationBuilder.DropTable(
                name: "InstancePricing");

            migrationBuilder.DropTable(
                name: "InstanceTypes");

            migrationBuilder.DropTable(
                name: "CloudRegions");

            migrationBuilder.DropTable(
                name: "CloudServices");

            migrationBuilder.DropTable(
                name: "CloudProviders");
        }
    }
}
