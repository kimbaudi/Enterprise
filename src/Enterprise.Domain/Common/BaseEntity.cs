using Enterprise.Domain.Interfaces;

namespace Enterprise.Domain.Common;

public abstract class BaseEntity : IAuditableEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
