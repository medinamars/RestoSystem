namespace RestoSystem.Server.Models.Base;

public class BaseEntity : IAuditableEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = "system";
    public bool IsDeleted { get; set; }
}
