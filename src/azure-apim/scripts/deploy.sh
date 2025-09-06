#!/bin/bash

# Azure API Management Deployment Script
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

# Function to display usage
usage() {
    echo "Usage: $0 -e <environment> -g <resource-group> [-s <subscription-id>] [-l <location>]"
    echo "  -e, --environment    Environment (dev, staging, prod)"
    echo "  -g, --resource-group Resource group name"
    echo "  -s, --subscription   Subscription ID (optional)"
    echo "  -l, --location       Location (default: East US 2)"
    echo "  -h, --help          Show this help message"
    exit 1
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -g|--resource-group)
            RESOURCE_GROUP="$2"
            shift 2
            ;;
        -s|--subscription)
            SUBSCRIPTION_ID="$2"
            shift 2
            ;;
        -l|--location)
            LOCATION="$2"
            shift 2
            ;;
        -h|--help)
            usage
            ;;
        *)
            echo "Unknown option $1"
            usage
            ;;
    esac
done

# Set default values
LOCATION=${LOCATION:-"East US 2"}

# Validate required parameters
if [[ -z "$ENVIRONMENT" || -z "$RESOURCE_GROUP" ]]; then
    echo -e "${RED}Error: Environment and Resource Group are required${NC}"
    usage
fi

# Validate environment
if [[ ! "$ENVIRONMENT" =~ ^(dev|staging|prod)$ ]]; then
    echo -e "${RED}Error: Environment must be one of: dev, staging, prod${NC}"
    exit 1
fi

echo -e "${GREEN}🚀 Starting Azure API Management deployment for environment: $ENVIRONMENT${NC}"

# Set subscription if provided
if [[ -n "$SUBSCRIPTION_ID" ]]; then
    echo -e "${YELLOW}📋 Setting subscription context to: $SUBSCRIPTION_ID${NC}"
    az account set --subscription "$SUBSCRIPTION_ID"
fi

# Get current subscription info
CURRENT_SUBSCRIPTION=$(az account show --query "{subscriptionId: id, name: name}" --output json)
SUBSCRIPTION_NAME=$(echo "$CURRENT_SUBSCRIPTION" | jq -r '.name')
SUBSCRIPTION_ID_ACTUAL=$(echo "$CURRENT_SUBSCRIPTION" | jq -r '.subscriptionId')
echo -e "${CYAN}📋 Current subscription: $SUBSCRIPTION_NAME ($SUBSCRIPTION_ID_ACTUAL)${NC}"

# Create resource group if it doesn't exist
echo -e "${YELLOW}🏗️ Ensuring resource group exists: $RESOURCE_GROUP${NC}"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BICEP_FILE="$SCRIPT_DIR/../bicep/main.bicep"
PARAMETER_FILE="$SCRIPT_DIR/../bicep/parameters/$ENVIRONMENT.bicepparam"

# Validate files exist
if [[ ! -f "$BICEP_FILE" ]]; then
    echo -e "${RED}Error: Bicep template not found: $BICEP_FILE${NC}"
    exit 1
fi

if [[ ! -f "$PARAMETER_FILE" ]]; then
    echo -e "${RED}Error: Parameter file not found: $PARAMETER_FILE${NC}"
    exit 1
fi

# Validate Bicep template
echo -e "${YELLOW}✅ Validating Bicep template...${NC}"
az deployment group validate \
    --resource-group "$RESOURCE_GROUP" \
    --template-file "$BICEP_FILE" \
    --parameters "$PARAMETER_FILE" \
    --output none

echo -e "${GREEN}✅ Template validation successful${NC}"

# Deploy infrastructure
echo -e "${YELLOW}🚀 Deploying Azure API Management infrastructure...${NC}"
DEPLOYMENT_NAME="apim-deployment-$(date +%Y%m%d-%H%M%S)"

DEPLOYMENT_RESULT=$(az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --template-file "$BICEP_FILE" \
    --parameters "$PARAMETER_FILE" \
    --name "$DEPLOYMENT_NAME" \
    --query "{apimServiceUrl: properties.outputs.apimServiceUrl.value, apimManagementUrl: properties.outputs.apimManagementUrl.value, apimGatewayUrl: properties.outputs.apimGatewayUrl.value}" \
    --output json)

# Parse outputs
APIM_SERVICE_URL=$(echo "$DEPLOYMENT_RESULT" | jq -r '.apimServiceUrl')
APIM_MANAGEMENT_URL=$(echo "$DEPLOYMENT_RESULT" | jq -r '.apimManagementUrl')
APIM_GATEWAY_URL=$(echo "$DEPLOYMENT_RESULT" | jq -r '.apimGatewayUrl')

echo -e "${GREEN}✅ Deployment completed successfully!${NC}"

# Display deployment outputs
echo -e "\n${CYAN}📊 Deployment Outputs:${NC}"
echo -e "  ${WHITE}🌐 APIM Service URL:    $APIM_SERVICE_URL${NC}"
echo -e "  ${WHITE}🔧 APIM Management URL: $APIM_MANAGEMENT_URL${NC}"
echo -e "  ${WHITE}🚪 APIM Gateway URL:    $APIM_GATEWAY_URL${NC}"

# Check deployment status
echo -e "\n${YELLOW}🔍 Checking API Management service status...${NC}"
APIM_NAME="apim-integration-gateway-$ENVIRONMENT"

APIM_STATUS=$(az apim show --name "$APIM_NAME" --resource-group "$RESOURCE_GROUP" --query "provisioningState" --output tsv)
if [[ "$APIM_STATUS" == "Succeeded" ]]; then
    echo -e "  ${GREEN}📊 APIM Status: $APIM_STATUS${NC}"
    echo -e "\n${GREEN}🎉 API Management deployment completed successfully!${NC}"
    echo -e "   ${CYAN}You can now configure your APIs and policies through the Azure portal or CLI.${NC}"
else
    echo -e "  ${YELLOW}📊 APIM Status: $APIM_STATUS${NC}"
    echo -e "\n${YELLOW}⏳ API Management is still provisioning. This can take 30-45 minutes.${NC}"
    echo -e "   ${CYAN}Check the Azure portal for provisioning progress.${NC}"
fi

echo -e "\n${YELLOW}🔗 Next Steps:${NC}"
echo -e "  ${WHITE}1. Configure your backend Integration Gateway service URL${NC}"
echo -e "  ${WHITE}2. Test the API endpoints through the APIM gateway${NC}"
echo -e "  ${WHITE}3. Configure additional policies as needed${NC}"
echo -e "  ${WHITE}4. Set up monitoring and alerting${NC}"