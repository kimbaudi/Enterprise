namespace Enterprise.WebApi.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public ApiResponse()
    {
        Success = true;
    }

    public ApiResponse(T data, string? message = null)
    {
        Success = true;
        Data = data;
        Message = message;
    }

    public ApiResponse(string error)
    {
        Success = false;
        Errors = new List<string> { error };
    }

    public ApiResponse(List<string> errors)
    {
        Success = false;
        Errors = errors;
    }
}
