using UserManagement.Domain.Common;

namespace UserManagement.Domain.Entities;

public sealed class Permission : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; private set; } = [];

    private Permission() { }

    public static Permission Create(string code, string name, string module, string description)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Module = module,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }
}
