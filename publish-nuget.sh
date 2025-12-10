#!/bin/bash

# DotNetAspects NuGet Publishing Script
# Usage: ./publish-nuget.sh [version] [configuration]
# Example: ./publish-nuget.sh 1.2.0 Release

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Configuration
CONFIGURATION="${2:-Release}"
NUGET_SOURCE="https://api.nuget.org/v3/index.json"

# Load API key from .env file if exists
if [ -f ".env" ]; then
    source .env
fi

# Check for API key
if [ -z "$NUGET_API_KEY" ]; then
    echo -e "${YELLOW}NUGET_API_KEY not found in environment or .env file${NC}"
    read -sp "Enter NuGet API Key: " NUGET_API_KEY
    echo ""
fi

if [ -z "$NUGET_API_KEY" ]; then
    echo -e "${RED}Error: NuGet API key is required${NC}"
    exit 1
fi

# Get version from parameter or prompt
VERSION="$1"
if [ -z "$VERSION" ]; then
    # Get current version from csproj
    CURRENT_VERSION=$(grep -oP '(?<=<Version>)[^<]+' src/DotNetAspects/DotNetAspects.csproj | head -1)
    echo -e "${YELLOW}Current version: ${CURRENT_VERSION}${NC}"
    read -p "Enter new version (or press Enter to keep current): " VERSION
    if [ -z "$VERSION" ]; then
        VERSION="$CURRENT_VERSION"
    fi
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  DotNetAspects NuGet Publisher${NC}"
echo -e "${GREEN}========================================${NC}"
echo -e "Version: ${YELLOW}${VERSION}${NC}"
echo -e "Configuration: ${YELLOW}${CONFIGURATION}${NC}"
echo ""

# Update version in csproj files
echo -e "${YELLOW}[1/6] Updating version in project files...${NC}"
sed -i "s|<Version>[^<]*</Version>|<Version>${VERSION}</Version>|g" src/DotNetAspects/DotNetAspects.csproj
sed -i "s|<Version>[^<]*</Version>|<Version>${VERSION}</Version>|g" src/DotNetAspects.Fody/DotNetAspects.Fody.csproj
echo -e "${GREEN}Version updated to ${VERSION}${NC}"

# Clean previous builds
echo ""
echo -e "${YELLOW}[2/6] Cleaning previous builds...${NC}"
rm -rf src/DotNetAspects/bin src/DotNetAspects/obj
rm -rf src/DotNetAspects.Fody/bin src/DotNetAspects.Fody/obj
echo -e "${GREEN}Clean completed${NC}"

# Build DotNetAspects.Fody first (required for the unified package)
echo ""
echo -e "${YELLOW}[3/6] Building DotNetAspects.Fody...${NC}"
dotnet build src/DotNetAspects.Fody/DotNetAspects.Fody.csproj -c "$CONFIGURATION"
if [ $? -ne 0 ]; then
    echo -e "${RED}Error: DotNetAspects.Fody build failed${NC}"
    exit 1
fi
echo -e "${GREEN}DotNetAspects.Fody build completed${NC}"

# Build and pack DotNetAspects (unified package)
echo ""
echo -e "${YELLOW}[4/6] Building and packing DotNetAspects...${NC}"
dotnet build src/DotNetAspects/DotNetAspects.csproj -c "$CONFIGURATION"
if [ $? -ne 0 ]; then
    echo -e "${RED}Error: DotNetAspects build failed${NC}"
    exit 1
fi

dotnet pack src/DotNetAspects/DotNetAspects.csproj -c "$CONFIGURATION" --no-build
if [ $? -ne 0 ]; then
    echo -e "${RED}Error: Package creation failed${NC}"
    exit 1
fi
echo -e "${GREEN}Package created successfully${NC}"

# Find the package file
PACKAGE_FILE="src/DotNetAspects/bin/${CONFIGURATION}/DotNetAspects.${VERSION}.nupkg"
if [ ! -f "$PACKAGE_FILE" ]; then
    echo -e "${RED}Error: Package file not found: ${PACKAGE_FILE}${NC}"
    exit 1
fi

# Show package contents
echo ""
echo -e "${YELLOW}[5/6] Package contents:${NC}"
unzip -l "$PACKAGE_FILE" | grep -E "\.dll|\.props|\.targets" | head -20

# Check if version already exists on NuGet
echo ""
echo -e "${YELLOW}Checking if version ${VERSION} exists on NuGet...${NC}"
NUGET_CHECK=$(curl -s "https://api.nuget.org/v3-flatcontainer/dotnetaspects/index.json" | grep -o "\"${VERSION}\"" || true)

if [ ! -z "$NUGET_CHECK" ]; then
    echo -e "${RED}⚠ WARNING: Version ${VERSION} already exists on NuGet!${NC}"
    echo -e "${YELLOW}Note: NuGet does not allow overwriting existing versions.${NC}"
    echo -e "${YELLOW}The push will fail if you continue.${NC}"
    echo ""
    read -p "Do you want to continue anyway? (y/N): " OVERWRITE
    if [[ ! "$OVERWRITE" =~ ^[Yy]$ ]]; then
        echo -e "${YELLOW}Aborted by user${NC}"
        exit 0
    fi
else
    echo -e "${GREEN}Version ${VERSION} is available${NC}"
fi

# Confirm before pushing
echo ""
echo -e "${YELLOW}Ready to push ${PACKAGE_FILE} to NuGet${NC}"
read -p "Continue? (y/N): " CONFIRM
if [[ ! "$CONFIRM" =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Aborted by user${NC}"
    exit 0
fi

# Push to NuGet
echo ""
echo -e "${YELLOW}[6/6] Pushing to NuGet...${NC}"
dotnet nuget push "$PACKAGE_FILE" --api-key "$NUGET_API_KEY" --source "$NUGET_SOURCE"
if [ $? -ne 0 ]; then
    echo -e "${RED}Error: NuGet push failed${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  Successfully published v${VERSION}!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "Package URL: https://www.nuget.org/packages/DotNetAspects/${VERSION}"
echo ""

# Ask to commit version change
read -p "Commit version change to git? (y/N): " COMMIT
if [[ "$COMMIT" =~ ^[Yy]$ ]]; then
    git add src/DotNetAspects/DotNetAspects.csproj src/DotNetAspects.Fody/DotNetAspects.Fody.csproj
    git commit -m "Release v${VERSION}"

    read -p "Push to remote? (y/N): " PUSH
    if [[ "$PUSH" =~ ^[Yy]$ ]]; then
        git push
        echo -e "${GREEN}Changes pushed to remote${NC}"
    fi
fi

echo -e "${GREEN}Done!${NC}"
