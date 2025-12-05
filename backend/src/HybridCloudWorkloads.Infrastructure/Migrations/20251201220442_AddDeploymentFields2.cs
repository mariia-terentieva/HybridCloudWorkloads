using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HybridCloudWorkloads.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeploymentFields2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessUrl",
                table: "Workloads",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContainerId",
                table: "Workloads",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContainerImage",
                table: "Workloads",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeployedAt",
                table: "Workloads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeploymentStatus",
                table: "Workloads",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnvironmentVariables",
                table: "Workloads",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExposedPort",
                table: "Workloads",
                type: "integer",
                nullable: false,
                defaultValue: 80);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessUrl",
                table: "Workloads");

            migrationBuilder.DropColumn(
                name: "ContainerId",
                table: "Workloads");

            migrationBuilder.DropColumn(
                name: "ContainerImage",
                table: "Workloads");

            migrationBuilder.DropColumn(
                name: "DeployedAt",
                table: "Workloads");

            migrationBuilder.DropColumn(
                name: "DeploymentStatus",
                table: "Workloads");

            migrationBuilder.DropColumn(
                name: "EnvironmentVariables",
                table: "Workloads");

            migrationBuilder.DropColumn(
                name: "ExposedPort",
                table: "Workloads");
        }
    }
}
