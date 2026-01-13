using EnterpriseApi.Domain.Common;

namespace EnterpriseApi.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Navigation property
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
