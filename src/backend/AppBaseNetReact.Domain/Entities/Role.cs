using AppBaseNetReact.Domain.Common;

namespace AppBaseNetReact.Domain.Entities;

public sealed class Role : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsSystem { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; } = [];
    public ICollection<RolePermission> RolePermissions { get; private set; } = [];

    private Role() { }

    public static Role Create(string name, string description, bool isSystem = false, Guid? createdBy = null)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Description = description,
            IsSystem = isSystem,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Update(string name, string description, Guid? updatedBy = null)
    {
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Description = description;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
