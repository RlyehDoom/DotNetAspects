#!/bin/bash

# DotNetAspects NuGet Publishing Script
# Usage: ./publish-nuget.sh [version] [configuration]
# Example: ./publish-nuget.sh 1.2.0 Release

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Configuration
CONFIGURATION="${2:-Release}"
NUGET_SOURCE="https://api.nuget.org/v3/index.json"

# Load credentials from .env file if exists
load_env() {
    if [ -f ".env" ]; then
        echo -e "${CYAN}Loading credentials from .env file...${NC}"
        # Export variables from .env (ignore comments, empty lines, and handle Windows CRLF)
        set -a
        source <(grep -v '^#' .env | grep -v '^$' | sed 's/\r$//')
        set +a
        return 0
    fi
    return 1
}

# Check and setup NuGet API key
setup_nuget_key() {
    if [ -z "$NUGET_API_KEY" ]; then
        echo -e "${YELLOW}NUGET_API_KEY not found in .env file${NC}"
        read -sp "Enter NuGet API Key: " NUGET_API_KEY
        echo ""

        if [ -z "$NUGET_API_KEY" ]; then
            echo -e "${RED}Error: NuGet API key is required${NC}"
            exit 1
        fi

        # Offer to save to .env
        read -p "Save API key to .env file? (y/N): " SAVE_KEY
        if [[ "$SAVE_KEY" =~ ^[Yy]$ ]]; then
            echo "NUGET_API_KEY=$NUGET_API_KEY" >> .env
            echo -e "${GREEN}API key saved to .env${NC}"
        fi
    else
        echo -e "${GREEN}NuGet API key loaded from .env${NC}"
    fi
}

# Check and setup Git credentials
setup_git_credentials() {
    local need_setup=false

    # Check git user.name
    local git_name=$(git config user.name 2>/dev/null || echo "")
    local git_email=$(git config user.email 2>/dev/null || echo "")

    # Use .env values if available
    if [ -n "$GIT_USER_NAME" ] && [ "$git_name" != "$GIT_USER_NAME" ]; then
        git config user.name "$GIT_USER_NAME"
        echo -e "${GREEN}Git user.name set from .env: $GIT_USER_NAME${NC}"
    fi

    if [ -n "$GIT_USER_EMAIL" ] && [ "$git_email" != "$GIT_USER_EMAIL" ]; then
        git config user.email "$GIT_USER_EMAIL"
        echo -e "${GREEN}Git user.email set from .env: $GIT_USER_EMAIL${NC}"
    fi

    # Re-check after potential .env updates
    git_name=$(git config user.name 2>/dev/null || echo "")
    git_email=$(git config user.email 2>/dev/null || echo "")

    # Prompt if still missing
    if [ -z "$git_name" ]; then
        echo -e "${YELLOW}Git user.name not configured${NC}"
        read -p "Enter your name for Git commits: " git_name
        if [ -n "$git_name" ]; then
            git config user.name "$git_name"
            read -p "Save to .env file? (y/N): " SAVE_NAME
            if [[ "$SAVE_NAME" =~ ^[Yy]$ ]]; then
                echo "GIT_USER_NAME=$git_name" >> .env
            fi
        fi
    fi

    if [ -z "$git_email" ]; then
        echo -e "${YELLOW}Git user.email not configured${NC}"
        read -p "Enter your email for Git commits: " git_email
        if [ -n "$git_email" ]; then
            git config user.email "$git_email"
            read -p "Save to .env file? (y/N): " SAVE_EMAIL
            if [[ "$SAVE_EMAIL" =~ ^[Yy]$ ]]; then
                echo "GIT_USER_EMAIL=$git_email" >> .env
            fi
        fi
    fi
}

# ============================================
# Main Script
# ============================================

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}  DotNetAspects NuGet Publisher${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Load environment variables
load_env

# Setup NuGet API key
setup_nuget_key

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
    # Setup git credentials if needed
    setup_git_credentials

    git add src/DotNetAspects/DotNetAspects.csproj src/DotNetAspects.Fody/DotNetAspects.Fody.csproj
    git commit -m "Release v${VERSION}"
    echo -e "${GREEN}Changes committed${NC}"

    read -p "Create git tag v${VERSION}? (y/N): " TAG
    if [[ "$TAG" =~ ^[Yy]$ ]]; then
        git tag -a "v${VERSION}" -m "Release v${VERSION}"
        echo -e "${GREEN}Tag v${VERSION} created${NC}"
    fi

    read -p "Push to remote (including tags)? (y/N): " PUSH
    if [[ "$PUSH" =~ ^[Yy]$ ]]; then
        git push
        if [[ "$TAG" =~ ^[Yy]$ ]]; then
            git push --tags
        fi
        echo -e "${GREEN}Changes pushed to remote${NC}"
    fi
fi

echo -e "${GREEN}Done!${NC}"
