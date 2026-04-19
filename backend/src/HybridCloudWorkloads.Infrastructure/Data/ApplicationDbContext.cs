using HybridCloudWorkloads.Core.Entities;
using HybridCloudWorkloads.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HybridCloudWorkloads.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Workload> Workloads { get; set; }
    public DbSet<PerformanceMetric> PerformanceMetrics { get; set; }
    public DbSet<CloudProvider> CloudProviders { get; set; }
    public DbSet<CloudRegion> CloudRegions { get; set; }
    public DbSet<CloudService> CloudServices { get; set; }
    public DbSet<InstanceType> InstanceTypes { get; set; }
    public DbSet<InstancePricing> InstancePricing { get; set; }
    public DbSet<Discount> Discounts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Workload>(entity =>
        {
            entity.HasKey(w => w.Id);
            
            entity.Property(w => w.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(w => w.Description)
                .HasMaxLength(1000);
                
            entity.Property(w => w.Type)
                .IsRequired();
                
            entity.Property(w => w.RequiredCpu)
                .IsRequired();
                
            entity.Property(w => w.RequiredMemory)
                .IsRequired();
                
            entity.Property(w => w.RequiredStorage)
                .IsRequired();
                
            // Существующие поля для деплоя
            entity.Property(w => w.ContainerImage)
                .HasMaxLength(500);
                
            entity.Property(w => w.ExposedPort)
                .HasDefaultValue(80);
                
            entity.Property(w => w.EnvironmentVariables)
                .HasColumnType("jsonb");
                
            entity.Property(w => w.DeploymentStatus)
                .HasMaxLength(50);
                
            entity.Property(w => w.ContainerId)
                .HasMaxLength(100);
                
            entity.Property(w => w.AccessUrl)
                .HasMaxLength(500);
                
            // Поля классификации
            entity.Property(w => w.UsagePattern)
                .IsRequired()
                .HasDefaultValue(UsagePattern.Constant)
                .HasSentinel(UsagePattern.Constant);
                
            entity.Property(w => w.Criticality)
                .IsRequired()
                .HasDefaultValue(CriticalityClass.NonCritical)
                .HasSentinel(CriticalityClass.NonCritical);
                
            entity.Property(w => w.BudgetTier)
                .IsRequired()
                .HasDefaultValue(BudgetTier.Medium)
                .HasSentinel(BudgetTier.Medium);
                
            entity.Property(w => w.SlaRequirements)
                .HasColumnType("jsonb");
                
            entity.Property(w => w.BusinessHours)
                .HasColumnType("jsonb");
                
            entity.Property(w => w.Tags)
                .HasColumnType("jsonb");
                
            entity.Property(w => w.BaselinePerformance)
                .HasColumnType("jsonb");
                
            entity.Property(w => w.LastProfiledAt);
                
            // Связь с пользователем
            entity.HasOne<User>()
                .WithMany(u => u.Workloads)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PerformanceMetric>(entity =>
        {
            entity.HasKey(m => m.Id);
            
            entity.Property(m => m.WorkloadId)
                .IsRequired();
                
            entity.Property(m => m.Timestamp)
                .IsRequired();
                
            entity.Property(m => m.CpuUsagePercent)
                .IsRequired();
                
            entity.Property(m => m.MemoryUsagePercent)
                .IsRequired();
                
            entity.Property(m => m.MemoryUsageMB)
                .IsRequired();
                
            entity.Property(m => m.NetworkInBytesPerSec)
                .IsRequired();
                
            entity.Property(m => m.NetworkOutBytesPerSec)
                .IsRequired();
                
            entity.Property(m => m.DiskReadOpsPerSec)
                .HasDefaultValue(0);
                
            entity.Property(m => m.DiskWriteOpsPerSec)
                .HasDefaultValue(0);
                
            entity.Property(m => m.ResponseTimeMs)
                .HasDefaultValue(0);
                
            entity.Property(m => m.RequestsPerSecond)
                .HasDefaultValue(0);
                
            entity.Property(m => m.ErrorCount)
                .HasDefaultValue(0);
                
            entity.Property(m => m.ContainerStatus)
                .HasMaxLength(50);
                
            entity.Property(m => m.AdditionalMetrics)
                .HasColumnType("jsonb");
                
            entity.HasIndex(m => m.WorkloadId);
            entity.HasIndex(m => m.Timestamp);
            entity.HasIndex(m => new { m.WorkloadId, m.Timestamp });
            
            // Связь с Workload
            entity.HasOne(m => m.Workload)
                .WithMany()
                .HasForeignKey(m => m.WorkloadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<User>(entity =>
        {
            entity.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.HasMany(u => u.Workloads)
                .WithOne()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

                // CloudProvider
        builder.Entity<CloudProvider>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(e => e.DisplayName)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.Description)
                .HasMaxLength(500);
                
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(500);
                
            entity.Property(e => e.ApiEndpoint)
                .HasMaxLength(500);
                
            entity.Property(e => e.AuthType)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("api-key");
                
            entity.Property(e => e.AuthConfig)
                .HasColumnType("jsonb");
                
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb");
                
            entity.HasIndex(e => e.Code)
                .IsUnique();
        });

        // CloudRegion
        builder.Entity<CloudRegion>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.DisplayName)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.Continent)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(e => e.Country)
                .HasMaxLength(50);
                
            entity.Property(e => e.City)
                .HasMaxLength(100);
                
            entity.Property(e => e.Coordinates)
                .HasMaxLength(50);
                
            entity.Property(e => e.Compliance)
                .HasColumnType("jsonb");
                
            entity.Property(e => e.AvailableServices)
                .HasColumnType("jsonb");
                
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb");
                
            entity.HasIndex(e => new { e.ProviderId, e.Code })
                .IsUnique();
                
            entity.HasOne(e => e.Provider)
                .WithMany(p => p.Regions)
                .HasForeignKey(e => e.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CloudService
        builder.Entity<CloudService>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.ServiceType)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Compute");
                
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
                
            entity.Property(e => e.DocumentationUrl)
                .HasMaxLength(500);
                
            entity.Property(e => e.PricingModel)
                .HasColumnType("jsonb");
                
            entity.Property(e => e.FreeTier)
                .HasColumnType("jsonb");
                
            entity.Property(e => e.SlaInfo)
                .HasColumnType("jsonb");
                
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb");
                
            entity.HasIndex(e => new { e.ProviderId, e.Code })
                .IsUnique();
                
            entity.HasOne(e => e.Provider)
                .WithMany(p => p.Services)
                .HasForeignKey(e => e.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // InstanceType
        builder.Entity<InstanceType>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.TypeCode)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.DisplayName)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
                
            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("General Purpose");
                
            entity.Property(e => e.Family)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(e => e.CpuModel)
                .HasMaxLength(100);
                
            entity.Property(e => e.CpuArchitecture)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("x86_64");
                
            entity.Property(e => e.CpuType)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Dedicated");
                
            entity.Property(e => e.NetworkPerformance)
                .HasMaxLength(100);
                
            entity.Property(e => e.StorageType)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("EBS");
                
            entity.Property(e => e.GpuModel)
                .HasMaxLength(100);
                
            entity.Property(e => e.VirtualizationType)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("HVM");
                
            entity.Property(e => e.PhysicalProcessor)
                .HasMaxLength(200);
                
            entity.Property(e => e.PerformanceCharacteristics)
                .HasColumnType("jsonb");
                
            entity.HasIndex(e => new { e.ProviderId, e.RegionId, e.TypeCode })
                .IsUnique();
                
            entity.HasOne(e => e.Provider)
                .WithMany()
                .HasForeignKey(e => e.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Region)
                .WithMany(r => r.InstanceTypes)
                .HasForeignKey(e => e.RegionId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Service)
                .WithMany(s => s.InstanceTypes)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // InstancePricing
        builder.Entity<InstancePricing>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("USD");
                
            entity.Property(e => e.OnDemandHourly)
                .HasPrecision(18, 6);
                
            entity.Property(e => e.OnDemandMonthly)
                .HasPrecision(18, 2);
                
            entity.Property(e => e.SpotCurrentPrice)
                .HasPrecision(18, 6);
                
            entity.Property(e => e.SpotAveragePrice)
                .HasPrecision(18, 6);
                
            entity.Property(e => e.SpotMinPrice)
                .HasPrecision(18, 6);
                
            entity.Property(e => e.SpotMaxPrice)
                .HasPrecision(18, 6);
                
            entity.Property(e => e.SpotSavingsPercent)
                .HasPrecision(5, 2);
                
            entity.Property(e => e.Reserved1YearNoUpfront)
                .HasPrecision(18, 6);
                
            entity.Property(e => e.Reserved1YearPartialUpfront)
                .HasPrecision(18, 6);
                
            entity.Property(e => e.Reserved1YearAllUpfront)
                .HasPrecision(18, 6);
                
            entity.Property(e => e.Reserved3YearNoUpfront)
                .HasPrecision(18, 6);
                
            entity.Property(e => e.Reserved3YearPartialUpfront)
                .HasPrecision(18, 6);
                
            entity.Property(e => e.Reserved3YearAllUpfront)
                .HasPrecision(18, 6);
                
            entity.Property(e => e.Reserved1YearSavingsPercent)
                .HasPrecision(5, 2);
                
            entity.Property(e => e.Reserved3YearSavingsPercent)
                .HasPrecision(5, 2);
                
            entity.Property(e => e.StorageGbMonthly)
                .HasPrecision(18, 6);
                
            entity.Property(e => e.DataTransferOutGb)
                .HasPrecision(18, 6);
                
            entity.Property(e => e.DataTransferInGb)
                .HasPrecision(18, 6);
                
            entity.Property(e => e.DataTransferInterRegionGb)
                .HasPrecision(18, 6);
                
            entity.Property(e => e.StaticIpMonthly)
                .HasPrecision(18, 2);
                
            entity.Property(e => e.LoadBalancerHourly)
                .HasPrecision(18, 6);
                
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb");
                
            entity.HasIndex(e => e.EffectiveDate);
            
            entity.HasOne(e => e.InstanceType)
                .WithMany(i => i.Pricing)
                .HasForeignKey(e => e.InstanceTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Discount
        builder.Entity<Discount>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
                
            entity.Property(e => e.DiscountType)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Promotional");
                
            entity.Property(e => e.Conditions)
                .HasColumnType("jsonb");
                
            entity.Property(e => e.DiscountPercent)
                .HasPrecision(5, 2);
                
            entity.Property(e => e.AppliesTo)
                .HasMaxLength(500);
                
            entity.Property(e => e.MinimumSpend)
                .HasPrecision(18, 2);
                
            entity.Property(e => e.MaximumDiscount)
                .HasPrecision(18, 2);
                
            entity.Property(e => e.PromoCode)
                .HasMaxLength(50);
                
            entity.HasOne(e => e.Provider)
                .WithMany(p => p.Discounts)
                .HasForeignKey(e => e.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is Workload && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Added)
            {
                ((Workload)entityEntry.Entity).CreatedAt = DateTime.UtcNow;
            }
            ((Workload)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
        }

        var timestampEntries = ChangeTracker
            .Entries()
            .Where(e => (e.Entity is CloudProvider || 
                         e.Entity is CloudRegion || 
                         e.Entity is Discount) &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in timestampEntries)
        {
            var entity = entityEntry.Entity;
            var updatedAtProperty = entity.GetType().GetProperty("UpdatedAt");
            
            if (entityEntry.State == EntityState.Added)
            {
                var createdAtProperty = entity.GetType().GetProperty("CreatedAt");
                createdAtProperty?.SetValue(entity, DateTime.UtcNow);
            }
            
            updatedAtProperty?.SetValue(entity, DateTime.UtcNow);
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}