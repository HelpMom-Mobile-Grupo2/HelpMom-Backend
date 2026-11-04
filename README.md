# Backend HelpMom

Backend REST de HelpMom para las dos apps móviles: la de la gestante (Android) y la del acompañante (Flutter). Está construido con .NET 8, Clean Architecture (Domain / Application / Infrastructure / Api), MediatR, FluentValidation, Entity Framework Core (SQLite en local, PostgreSQL en Render) y JWT.

El contrato completo para las apps está en [docs/00_contrato_api_backend.md](docs/00_contrato_api_backend.md).

## Requisitos

- .NET 8 SDK.
- Solo para crear migraciones nuevas: `dotnet-ef` (`dotnet tool install --global dotnet-ef --version 8.*`).
- Solo para construir la imagen o probar con PostgreSQL en local: Docker.

## Ejecutar la API

```powershell
dotnet run --project src/BackendMoviles.Api/BackendMoviles.Api.csproj --launch-profile http
```

- El perfil `http` escucha en `http://0.0.0.0:61614`, así que acepta conexiones del emulador y de celulares en la misma red. El perfil por defecto (`BackendMoviles.Api`) usa `https://localhost:61613` y `http://localhost:61614`.
- Swagger está en `http://localhost:61614/swagger`, con los endpoints agrupados por módulo.
- **Base de datos:** SQLite en `src/BackendMoviles.Api/helpmom.db`. Al arrancar se aplican las migraciones pendientes y se siembran los tips del acompañante. Los datos se conservan entre reinicios; para empezar de cero, detén la API y borra `helpmom.db*`.
- **Secreto JWT:** en desarrollo se usa el de `appsettings.Development.json`, así que `dotnet run` funciona sin configurar nada. **Ese secreto es solo para desarrollo.**

### URL base según desde dónde se prueba

| Cliente | URL base |
|---|---|
| Emulador Android | `http://10.0.2.2:61614/` |
| Simulador iOS, Flutter web o desktop | `http://localhost:61614/` |
| Celular físico en la misma red | `http://<IP-de-tu-PC>:61614/` (con el perfil `http`; puede que haya que permitir el puerto en el firewall de Windows) |

Usa HTTP, no HTTPS: el emulador no confía en el certificado de desarrollo. En Android hay que permitir tráfico en texto plano solo en debug.

### Proveedores de base de datos

`Database:Provider` (variable `Database__Provider`) admite tres valores:

| Valor | Uso | Conexión |
|---|---|---|
| `Sqlite` | Local (valor de `appsettings.json`) | `Database:ConnectionString`; por defecto `Data Source=helpmom.db` |
| `PostgreSql` | Render y la imagen Docker | `DATABASE_URL` (`postgres://user:pass@host:port/db`) o, si no existe, `Database:ConnectionString` en formato Npgsql |
| `InMemory` | Tests | `Database:Name` |

Al arrancar, la API aplica las migraciones pendientes del proveedor elegido; no hace falta correr `dotnet ef database update`.

## Tests

```powershell
dotnet test BackendMoviles.sln
```

Los tests usan `Database:Provider = InMemory`, salvo los smoke tests, que aplican la migración real y recorren el flujo principal:

- `SqliteSmokeTests` usa un archivo SQLite temporal y corre siempre.
- `PostgreSqlSmokeTests` se salta si no defines `HELPMOM_TEST_POSTGRES`. Para correrlo, levanta un PostgreSQL y apunta la variable al servidor; cada corrida crea su propia base y la borra al terminar:

```powershell
docker run -d --name helpmom-pg -e POSTGRES_PASSWORD=postgres -p 55432:5432 postgres:16-alpine
$env:HELPMOM_TEST_POSTGRES = 'Host=localhost;Port=55432;Username=postgres;Password=postgres'
dotnet test BackendMoviles.sln
```

Si el equipo no tiene el runtime de .NET 8 instalado, ejecuta antes `$env:DOTNET_ROLL_FORWARD = 'Major'`.

Para probar a mano el flujo completo (madre, acompañante, alertas y SOS automática) abre [docs/prueba_manual.http](docs/prueba_manual.http) con la extensión REST Client de VS Code.

## Migraciones

Cada proveedor tiene su propia carpeta de migraciones y su propio contexto: `HelpMomDbContext` para SQLite y `PostgreSqlHelpMomDbContext` para PostgreSQL. El modelo es el mismo, así que cada cambio del modelo necesita una migración para cada uno:

```powershell
dotnet ef migrations add <Nombre> --context HelpMomDbContext --project src/BackendMoviles.Infrastructure --startup-project src/BackendMoviles.Api --output-dir Persistence/Migrations/Sqlite
dotnet ef migrations add <Nombre> --context PostgreSqlHelpMomDbContext --project src/BackendMoviles.Infrastructure --startup-project src/BackendMoviles.Api --output-dir Persistence/Migrations/PostgreSql
```

Las dos se generan con las fábricas de diseño (`DesignTimeDbContextFactory` y `PostgreSqlDesignTimeDbContextFactory`), sin arrancar la API ni conectarse a una base.

## Deploy en Render

El repositorio trae un `Dockerfile` multi-stage: compila con `sdk:8.0` y ejecuta con `aspnet:8.0`, como usuario sin privilegios. La imagen usa PostgreSQL por defecto y escucha en `http://0.0.0.0:${PORT}`, o en el puerto 8080 si `PORT` no existe. Render define `PORT` automáticamente.

1. En Render, crea una base **PostgreSQL** en la misma región que tendrá la API y copia su **Internal Database URL**.
2. Crea un **Web Service** desde este repositorio con runtime **Docker**. Render detecta el `Dockerfile` de la raíz.
3. En **Health Check Path** escribe `/health`. El endpoint es público y responde `200 Healthy` cuando la API está lista y la base responde, o `503 Unhealthy` si no puede conectarse.
4. Define las variables de entorno de la tabla y despliega. Al arrancar, la API aplica la migración de PostgreSQL y siembra los tips.

| Variable | Obligatoria | Valor |
|---|---|---|
| `Jwt__Secret` | Sí | Secreto aleatorio de al menos 32 bytes; por ejemplo, el resultado de `openssl rand -base64 48`. Sin ella, la API no arranca. No reutilices el de desarrollo. |
| `DATABASE_URL` | Sí, salvo que uses `Database__ConnectionString` | La Internal Database URL de Render (`postgresql://user:pass@host/db`). Se convierte sola a formato Npgsql; si no trae puerto, se usa 5432, y se respeta un `?sslmode=require` si lo agregas. |
| `Database__ConnectionString` | No | Alternativa a `DATABASE_URL`, en formato Npgsql (`Host=...;Database=...;Username=...;Password=...`). Si existen las dos, gana `DATABASE_URL`. |
| `Database__Provider` | No | La imagen ya define `PostgreSql`. |
| `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationMinutes` | No | Por defecto `HelpMomApi`, `HelpMomFlutter` y `60`. |
| `PORT` | No | La define Render. |

Con eso, las apps usan como URL base `https://<tu-servicio>.onrender.com/`. Render termina HTTPS, así que en producción no hace falta permitir tráfico en texto plano. Swagger queda disponible en `/swagger`. Revisa los límites del plan gratuito de Render: los servicios gratuitos se suspenden por inactividad, y la primera petición tras la pausa tarda más.

Para probar la imagen en local, con un PostgreSQL en otro contenedor:

```powershell
docker build -t helpmom-api .
docker network create helpmom-net
docker run -d --name helpmom-pg --network helpmom-net -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=helpmom postgres:16-alpine
docker run --rm --network helpmom-net -p 8080:8080 -e Jwt__Secret=<secreto-de-32-bytes-o-más> -e DATABASE_URL=postgres://postgres:postgres@helpmom-pg/helpmom helpmom-api
```

Después, `http://localhost:8080/health` debe responder `Healthy`.

## Endpoints

Todos usan JSON en camelCase y, salvo `auth` y `GET /health`, requieren `Authorization: Bearer <token>`. Si el rol del token no corresponde, la respuesta es 403.

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| POST | `/api/auth/register` | Público | Registro. `role` opcional: `"Mother"` (por defecto) o `"Partner"` |
| POST | `/api/auth/login` | Público | Login. Devuelve `familyGroupId` y `role` |
| GET | `/health` | Público | Health check para Render (`Healthy` o `Unhealthy`) |
| POST | `/api/family/invitations` | Mother | Crea un código `HM-XXXX` que vence en 24 h |
| POST | `/api/family/join` | Partner | Se vincula con un código de invitación |
| DELETE | `/api/family/partner` | Mother | Desvincula al acompañante |
| GET | `/api/family` | Mother, Partner | Grupo familiar y sus miembros |
| POST | `/api/pregnancy` | Mother | Crea el registro de embarazo |
| GET | `/api/pregnancy` | Mother | Lista los embarazos de la madre |
| GET | `/api/pregnancy/{id}` | Mother | Detalle de un embarazo |
| PUT | `/api/pregnancy/{id}` | Mother | Actualiza FUM y FPP |
| DELETE | `/api/pregnancy/{id}` | Mother | Elimina el registro |
| POST | `/api/pregnancy/{pregnancyId}/symptoms` | Mother | Registra un síntoma |
| POST | `/api/telemetry` | Mother | Registra una medición simulada |
| GET | `/api/telemetry/status` | Mother | Última medición |
| GET | `/api/telemetry/history?count=20` | Mother | Historial de mediciones |
| POST | `/api/triage` | Mother | Inicia una conversación de triage |
| GET | `/api/triage` | Mother | Lista de conversaciones |
| POST | `/api/triage/{conversationId}/messages` | Mother | Envía un mensaje |
| GET | `/api/triage/{conversationId}` | Mother | Detalle de la conversación |
| GET | `/api/companion/summary` | Partner | Resumen de la gestante, sin valores clínicos |
| GET | `/api/companion/tips/today` | Partner | 3 tips del día según la semana de gestación |
| POST | `/api/companion/tips/{id}/done` | Partner | Marca un tip como hecho hoy |
| POST | `/api/alerts` | Mother | Envía una alerta rápida "Pasa la Voz" |
| GET | `/api/alerts?status=pending\|all` | Mother, Partner | Alertas del grupo (`all` por defecto) |
| POST | `/api/alerts/{id}/respond` | Partner | Responde una alerta |

Cuando el triage clasifica un mensaje como emergencia o una medición sale en rojo, el backend crea automáticamente una alerta SOS para el acompañante vinculado. Si ya hay una SOS pendiente del mismo motivo en los últimos 10 minutos, no crea otra.
