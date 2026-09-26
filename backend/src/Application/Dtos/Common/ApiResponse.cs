using System.Text.Json.Serialization;

namespace Application.Dtos.Common;

/// <summary>A successful or failed response containing one data object.</summary>
public record ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null, string? traceId = null) =>
        new() { Success = true, Data = data, Message = message, TraceId = traceId, StatusCode = 200 };

    public static ApiResponse<T> Error(string message, int statusCode = 400, string? traceId = null) =>
        new() { Success = false, Message = message, TraceId = traceId, StatusCode = statusCode };

    public static ApiResponse<T> NotFound(string message = "Resource not found.", string? traceId = null) =>
        Error(message, 404, traceId);

    public static ApiResponse<T> Unauthorized(string message = "Unauthorized.", string? traceId = null) =>
        Error(message, 401, traceId);
}

/// <summary>Pagination metadata for a list response.</summary>
public record ApiPagination
{
    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }

    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; init; }
}

/// <summary>A paginated response containing a list of data objects.</summary>
public record ApiListResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; init; } = true;

    [JsonPropertyName("data")]
    public IReadOnlyList<T> Data { get; init; } = [];

    [JsonPropertyName("pagination")]
    public ApiPagination Pagination { get; init; } = new();

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    public static ApiListResponse<T> Ok(
        IReadOnlyList<T> data,
        ApiPagination pagination,
        string? traceId = null) =>
        new() { Data = data, Pagination = pagination, TraceId = traceId };
}

/// <summary>A machine-readable API error.</summary>
public record ApiError
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}

/// <summary>A standardized error response.</summary>
public record ApiErrorResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<ApiError> Errors { get; init; } = [];

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; init; }

    public static ApiErrorResponse Error(
        string code,
        string message,
        int statusCode = 400,
        string? traceId = null) =>
        new()
        {
            Success = false,
            Errors = [new ApiError { Code = code, Message = message }],
            StatusCode = statusCode,
            TraceId = traceId
        };

    public static ApiErrorResponse NotFound(string message = "Resource not found.", string? traceId = null) =>
        Error("not_found", message, 404, traceId);

    public static ApiErrorResponse Unauthorized(string message = "Unauthorized.", string? traceId = null) =>
        Error("unauthorized", message, 401, traceId);
}
