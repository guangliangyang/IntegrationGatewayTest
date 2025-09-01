using System.Diagnostics;
using System.Net;
using System.Text.Json;
using IntegrationGateway.Models.Exceptions;
using IntegrationGateway.Services.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationGateway.Api.Middleware;

/// <summary>
/// Global exception handling middleware that converts exceptions to appropriate HTTP responses
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = CreateErrorResponse(exception);
        
        // Log the exception with appropriate level
        LogException(exception, response.Status ?? 500);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.Status ?? 500;

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private ProblemDetails CreateErrorResponse(Exception exception)
    {
        return ProblemDetailsFactory.CreateFromException(exception, _environment);
    }


    private void LogException(Exception exception, int statusCode)
    {
        var logLevel = statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, exception, 
            "HTTP {StatusCode} - {ExceptionType}: {Message}",
            statusCode, exception.GetType().Name, exception.Message);
    }
}

/// <summary>
/// Extension methods for registering the global exception handling middleware
/// </summary>
public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}