namespace StudioManager.Application;

public sealed record Result(bool Success, string Code, string Message)
{
    public static Result Ok(string message = "Thao tác thành công.") => new(true, "OK", message);
    public static Result Fail(string code, string message) => new(false, code, message);
}
public sealed record Result<T>(bool Success, string Code, string Message, T? Data)
{
    public static Result<T> Ok(T data, string message = "Thao tác thành công.") => new(true, "OK", message, data);
    public static Result<T> Fail(string code, string message) => new(false, code, message, default);
}

public interface IPasswordHasher { string Hash(string password); bool Verify(string password, string encoded); }
public interface IClock { DateTime Now { get; } }
public sealed class SystemClock : IClock { public DateTime Now => DateTime.Now; }
