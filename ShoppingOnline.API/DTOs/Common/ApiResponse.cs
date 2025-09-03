namespace ShoppingOnline.API.DTOs.Common
{
    /// <summary>
    /// Standardized API Response for Frontend Integration
    /// </summary>
    /// <typeparam name="T">Data type</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public object? Errors { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int StatusCode { get; set; }

        // Success response
        public static ApiResponse<T> SuccessResult(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                StatusCode = 200
            };
        }

        // Error response
        public static ApiResponse<T> ErrorResult(string message, int statusCode = 400, object? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors,
                StatusCode = statusCode
            };
        }
    }

    /// <summary>
    /// Paginated response for list endpoints
    /// </summary>
    /// <typeparam name="T">Item type</typeparam>
    public class PaginatedResponse<T>
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = "Success";
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public PaginationMeta Pagination { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class PaginationMeta
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }

    /// <summary>
    /// Authentication response for login/register
    /// </summary>
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserInfo? User { get; set; }
        public string? Token { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public object? Errors { get; set; }
    }

    public class UserInfo
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? RoleName { get; set; }
        public int? RoleId { get; set; }
        public bool IsActive { get; set; }
    }
}
