namespace Enterprise.WebApi.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public string? RequestId { get; set; }
    public DateTime Timestamp { get; set; }

    public ApiResponse()
    {
        Success = true;
        Timestamp = DateTime.UtcNow;
    }

    public ApiResponse(T data, string? message = null)
    {
        Success = true;
        Data = data;
        Message = message;
        Timestamp = DateTime.UtcNow;
    }

    public ApiResponse(string error)
    {
        Success = false;
        Errors = new List<string> { error };
        Timestamp = DateTime.UtcNow;
    }

    public ApiResponse(List<string> errors)
    {
        Success = false;
        Errors = errors;
        Timestamp = DateTime.UtcNow;
    }
}
