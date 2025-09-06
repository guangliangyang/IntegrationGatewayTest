using FluentValidation;
using IntegrationGateway.Application.Common.Interfaces;
using IntegrationGateway.Models.DTOs;
using IntegrationGateway.Services.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IntegrationGateway.Application.Products.Commands;

/// <summary>
/// Command to update an existing product (V2 - returns enhanced ProductV2Dto)
/// </summary>
public record UpdateProductV2Command : IRequest<ProductV2Dto>
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal Price { get; init; }
    public required string Category { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Validator for UpdateProductV2Command
/// </summary>
public class UpdateProductV2CommandValidator : AbstractValidator<UpdateProductV2Command>
{
    public UpdateProductV2CommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Product ID is required")
            .MaximumLength(50)
            .WithMessage("Product ID must not exceed 50 characters");

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
/// Handler for UpdateProductV2Command
/// </summary>
public class UpdateProductV2CommandHandler : IRequestHandler<UpdateProductV2Command, ProductV2Dto>
{
    private readonly IProductService _productService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UpdateProductV2CommandHandler> _logger;

    public UpdateProductV2CommandHandler(
        IProductService productService,
        ICurrentUserService currentUser,
        ILogger<UpdateProductV2CommandHandler> logger)
    {
        _productService = productService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ProductV2Dto> Handle(UpdateProductV2Command request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} updating product V2: {ProductId}", 
            _currentUser.UserId ?? "Unknown", request.Id);

        // Update the product and get V2 response directly
        var productV2 = await _productService.UpdateProductV2Async(request.Id, new UpdateProductRequest
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category,
            IsActive = request.IsActive
        }, cancellationToken);

        _logger.LogInformation("Updated product V2: {ProductId}", request.Id);
        return productV2;
    }
}