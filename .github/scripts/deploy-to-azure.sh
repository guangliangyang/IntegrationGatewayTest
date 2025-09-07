#!/bin/bash

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-integration-gateway-rg}"
CONTAINER_APP_NAME="${CONTAINER_APP_NAME:-integration-gateway}"
LOCATION="${AZURE_LOCATION:-East US}"
ENVIRONMENT_NAME="${CONTAINER_APP_ENV:-integration-gateway-env}"
IMAGE_NAME="${1:-ghcr.io/your-org/integration-gateway:latest}"

echo -e "${BLUE}🚀 Starting Azure deployment...${NC}"
echo -e "${BLUE}Resource Group: ${RESOURCE_GROUP}${NC}"
echo -e "${BLUE}Container App: ${CONTAINER_APP_NAME}${NC}"
echo -e "${BLUE}Image: ${IMAGE_NAME}${NC}"

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}❌ Azure CLI is not installed${NC}"
    exit 1
fi

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    echo -e "${RED}❌ Not logged in to Azure. Please run 'az login'${NC}"
    exit 1
fi

# Create resource group if it doesn't exist
echo -e "${YELLOW}📦 Creating resource group...${NC}"
az group create \
    --name "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --output none

# Create Container Apps environment if it doesn't exist
echo -e "${YELLOW}🌍 Creating Container Apps environment...${NC}"
if ! az containerapp env show --name "$ENVIRONMENT_NAME" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
    az containerapp env create \
        --name "$ENVIRONMENT_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --location "$LOCATION" \
        --output none
fi

# Deploy or update the container app
echo -e "${YELLOW}🚢 Deploying container app...${NC}"

# Check if container app exists
if az containerapp show --name "$CONTAINER_APP_NAME" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
    echo -e "${YELLOW}📝 Updating existing container app...${NC}"
    az containerapp update \
        --name "$CONTAINER_APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --image "$IMAGE_NAME" \
        --output none
else
    echo -e "${YELLOW}🆕 Creating new container app...${NC}"
    az containerapp create \
        --name "$CONTAINER_APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --environment "$ENVIRONMENT_NAME" \
        --image "$IMAGE_NAME" \
        --target-port 8080 \
        --ingress external \
        --min-replicas 1 \
        --max-replicas 10 \
        --cpu 0.5 \
        --memory 1Gi \
        --env-vars \
            ASPNETCORE_ENVIRONMENT=Production \
            ASPNETCORE_URLS=http://+:8080 \
        --output none
fi

# Get the app URL
echo -e "${YELLOW}🔍 Getting application URL...${NC}"
APP_URL=$(az containerapp show \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query "properties.configuration.ingress.fqdn" \
    --output tsv)

if [ -n "$APP_URL" ]; then
    echo -e "${GREEN}✅ Deployment successful!${NC}"
    echo -e "${GREEN}🌐 Application URL: https://${APP_URL}${NC}"
    echo -e "${GREEN}🏥 Health Check: https://${APP_URL}/health${NC}"
    echo -e "${GREEN}📚 API Docs: https://${APP_URL}/swagger${NC}"
    
    # Test the health endpoint
    echo -e "${YELLOW}🏥 Testing health endpoint...${NC}"
    sleep 30  # Wait for app to start
    
    if curl -f "https://${APP_URL}/health" &> /dev/null; then
        echo -e "${GREEN}✅ Health check passed!${NC}"
    else
        echo -e "${YELLOW}⚠️ Health check failed, but deployment completed${NC}"
    fi
else
    echo -e "${RED}❌ Failed to get application URL${NC}"
    exit 1
fi

echo -e "${GREEN}🎉 Deployment completed successfully!${NC}"