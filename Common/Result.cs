namespace AmsaAPI.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    BadRequest,
    Unauthorized,
    Forbidden
}

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public ErrorType ErrorType { get; }
    public string ErrorMessage { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        ErrorType = default;
        ErrorMessage = string.Empty;
    }

    private Result(ErrorType errorType, string errorMessage)
    {
        IsSuccess = false;
        Value = default;
        ErrorType = errorType;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(ErrorType errorType, string errorMessage) => new(errorType, errorMessage);

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<ErrorType, string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(ErrorType, ErrorMessage);
    }
}

public static class Result
{
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(ErrorType errorType, string errorMessage) => Result<T>.Failure(errorType, errorMessage);
    
    // Convenience methods for common error types
    public static Result<T> NotFound<T>(string message = "Resource not found") => Failure<T>(ErrorType.NotFound, message);
    public static Result<T> BadRequest<T>(string message) => Failure<T>(ErrorType.BadRequest, message);
    public static Result<T> Validation<T>(string message) => Failure<T>(ErrorType.Validation, message);
    public static Result<T> Conflict<T>(string message) => Failure<T>(ErrorType.Conflict, message);
}