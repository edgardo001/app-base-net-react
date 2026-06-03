namespace AppBaseNetReact.Domain.Entities;

public sealed class UserRole
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;

    private UserRole() { }

    public static UserRole Create(Guid userId, Guid roleId)
    {
        return new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };
    }
}
