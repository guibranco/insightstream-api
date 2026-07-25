namespace InsightStream.Core.Dtos;

public class ApiResponse<T>
{
    public bool Success { get; init; }

    public T? Data { get; init; }

    public string? Message { get; init; }

    public PaginationInfo? Pagination { get; init; }

    public static ApiResponse<T> Ok(T data, PaginationInfo? pagination = null) =>
        new() { Success = true, Data = data, Pagination = pagination };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}

public class ApiResponse
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public static ApiResponse Ok(string? message = null) => new() { Success = true, Message = message };

    public static ApiResponse Fail(string message) => new() { Success = false, Message = message };
}
