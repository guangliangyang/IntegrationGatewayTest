# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY IntegrationGateway.sln .

# Copy project files
COPY src/IntegrationGateway.Api/IntegrationGateway.Api.csproj src/IntegrationGateway.Api/
COPY src/IntegrationGateway.Application/IntegrationGateway.Application.csproj src/IntegrationGateway.Application/
COPY src/IntegrationGateway.Services/IntegrationGateway.Services.csproj src/IntegrationGateway.Services/
COPY src/IntegrationGateway.Models/IntegrationGateway.Models.csproj src/IntegrationGateway.Models/
COPY src/IntegrationGateway.Framework/IntegrationGateway.Framework.csproj src/IntegrationGateway.Framework/

# Restore packages
RUN dotnet restore

# Copy source code
COPY src/ src/

# Build application
RUN dotnet build src/IntegrationGateway.Api/IntegrationGateway.Api.csproj -c Release --no-restore

# Publish application
RUN dotnet publish src/IntegrationGateway.Api/IntegrationGateway.Api.csproj -c Release --no-build -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Set ownership and permissions
RUN chown -R appuser:appuser /app && chmod -R 755 /app

# Switch to non-root user
USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Configure environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "IntegrationGateway.Api.dll"]