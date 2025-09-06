using FluentValidation;
using IntegrationGateway.Application.Common.Interfaces;
using IntegrationGateway.Models.DTOs;
using IntegrationGateway.Services.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IntegrationGateway.Application.Products.Commands;

/// <summary>
/// Command to create a new product (V2 - returns enhanced ProductV2Dto)
/// </summary>
public record CreateProductV2Command : IRequest<ProductV2Dto>
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal Price { get; init; }
    public required string Category { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Validator for CreateProductV2Command
/// </summary>
public class CreateProductV2CommandValidator : AbstractValidator<CreateProductV2Command>
{
    public CreateProductV2CommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required")
            .MaximumLength(100)
            .WithMessage("Product name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Product description is required")
            .MaximumLength(500)
            .WithMessage("Product description must not exceed 500 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Product price must be greater than 0")
            .LessThan(1000000)
            .WithMessage("Product price must be less than 1,000,000");

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Product category is required")
            .MaximumLength(50)
            .WithMessage("Product category must not exceed 50 characters");
    }
}

/// <summary>
/// Handler for CreateProductV2Command
/// </summary>
public class CreateProductV2CommandHandler : IRequestHandler<CreateProductV2Command, ProductV2Dto>
{
    private readonly IProductService _productService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreateProductV2CommandHandler> _logger;

    public CreateProductV2CommandHandler(
        IProductService productService,
        ICurrentUserService currentUser,
        ILogger<CreateProductV2CommandHandler> logger)
    {
        _productService = productService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ProductV2Dto> Handle(CreateProductV2Command request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} creating product V2: {ProductName}", 
            _currentUser.UserId ?? "Unknown", request.Name);

        // Create the product and get V2 response directly
        var productV2 = await _productService.CreateProductV2Async(new CreateProductRequest
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category,
            IsActive = request.IsActive
        }, cancellationToken);

        _logger.LogInformation("Created product V2: {ProductId}", productV2.Id);
        return productV2;
    }
}