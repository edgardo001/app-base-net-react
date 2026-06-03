namespace AppBaseNetReact.WebApi.Filters;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<ApiError>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<T> Fail(string message, List<ApiError>? errors = null) => new()
    {
        Success = false,
        Message = message,
        Errors = errors
    };
}

public class ApiError
{
    public string Property { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
