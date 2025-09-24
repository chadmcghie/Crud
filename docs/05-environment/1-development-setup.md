# Development Environment Setup

## Prerequisites

### Required Software

**Core Development Tools:**
- **.NET 8 SDK** - Download from [microsoft.com/net](https://dotnet.microsoft.com/download)
- **Node.js 20+** - Download from [nodejs.org](https://nodejs.org)
- **Git** - Download from [git-scm.com](https://git-scm.com)

**IDE Options:**
- **Visual Studio 2022** (recommended for .NET development)
- **VS Code** with C# and Angular extensions
- **JetBrains Rider** (alternative professional IDE)

**Database Tools:**
- **SQLite browser** for database inspection (optional)
- **DB Browser for SQLite** or similar tool

### Platform-Specific Setup

#### Windows
```powershell
# Install via Chocolatey (optional)
choco install dotnet-sdk nodejs git vscode

# Verify installations
dotnet --version
node --version
npm --version
git --version
```

#### macOS
```bash
# Install via Homebrew
brew install dotnet node git

# Verify installations
dotnet --version
node --version
npm --version
git --version
```

#### Linux (Ubuntu/Debian)
```bash
# Install .NET SDK
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

# Install Node.js
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt-get install -y nodejs

# Install Git
sudo apt-get install git

# Verify installations
dotnet --version
node --version
npm --version
git --version
```

## Repository Setup

### 1. Clone Repository
```bash
git clone [repository-url]
cd Crud-Worktree-2
```

### 2. Backend Setup
```bash
# Restore .NET packages
dotnet restore solutions/Crud.sln

# Build the solution
dotnet build solutions/Crud.sln

# Run database migrations
dotnet ef database update -p src/Infrastructure -s src/Api
```

### 3. Frontend Setup
```bash
# Navigate to Angular project
cd src/Angular

# Install npm packages
npm install

# Build Angular project
npm run build
```

### 4. Verify Installation
```bash
# Start API (from root)
dotnet run --project src/Api/Api.csproj --launch-profile http

# In another terminal, start Angular (from src/Angular)
npm start

# Access application
# API: http://localhost:5172
# UI: http://localhost:4200
```

## IDE Configuration

### Visual Studio 2022
1. Open `solutions/Crud.sln`
2. Set `Api` as startup project
3. Install recommended extensions:
   - Entity Framework Power Tools
   - SonarLint
   - CodeMaid

### VS Code
1. Open root folder
2. Install recommended extensions (see `.vscode/extensions.json`)
3. Configure debugging (see `.vscode/launch.json`)

### Essential Extensions
- **C# for Visual Studio Code**
- **Angular Language Service**
- **ESLint**
- **Prettier**
- **GitLens**

## Configuration Files

### Environment Variables
Create `.env` file in root (not committed to git):
```
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=Data Source=src/Api/CrudAppDev.db
JWT_SECRET_KEY=your-development-secret-key-here
```

### User Secrets (Recommended)
```bash
# Set up user secrets for API project
dotnet user-secrets init --project src/Api
dotnet user-secrets set "JWT:SecretKey" "your-secret-key" --project src/Api
```

## Quick Start Commands

### Daily Development
```bash
# Kill any running servers
.scripts/kill-servers.ps1

# Start both API and Angular
.scripts/LaunchApps.ps1
```

### Development Workflow
```bash
# Run all tests before committing
dotnet test
cd src/Angular && npm test

# Format code
dotnet format solutions/Crud.sln
cd src/Angular && npm run lint

# Create feature branch
git checkout -b feature/your-feature-name
```

## Troubleshooting

### Common Issues

**Port already in use:**
```bash
# Kill processes on development ports
netstat -ano | findstr :5172
netstat -ano | findstr :4200
taskkill /PID [process-id] /F
```

**Database locked:**
```bash
# Stop all applications, then
dotnet ef database update -p src/Infrastructure -s src/Api
```

**npm install failures:**
```bash
# Clear npm cache and reinstall
cd src/Angular
rm -rf node_modules package-lock.json
npm cache clean --force
npm install
```

**SSL certificate issues:**
```bash
# Trust development certificates
dotnet dev-certs https --trust
```

### Getting Help

1. **Documentation** - Check relevant docs in this repository
2. **GitHub Issues** - Search existing issues or create new ones
3. **Team Chat** - Contact development team members
4. **Stack Overflow** - For general .NET/Angular questions

## Next Steps

After successful setup:
1. **Understand the Architecture** → [02-architecture/1-architecture-guidelines.md](../02-architecture/1-architecture-guidelines.md)
2. **Review Testing Setup** → [2-testing-setup.md](./2-testing-setup.md)
3. **Learn Development Workflow** → [03-development/6-workflows/development-workflow.md](../03-development/6-workflows/development-workflow.md)