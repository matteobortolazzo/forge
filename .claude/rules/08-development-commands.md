# Development Commands

## Backend

```bash
# Navigate to backend
cd src/Forge.Api/Forge.Api

# Restore and run
dotnet restore
dotnet run

# Run with watch
dotnet watch run

# Run integration tests (from solution directory)
cd src/Forge.Api
dotnet test tests/Forge.Api.IntegrationTests

# Database migrations (from Forge.Api project)
cd src/Forge.Api/Forge.Api
dotnet ef migrations add MigrationName
dotnet ef database update
```

## Frontend

```bash
# Navigate to frontend
cd src/Forge.Ui

# Install dependencies
npm install

# Run development server
ng serve

# Build for production
ng build --configuration production

# Run tests
ng test
```

## E2E Testing

```bash
# Start backend in mock mode (from Forge.Api project)
cd src/Forge.Api/Forge.Api
dotnet run --launch-profile e2e

# Run Playwright tests (from Forge.Ui)
cd src/Forge.Ui
npm run e2e

# Run with interactive UI
npm run e2e:ui

# Run with visible browser
npm run e2e:headed
```

## Environment Variables

```env
DATABASE_PATH="forge.db"
CLAUDE_CODE_PATH="claude"
ASPNETCORE_URLS="http://localhost:5000"
CLAUDE_MOCK_MODE="true"         # Enable mock Claude client for E2E testing
AGENTS_PATH="./agents"          # Optional: custom path to agents directory (default: ./agents)
```

Note: Repository paths are now managed through the API. Use `POST /api/repositories` to add repositories.
