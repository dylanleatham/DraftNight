# Azure Deployment Guide

This guide covers the Azure infrastructure setup and configuration required to deploy the DraftNight application.

## Architecture Overview

```
                    ┌─────────────────────┐
                    │   Azure Front Door  │
                    │   (CDN + Routing)   │
                    └──────────┬──────────┘
                               │
              ┌────────────────┼────────────────┐
              │                │                │
              ▼                ▼                ▼
    ┌─────────────────┐ ┌─────────────┐ ┌─────────────────┐
    │  Static Web App │ │ App Service │ │ SignalR Service │
    │   (Frontend)    │ │  (Backend)  │ │   (Realtime)    │
    └─────────────────┘ └──────┬──────┘ └─────────────────┘
                               │
                               ▼
                      ┌─────────────────┐
                      │  Azure SQL DB   │
                      └─────────────────┘
```

## Azure Resources Required

| Resource | SKU | Purpose |
|----------|-----|---------|
| Azure Front Door | Standard | CDN, routing, custom domain SSL |
| Azure Static Web Apps | Free | React SPA hosting |
| Azure App Service | B1+ | .NET API hosting |
| Azure SignalR Service | Free/Standard | Real-time WebSocket connections |
| Azure SQL Database | Basic/S0 | Data persistence |

## Resource Configuration

### 1. Azure SQL Database

1. Create an Azure SQL Server and Database
2. Configure firewall rules:
   - Enable "Allow Azure services and resources to access this server"
3. Note the connection string for App Service configuration

### 2. Azure SignalR Service

1. Create an Azure SignalR Service resource
2. **Critical: Set Service Mode to "Default"**
   - Go to Settings → Service Mode
   - Select "Default" (NOT "Serverless")
   - Serverless mode does not support ASP.NET Core SignalR hubs
3. Copy the connection string from Keys section

### 3. Azure App Service (Backend)

1. Create an App Service (Linux, .NET 10)
2. Configure Application Settings:

| Setting Type | Name | Value |
|--------------|------|-------|
| Connection String | `DefaultConnection` | SQL Server connection string |
| Connection String | `AzureSignalR` | SignalR Service connection string |
| App Setting | `AllowedOrigins__0` | `https://www.yourdomain.com` |
| App Setting | `AllowedOrigins__1` | `https://yourdomain.com` |

3. The app automatically runs database migrations on startup

### 4. Azure Static Web Apps (Frontend)

1. Create a Static Web App linked to your GitHub repository
2. Build configuration:
   - App location: `src/frontend`
   - Output location: `dist`
   - Build command is handled by GitHub Actions
3. The `staticwebapp.config.json` in `src/frontend/public/` configures:
   - Navigation fallback for SPA routing
   - Security headers
   - **Important**: `/api/*` is excluded from fallback to allow Front Door routing

### 5. Azure Front Door

Front Door provides unified routing, SSL termination, and CDN for both frontend and backend.

#### Origin Groups

Create two origin groups:

**frontend** (Static Web App):
- Origin: `<your-swa>.azurestaticapps.net`
- Origin host header: same as origin

**backend** (App Service):
- Origin: `<your-app>.azurewebsites.net`
- Origin host header: same as origin

#### Routes

Create two routes on your endpoint:

**api-route** (must be evaluated first):
- Patterns to match: `/api/*`
- Origin group: `backend`
- Forwarding protocol: HTTPS only
- Caching: Disabled

**frontend-route**:
- Patterns to match: `/*`
- Origin group: `frontend`
- Forwarding protocol: HTTPS only

#### Custom Domain

1. Add your custom domain to the Front Door endpoint
2. **Important**: Associate the domain with BOTH routes (api-route and frontend-route)
3. Enable HTTPS with Front Door managed certificate
4. DNS: Create a CNAME record pointing to your Front Door endpoint (`*.azurefd.net`)

## GitHub Actions Configuration

### Repository Secrets

| Secret | Description |
|--------|-------------|
| `AZURE_CLIENT_ID` | Service principal client ID |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |

### Repository Variables

| Variable | Description |
|----------|-------------|
| `AZURE_WEBAPP_NAME` | App Service name (e.g., `draftnight-api`) |
| `AZURE_STATICWEBAPP_NAME` | Static Web App name |
| `VITE_API_URL` | API URL for frontend (e.g., `https://www.yourdomain.com/api`) |

### Deployment Workflow

The `deploy.yml` workflow:
1. Builds and tests the backend
2. Deploys backend to App Service
3. Builds frontend with `VITE_API_URL` environment variable
4. Deploys frontend to Static Web Apps

## Environment Variables

### Backend (appsettings.Production.json)

The production settings file configures:
- CORS origins for your domain
- Logging levels

Connection strings are configured in Azure App Service, not in the config file.

### Frontend

| Variable | Description | Build-time/Runtime |
|----------|-------------|-------------------|
| `VITE_API_URL` | Backend API URL | Build-time |

## Troubleshooting

### 405 Method Not Allowed on API requests

**Cause**: Requests to `/api/*` are being handled by Static Web Apps instead of being routed to the backend.

**Solutions**:
1. Verify your custom domain is associated with the `api-route` in Front Door
2. Ensure `/api/*` is excluded in `staticwebapp.config.json` navigation fallback
3. Check that the `api-route` pattern is `/api/*` (with the wildcard)

### 500 Internal Server Error on API requests

**Cause**: Backend application error, often related to SignalR or database connectivity.

**Check**:
1. Azure SignalR Service mode is set to "Default" (not "Serverless")
2. Connection strings are properly configured in App Service
3. View logs: `az webapp log tail --name <app-name> --resource-group <rg-name>`

### SignalR "serverless mode, server connection is not allowed"

**Cause**: Azure SignalR Service is in Serverless mode.

**Fix**: Change SignalR Service mode from "Serverless" to "Default" in Azure Portal.

### Database migration failures

**Symptoms**: App crashes on startup with database-related errors.

**Check**:
1. `DefaultConnection` connection string is correct
2. Azure SQL firewall allows Azure services
3. Database credentials are valid

View startup logs to see the specific error:
```bash
az webapp log tail --name <app-name> --resource-group <rg-name>
```

## Local Development

For local development, see the commands in `CLAUDE.md`. Key points:

1. Copy `src/frontend/.env.example` to `src/frontend/.env`
2. Set `VITE_API_URL=http://localhost:5000/api`
3. Run `docker-compose up -d` for local SQL Server
4. Run backend: `dotnet run --project src/backend/DraftApp.Api`
5. Run frontend: `npm run dev --prefix src/frontend`
