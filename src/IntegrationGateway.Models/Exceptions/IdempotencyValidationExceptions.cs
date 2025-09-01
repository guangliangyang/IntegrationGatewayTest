namespace IntegrationGateway.Models.Exceptions;

/// <summary>
/// Exception thrown when Idempotency-Key header is missing for POST/PUT requests
/// </summary>
public class MissingIdempotencyKeyException : BaseApplicationException
{
    public override int StatusCode => 400;
    public override string ErrorType => "missing_idempotency_key";

    public MissingIdempotencyKeyException() 
        : base("Idempotency-Key header is required for POST and PUT requests")
    {
    }

    public MissingIdempotencyKeyException(string message) : base(message)
    {
    }

    public MissingIdempotencyKeyException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when Idempotency-Key header has invalid format
/// </summary>
public class InvalidIdempotencyKeyException : BaseApplicationException
{
    public override int StatusCode => 400;
    public override string ErrorType => "invalid_idempotency_key";

    public string? IdempotencyKey { get; }

    public InvalidIdempotencyKeyException(string idempotencyKey) 
        : base("Idempotency-Key header must be between 16 and 128 characters")
    {
        IdempotencyKey = idempotencyKey;
    }

    public InvalidIdempotencyKeyException(string message, string? idempotencyKey) 
        : base(message)
    {
        IdempotencyKey = idempotencyKey;
    }

    public InvalidIdempotencyKeyException(string message, string? idempotencyKey, Exception innerException) 
        : base(message, innerException)
    {
        IdempotencyKey = idempotencyKey;
    }
}