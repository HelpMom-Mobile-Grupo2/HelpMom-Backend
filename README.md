# Backend HelpMom

Backend REST para HelpMom, construido con .NET 8, Clean Architecture, MediatR, Entity Framework Core InMemory y JWT.

## Requisitos

- .NET 8 SDK

## Ejecutar la API

Configura una clave JWT local de al menos 32 bytes y luego inicia el proyecto:

```powershell
$env:Jwt__Secret = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet run --project src/BackendMoviles.Api/BackendMoviles.Api.csproj
```

Swagger estará disponible en `/swagger`. La base InMemory conserva datos solo mientras el proceso siga activo.

## Validar

```powershell
dotnet build BackendMoviles.sln
$env:DOTNET_ROLL_FORWARD = 'Major'
dotnet test BackendMoviles.sln
```

El roll-forward solo es necesario si el equipo no tiene instalado el runtime .NET 8. Para despliegue, configura `Jwt__Secret` con un secreto gestionado fuera del repositorio y sustituye el proveedor InMemory por una base persistente.