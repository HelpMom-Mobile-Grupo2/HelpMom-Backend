# Backend HelpMom

Backend REST de HelpMom para las dos apps móviles: la de la gestante (Android) y la del acompañante (Flutter). Está construido con .NET 8, Clean Architecture (Domain / Application / Infrastructure / Api), MediatR, FluentValidation, Entity Framework Core (SQLite) y JWT.

El contrato completo para las apps está en [docs/00_contrato_api_backend.md](docs/00_contrato_api_backend.md).

## Requisitos

- .NET 8 SDK.
- Solo para crear migraciones nuevas: `dotnet-ef` (`dotnet tool install --global dotnet-ef --version 8.*`).

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

### Producción

- Define el secreto JWT con la variable de entorno `Jwt__Secret` (al menos 32 bytes), gestionada fuera del repositorio. Fuera del entorno `Development` no se carga `appsettings.Development.json`, y la API no arranca sin `Jwt__Secret`.
- La cadena de conexión se cambia con `Database__ConnectionString`.

## Tests

```powershell
dotnet test BackendMoviles.sln
```

Los tests usan `Database:Provider = InMemory`, salvo `SqliteSmokeTests`, que aplica la migración sobre un archivo SQLite temporal. Si el equipo no tiene el runtime de .NET 8 instalado, ejecuta antes `$env:DOTNET_ROLL_FORWARD = 'Major'`.

Para probar a mano el flujo completo (madre, acompañante, alertas y SOS automática) abre [docs/prueba_manual.http](docs/prueba_manual.http) con la extensión REST Client de VS Code.

## Migraciones

```powershell
dotnet ef migrations add <Nombre> --project src/BackendMoviles.Infrastructure --startup-project src/BackendMoviles.Api --output-dir Persistence/Migrations
```

Las migraciones se generan para SQLite mediante `DesignTimeDbContextFactory`, sin arrancar la API.

## Endpoints

Todos usan JSON en camelCase y, salvo `auth`, requieren `Authorization: Bearer <token>`. Si el rol del token no corresponde, la respuesta es 403.

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| POST | `/api/auth/register` | Público | Registro. `role` opcional: `"Mother"` (por defecto) o `"Partner"` |
| POST | `/api/auth/login` | Público | Login. Devuelve `familyGroupId` y `role` |
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
