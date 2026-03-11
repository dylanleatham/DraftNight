# Stage 1: Build React frontend
FROM node:22-alpine AS frontend-build
WORKDIR /app/frontend
COPY src/frontend/package.json src/frontend/package-lock.json ./
RUN npm ci
COPY src/frontend/ ./
RUN npm run build

# Stage 2: Build .NET backend
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS backend-build
WORKDIR /app
COPY src/backend/DraftApp.Engine/DraftApp.Engine.csproj ./src/backend/DraftApp.Engine/
COPY src/backend/DraftApp.Api/DraftApp.Api.csproj ./src/backend/DraftApp.Api/
RUN dotnet restore ./src/backend/DraftApp.Api/DraftApp.Api.csproj
COPY src/backend/ ./src/backend/
RUN dotnet publish ./src/backend/DraftApp.Api/DraftApp.Api.csproj -c Release -o /out

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=backend-build /out ./
COPY --from=frontend-build /app/frontend/dist ./wwwroot/
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
HEALTHCHECK --interval=30s --timeout=5s --retries=3 CMD curl --fail http://localhost:8080/healthz || exit 1
USER app
ENTRYPOINT ["dotnet", "DraftApp.Api.dll"]
