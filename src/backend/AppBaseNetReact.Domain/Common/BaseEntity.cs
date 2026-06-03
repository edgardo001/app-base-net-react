namespace AppBaseNetReact.Domain.Common;

// BaseEntity es la clase base abstracta para todas las entidades del dominio.
// GUID como PK: evita roundtrips a la DB para generar IDs y previene enumeracion de recursos via IDs secuenciales.
// ConcurrencyToken (byte[]): permite optimistic concurrency a nivel de EF Core para prevenir lost updates.
// Soft delete (DeletedAt): los registros nunca se eliminan fisicamente; las queries global filters los excluyen automaticamente.
// protected set: las propiedades solo se modifican via metodos del dominio (comportamiento), no directamente.
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
    public Guid? UpdatedBy { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }
    public byte[] ConcurrencyToken { get; protected set; } = [];

    public void SoftDelete(Guid? deletedBy)
    {
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = deletedBy;
    }
}
