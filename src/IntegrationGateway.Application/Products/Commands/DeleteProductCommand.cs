using FluentValidation;
using IntegrationGateway.Application.Common.Behaviours;
using IntegrationGateway.Application.Common.Interfaces;
using IntegrationGateway.Services.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IntegrationGateway.Application.Products.Commands;

/// <summary>
/// Command to delete a product (soft delete)
/// </summary>
public record DeleteProductCommand : IRequest<Unit>
{
    public required string Id { get; init; }

    // NOTE: Cache invalidation removed - relying on 5-second TTL for data freshness
    // This is a demo implementation. Production should use event-driven cache invalidation.
}

/// <summary>
/// Validator for DeleteProductCommand
/// </summary>
public class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Product ID is required")
            .MaximumLength(50)
            .WithMessage("Product ID must not exceed 50 characters");
    }
}

/// <summary>
/// Handler for DeleteProductCommand
/// </summary>
public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Unit>
{
    private readonly IProductService _productService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(
        IProductService productService,
        ICurrentUserService currentUser,
        ILogger<DeleteProductCommandHandler> logger)
    {
        _productService = productService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} deleting product: {ProductId}", 
            _currentUser.UserId ?? "Unknown", request.Id);

        var success = await _productService.DeleteProductAsync(request.Id, cancellationToken);

        if (!success)
        {
            throw new IntegrationGateway.Models.Exceptions.NotFoundException("Product", request.Id);
        }

        _logger.LogInformation("Deleted product: {ProductId}", request.Id);
        return Unit.Value;
    }
}