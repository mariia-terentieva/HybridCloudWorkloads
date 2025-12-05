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
                
            // Новые поля
            entity.Property(w => w.ContainerImage)
                .HasMaxLength(500);
                
            entity.Property(w => w.ExposedPort)
                .HasDefaultValue(80);
                
            entity.Property(w => w.EnvironmentVariables)
                .HasColumnType("jsonb"); // Для PostgreSQL
                
            entity.Property(w => w.DeploymentStatus)
                .HasMaxLength(50);
                
            entity.Property(w => w.ContainerId)
                .HasMaxLength(100);
                
            entity.Property(w => w.AccessUrl)
                .HasMaxLength(500);
                
            // Связь с пользователем
            entity.HasOne<User>()
                .WithMany(u => u.Workloads)
                .HasForeignKey(w => w.UserId)
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