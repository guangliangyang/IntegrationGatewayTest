using System.Diagnostics;
using IntegrationGateway.Models.Exceptions;
using IntegrationGateway.Services.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationGateway.Api.Middleware;

/// <summary>
/// Factory for creating consistent ProblemDetails responses from exceptions
/// </summary>
public static class ProblemDetailsFactory
{
    /// <summary>
    /// Creates a ProblemDetails instance from an exception with consistent formatting
    /// </summary>
    public static ProblemDetails CreateFromException(Exception exception, IWebHostEnvironment environment)
    {
        var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        return exception switch
        {
            BaseApplicationException appEx => CreateFromApplicationException(appEx, traceId, environment),
            TaskCanceledException => CreateTimeoutProblem(traceId),
            ArgumentException argEx => CreateBadRequestProblem(argEx, traceId, environment),
            _ => CreateInternalServerErrorProblem(exception, traceId, environment)
        };
    }

    private static ProblemDetails CreateFromApplicationException(BaseApplicationException appEx, string traceId, IWebHostEnvironment environment)
    {
        var safeMessage = GetSafeErrorMessage(appEx, environment);
        var problemDetails = CreateBaseProblemDetails(appEx.StatusCode, GetTitleForStatusCode(appEx.StatusCode), safeMessage, traceId);
        
        // Standard extensions for all BaseApplicationException
        problemDetails.Extensions["errorType"] = appEx.ErrorType;
        problemDetails.Extensions["errors"] = GetValidationErrors(appEx);
        
        // Special handling for specific exception types with additional data
        switch (appEx)
        {
            case IdempotencyConflictException idempotencyEx:
                problemDetails.Extensions["idempotencyKey"] = idempotencyEx.IdempotencyKey;
                problemDetails.Extensions["operation"] = idempotencyEx.Operation;
                problemDetails.Extensions["expectedBodyHash"] = idempotencyEx.ExpectedBodyHash;
                problemDetails.Extensions["actualBodyHash"] = idempotencyEx.ActualBodyHash;
                break;
                
            case InvalidIdempotencyKeyException invalidKeyEx:
                if (!string.IsNullOrEmpty(invalidKeyEx.IdempotencyKey))
                {
                    problemDetails.Extensions["providedKey"] = invalidKeyEx.IdempotencyKey;
                    problemDetails.Extensions["keyLength"] = invalidKeyEx.IdempotencyKey.Length;
                }
                break;
        }
        
        // Development-only information
        if (environment.IsDevelopment())
        {
            problemDetails.Extensions["innerException"] = appEx.InnerException?.Message;
            problemDetails.Extensions["stackTrace"] = appEx.StackTrace;
        }
        
        return problemDetails;
    }

    private static ProblemDetails CreateTimeoutProblem(string traceId)
    {
        var problemDetails = CreateBaseProblemDetails(408, "Request Timeout", "The request timed out", traceId);
        problemDetails.Extensions["errorType"] = "request_timeout";
        return problemDetails;
    }

    private static ProblemDetails CreateBadRequestProblem(ArgumentException argEx, string traceId, IWebHostEnvironment environment)
    {
        var message = environment.IsDevelopment() ? argEx.Message : "Invalid request parameters";
        var problemDetails = CreateBaseProblemDetails(400, "Bad Request", message, traceId);
        problemDetails.Extensions["errorType"] = "bad_request";
        return problemDetails;
    }

    private static ProblemDetails CreateInternalServerErrorProblem(Exception exception, string traceId, IWebHostEnvironment environment)
    {
        var message = environment.IsDevelopment() ? exception.Message : "An unexpected error occurred";
        var problemDetails = CreateBaseProblemDetails(500, "Internal Server Error", message, traceId);
        problemDetails.Extensions["errorType"] = "internal_server_error";
        
        if (environment.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }
        
        return problemDetails;
    }

    private static ProblemDetails CreateBaseProblemDetails(int statusCode, string title, string detail, string traceId)
    {
        return new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = Activity.Current?.Id,
            Extensions = new Dictionary<string, object?>
            {
                ["traceId"] = traceId,
                ["timestamp"] = DateTime.UtcNow
            }
        };
    }

    private static string GetTitleForStatusCode(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        500 => "Internal Server Error",
        502 => "Bad Gateway",
        503 => "Service Unavailable",
        _ => "Error"
    };

    private static object? GetValidationErrors(BaseApplicationException appEx)
    {
        // Handle ValidationException with detailed error information
        if (appEx is ValidationException validationEx)
        {
            return validationEx.Errors;
        }

        return null;
    }

    private static string GetSafeErrorMessage(BaseApplicationException exception, IWebHostEnvironment environment)
    {
        // For business exceptions, always return the message (they're designed to be user-safe)
        if (exception is ValidationException || 
            exception is NotFoundException || 
            exception is ConflictException || 
            exception is BusinessRuleViolationException ||
            exception is IdempotencyConflictException ||
            exception is MissingIdempotencyKeyException ||
            exception is InvalidIdempotencyKeyException)
        {
            return exception.Message;
        }

        // For security/auth exceptions, return generic messages in production
        if (exception is UnauthorizedException)
        {
            return environment.IsDevelopment() ? exception.Message : "Authentication required";
        }

        if (exception is ForbiddenException)
        {
            return environment.IsDevelopment() ? exception.Message : "Access denied";
        }

        // For external service exceptions, be more cautious in production
        if (exception is ExternalServiceException extEx)
        {
            return environment.IsDevelopment() 
                ? $"External service '{extEx.ServiceName}' error: {exception.Message}"
                : "External service temporarily unavailable";
        }

        // For service unavailable, return generic message
        if (exception is ServiceUnavailableException)
        {
            return environment.IsDevelopment() ? exception.Message : "Service temporarily unavailable";
        }

        // Default: return the message as-is (should not happen with proper exception design)
        return exception.Message;
    }
}