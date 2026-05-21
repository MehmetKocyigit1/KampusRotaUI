namespace KampusRota.Api.DTOs;

public record ServiceResult<T>(bool Success, string Message, T? Data)
{
    public static ServiceResult<T> Ok(T data, string message = "Islem basarili.")
        => new(true, message, data);

    public static ServiceResult<T> Fail(string message)
        => new(false, message, default);
}
