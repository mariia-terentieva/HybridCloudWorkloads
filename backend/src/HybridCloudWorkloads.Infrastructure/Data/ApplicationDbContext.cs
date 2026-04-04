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
    public DbSet<PerformanceMetric> PerformanceMetrics { get; set; } // Добавлено

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

        // НОВАЯ конфигурация для PerformanceMetric
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

        return base.SaveChangesAsync(cancellationToken);
    }
}