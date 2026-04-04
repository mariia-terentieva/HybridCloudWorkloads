using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HybridCloudWorkloads.Infrastructure.src.HybridCloudWorkloads.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerformanceMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkloadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CpuUsagePercent = table.Column<double>(type: "double precision", nullable: false),
                    MemoryUsagePercent = table.Column<double>(type: "double precision", nullable: false),
                    MemoryUsageMB = table.Column<double>(type: "double precision", nullable: false),
                    NetworkInBytesPerSec = table.Column<long>(type: "bigint", nullable: false),
                    NetworkOutBytesPerSec = table.Column<long>(type: "bigint", nullable: false),
                    DiskReadOpsPerSec = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DiskWriteOpsPerSec = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ResponseTimeMs = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    RequestsPerSecond = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ContainerStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AdditionalMetrics = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceMetrics_Workloads_WorkloadId",
                        column: x => x.WorkloadId,
                        principalTable: "Workloads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_Timestamp",
                table: "PerformanceMetrics",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_WorkloadId",
                table: "PerformanceMetrics",
                column: "WorkloadId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_WorkloadId_Timestamp",
                table: "PerformanceMetrics",
                columns: new[] { "WorkloadId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformanceMetrics");
        }
    }
}
