using Microsoft.AspNetCore.Identity;
using HybridCloudWorkloads.Core.Entities;

namespace HybridCloudWorkloads.Infrastructure.Entities;

public class User : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual ICollection<Workload> Workloads { get; set; } = new List<Workload>();
}