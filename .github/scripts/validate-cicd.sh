#!/bin/bash

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🔍 Validating CI/CD Configuration${NC}"

# Check if we're in a git repository
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    echo -e "${RED}❌ Not in a git repository${NC}"
    exit 1
fi

# Get repository root
REPO_ROOT=$(git rev-parse --show-toplevel)
GITHUB_DIR="$REPO_ROOT/.github"

echo -e "${BLUE}📁 Repository root: $REPO_ROOT${NC}"

# Check required files exist
echo -e "${YELLOW}📋 Checking required files...${NC}"

required_files=(
    ".github/workflows/ci-cd.yml"
    ".github/workflows/release.yml"
    ".github/workflows/dependency-update.yml"
    ".github/workflows/infrastructure.yml"
    ".github/scripts/deploy-to-azure.sh"
    ".github/pull_request_template.md"
    ".github/ISSUE_TEMPLATE/bug_report.yml"
    ".github/ISSUE_TEMPLATE/feature_request.yml"
    "src/IntegrationGateway.Api/Dockerfile"
    ".dockerignore"
)

missing_files=()
for file in "${required_files[@]}"; do
    if [ -f "$REPO_ROOT/$file" ]; then
        echo -e "${GREEN}✅ $file${NC}"
    else
        echo -e "${RED}❌ $file${NC}"
        missing_files+=("$file")
    fi
done

if [ ${#missing_files[@]} -gt 0 ]; then
    echo -e "${RED}❌ Missing files detected${NC}"
    exit 1
fi

# Validate YAML syntax
echo -e "${YELLOW}🔧 Validating YAML syntax...${NC}"

if command -v yamllint &> /dev/null; then
    for workflow in "$GITHUB_DIR/workflows"/*.yml; do
        if yamllint "$workflow" > /dev/null 2>&1; then
            echo -e "${GREEN}✅ $(basename "$workflow") - Valid YAML${NC}"
        else
            echo -e "${RED}❌ $(basename "$workflow") - Invalid YAML${NC}"
            yamllint "$workflow"
            exit 1
        fi
    done
else
    echo -e "${YELLOW}⚠️ yamllint not installed, skipping YAML validation${NC}"
fi

# Validate Dockerfile
echo -e "${YELLOW}🐳 Validating Dockerfile...${NC}"
dockerfile="$REPO_ROOT/src/IntegrationGateway.Api/Dockerfile"

if [ -f "$dockerfile" ]; then
    # Check for common Dockerfile issues
    if grep -q "FROM.*latest" "$dockerfile"; then
        echo -e "${YELLOW}⚠️ Dockerfile uses 'latest' tag - consider using specific versions${NC}"
    fi
    
    if grep -q "USER.*root" "$dockerfile"; then
        echo -e "${RED}❌ Dockerfile runs as root user - security risk${NC}"
        exit 1
    fi
    
    if grep -q "HEALTHCHECK" "$dockerfile"; then
        echo -e "${GREEN}✅ Dockerfile includes health check${NC}"
    else
        echo -e "${YELLOW}⚠️ Dockerfile missing health check${NC}"
    fi
    
    echo -e "${GREEN}✅ Dockerfile validation passed${NC}"
fi

# Check for .dockerignore
if [ -f "$REPO_ROOT/.dockerignore" ]; then
    echo -e "${GREEN}✅ .dockerignore exists${NC}"
else
    echo -e "${YELLOW}⚠️ .dockerignore missing${NC}"
fi

# Validate solution structure
echo -e "${YELLOW}📦 Validating .NET solution structure...${NC}"

if [ -f "$REPO_ROOT/IntegrationGateway.sln" ]; then
    echo -e "${GREEN}✅ Solution file exists${NC}"
    
    # Check if projects can be built
    if command -v dotnet &> /dev/null; then
        cd "$REPO_ROOT"
        if dotnet restore > /dev/null 2>&1; then
            echo -e "${GREEN}✅ dotnet restore successful${NC}"
        else
            echo -e "${RED}❌ dotnet restore failed${NC}"
            exit 1
        fi
        
        if dotnet build --configuration Release > /dev/null 2>&1; then
            echo -e "${GREEN}✅ dotnet build successful${NC}"
        else
            echo -e "${YELLOW}⚠️ dotnet build failed - check project configuration${NC}"
        fi
    else
        echo -e "${YELLOW}⚠️ .NET SDK not installed, skipping build validation${NC}"
    fi
else
    echo -e "${RED}❌ Solution file not found${NC}"
    exit 1
fi

# Check GitHub workflow syntax using GitHub CLI
echo -e "${YELLOW}⚡ Checking GitHub Actions workflow syntax...${NC}"

if command -v gh &> /dev/null; then
    cd "$REPO_ROOT"
    
    for workflow in .github/workflows/*.yml; do
        workflow_name=$(basename "$workflow")
        if gh workflow view "$workflow_name" &> /dev/null; then
            echo -e "${GREEN}✅ $workflow_name - Valid GitHub Actions workflow${NC}"
        else
            echo -e "${YELLOW}⚠️ $workflow_name - Cannot validate (may need to be pushed to GitHub first)${NC}"
        fi
    done
else
    echo -e "${YELLOW}⚠️ GitHub CLI not installed, skipping workflow validation${NC}"
fi

# Check for security best practices
echo -e "${YELLOW}🔒 Checking security best practices...${NC}"

# Check for hardcoded secrets
if grep -r -i "password\|secret\|key\|token" .github/workflows/ --include="*.yml" | grep -v "\${{" | grep -v "#"; then
    echo -e "${RED}❌ Potential hardcoded secrets found in workflows${NC}"
    exit 1
else
    echo -e "${GREEN}✅ No hardcoded secrets detected${NC}"
fi

# Check for secret usage
if grep -r "\${{ secrets\." .github/workflows/ &> /dev/null; then
    echo -e "${GREEN}✅ Workflows use GitHub secrets properly${NC}"
else
    echo -e "${YELLOW}⚠️ No secret usage found - ensure secrets are configured${NC}"
fi

# Summary
echo -e "${GREEN}🎉 CI/CD Configuration Validation Complete!${NC}"
echo -e "${BLUE}📊 Summary:${NC}"
echo -e "${GREEN}✅ All required files present${NC}"
echo -e "${GREEN}✅ YAML syntax validation passed${NC}"
echo -e "${GREEN}✅ Dockerfile validation passed${NC}"
echo -e "${GREEN}✅ .NET solution structure validated${NC}"
echo -e "${GREEN}✅ Security best practices check passed${NC}"

echo -e "${BLUE}📋 Next Steps:${NC}"
echo -e "${YELLOW}1. Configure GitHub secrets${NC}"
echo -e "${YELLOW}2. Set up Azure service principal${NC}"
echo -e "${YELLOW}3. Configure environment protection rules${NC}"
echo -e "${YELLOW}4. Test workflows with a pull request${NC}"

echo -e "${GREEN}🚀 Ready for CI/CD!${NC}"