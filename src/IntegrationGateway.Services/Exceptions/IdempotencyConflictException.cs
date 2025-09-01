using IntegrationGateway.Models.Exceptions;

namespace IntegrationGateway.Services.Exceptions;

public class IdempotencyConflictException : BaseApplicationException
{
    public override int StatusCode => 409;
    public override string ErrorType => "idempotency_conflict";
    
    public string IdempotencyKey { get; }
    public string Operation { get; }
    public string ExpectedBodyHash { get; }
    public string ActualBodyHash { get; }

    public IdempotencyConflictException(
        string idempotencyKey, 
        string operation, 
        string expectedBodyHash, 
        string actualBodyHash) 
        : base($"Idempotency key '{idempotencyKey}' for operation '{operation}' conflicts with existing request. " +
               "Same idempotency key cannot be used for different request bodies.")
    {
        IdempotencyKey = idempotencyKey;
        Operation = operation;
        ExpectedBodyHash = expectedBodyHash;
        ActualBodyHash = actualBodyHash;
    }

    public IdempotencyConflictException(
        string idempotencyKey, 
        string operation, 
        string expectedBodyHash, 
        string actualBodyHash, 
        Exception innerException) 
        : base($"Idempotency key '{idempotencyKey}' for operation '{operation}' conflicts with existing request. " +
               "Same idempotency key cannot be used for different request bodies.", innerException)
    {
        IdempotencyKey = idempotencyKey;
        Operation = operation;
        ExpectedBodyHash = expectedBodyHash;
        ActualBodyHash = actualBodyHash;
    }
}