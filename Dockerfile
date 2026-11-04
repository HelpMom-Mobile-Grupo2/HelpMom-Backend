# Imagen de la API de HelpMom para Render (o cualquier host de contenedores).
# Build:  docker build -t helpmom-api .
# Run:    docker run -p 8080:8080 -e Jwt__Secret=... -e DATABASE_URL=postgres://... helpmom-api

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, with only the project files, so this layer is cached while the code changes.
COPY src/BackendMoviles.Domain/BackendMoviles.Domain.csproj src/BackendMoviles.Domain/
COPY src/BackendMoviles.Application/BackendMoviles.Application.csproj src/BackendMoviles.Application/
COPY src/BackendMoviles.Infrastructure/BackendMoviles.Infrastructure.csproj src/BackendMoviles.Infrastructure/
COPY src/BackendMoviles.Api/BackendMoviles.Api.csproj src/BackendMoviles.Api/
RUN dotnet restore src/BackendMoviles.Api/BackendMoviles.Api.csproj

COPY src/ src/
RUN dotnet publish src/BackendMoviles.Api/BackendMoviles.Api.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# The container uses PostgreSQL by default (DATABASE_URL or Database__ConnectionString).
# The base image's ASPNETCORE_HTTP_PORTS is cleared so the --urls below is the only binding.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS= \
    Database__Provider=PostgreSql

COPY --from=build /app/publish .

# Non-root user that ships with the .NET 8 images.
USER $APP_UID
EXPOSE 8080

# Render injects PORT; outside Render the API listens on 8080.
ENTRYPOINT ["/bin/sh", "-c", "exec dotnet BackendMoviles.Api.dll --urls \"http://0.0.0.0:${PORT:-8080}\""]
