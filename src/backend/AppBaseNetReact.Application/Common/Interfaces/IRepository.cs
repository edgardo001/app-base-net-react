using System.Linq.Expressions;

namespace AppBaseNetReact.Application.Common.Interfaces;

// Se restringe T a BaseEntity para que GenericRepository pueda usar
// SoftDelete, CreatedAt/UpdatedAt, y metodos genericos de BaseEntity
// Sin esta constraint, el compilador no resuelve T? correctamente
// y genera CS0738 al implementar la interfaz desde GenericRepository<T>
public interface IRepository<T> where T : AppBaseNetReact.Domain.Common.BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<PagedResult<T>> GetPagedAsync(
        int page, int pageSize,
        Expression<Func<T, bool>>? filter = null,
        string? sortBy = null, bool sortDesc = false,
        string? searchTerm = null,
        CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default);
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
