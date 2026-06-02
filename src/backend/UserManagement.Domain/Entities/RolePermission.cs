namespace UserManagement.Domain.Entities;

public sealed class RolePermission
{
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;
    public Guid PermissionId { get; private set; }
    public Permission Permission { get; private set; } = null!;
    public bool Granted { get; private set; }

    private RolePermission() { }

    public static RolePermission Create(Guid roleId, Guid permissionId, bool granted = true)
    {
        return new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            Granted = granted
        };
    }

    public void SetGranted(bool granted)
    {
        Granted = granted;
    }
}
