# HelpMom: contrato de API para las apps móviles

Este bloque se pega dentro de los dos prompts (Android y Flutter). Describe el backend real del repo `HelpMom-Mobile-Grupo2/HelpMom-Backend` en la rama `feature/backend-companion-complete` (noviembre de 2026). **Todos los endpoints que necesita la app del acompañante ya están implementados**: las implementaciones simuladas (mock) de los repositorios se pueden cambiar por las reales sin tocar las pantallas.

## A. Cómo levantar el backend para probar

- Stack: .NET 8, ASP.NET Core, MediatR, EF Core con **SQLite** (los datos se conservan entre reinicios en `src/BackendMoviles.Api/helpmom.db`) y JWT.
- Arranque (PowerShell). En desarrollo ya no hace falta definir `Jwt__Secret`:
  ```powershell
  dotnet run --project src/BackendMoviles.Api/BackendMoviles.Api.csproj --launch-profile http
  ```
- El perfil `http` escucha en `http://0.0.0.0:61614`. Swagger está en `/swagger`.
- URL base según desde dónde se pruebe:
  - Emulador Android: `http://10.0.2.2:61614/`
  - Simulador iOS o Flutter web/desktop: `http://localhost:61614/`
  - Celular físico: `http://<IP-de-tu-PC>:61614/`, con el perfil `http`.
- Usar **HTTP**, no HTTPS: el emulador no confía en el certificado de desarrollo. En Android hay que permitir tráfico en texto plano (`android:usesCleartextTraffic="true"`) solo en debug.
- Para reiniciar los datos, detener la API y borrar `helpmom.db*`.

## B. Reglas generales

- JSON en **camelCase**.
- **Los enums llegan como números**, no como texto:
  - `healthLight`: 1 = Green, 2 = Yellow, 3 = Red
  - `severity`: 1 = Mild, 2 = Moderate, 3 = Severe
  - `urgency`: 1 = SelfCare, 2 = ContactCareTeam, 3 = Emergency, o `null` si aún no hay evaluación
  - `author`: 1 = Mother, 2 = Assistant
  - `role` dentro de `members` (grupo familiar): 1 = Mother, 2 = Doctor, 3 = Partner
  - `type` de alerta: 1 = Craving (antojo), 2 = TiredOrPain (cansancio o dolor), 3 = ComeHere (ven un momento), 4 = Sos
  - `status` de alerta: 1 = Pending, 2 = Responded
  - `response` de alerta: 1 = OnMyWay (voy en camino), 2 = CallingNow (te llamo ahora), 3 = Done (ya me encargué), o `null` si no hay respuesta
  - **Excepción:** `role` en `AuthResponse` es texto: `"Mother"`, `"Partner"` o `"Doctor"`.
- Fechas: `DateOnly` llega como `"2026-05-10"` y `DateTimeOffset` como ISO-8601 (`"2026-11-03T01:00:00+00:00"`).
- Autenticación: header `Authorization: Bearer <accessToken>`. El token dura **60 minutos** y lleva el rol del usuario. Ante un 401 se borra el token y se vuelve al login.
- **Roles:** cada endpoint acepta solo los roles indicados en la tabla. Con otro rol responde **403** con el cuerpo vacío; sin token o con un token inválido, **401**, también con el cuerpo vacío.
- Errores: llegan como `application/problem+json`, con la forma `{ "status": 400, "title": "...", "detail": "...", "instance": "/api/...", "errors": { "Password": ["..."] } }`. `errors` solo viene en errores de validación (400). El `detail` está en español y se puede mostrar al usuario.
  - 400: validación
  - 401: credenciales o token inválidos
  - 403: el rol no tiene permiso para ese endpoint
  - 404: no encontrado
  - 409: conflicto (por ejemplo, correo ya registrado, código de invitación vencido o acompañante no vinculado)

## C. Endpoints

Todos están implementados. La columna "Estado" indica si el endpoint ya existía (**Existente**) o se agregó en esta versión (**Nuevo ✅**).

| Método | Ruta | Rol | Body | Respuesta | Estado |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Público | `{ "email", "fullName", "password", "role"? }` (password de **10 a 128** caracteres; `role`: `"Mother"` por defecto o `"Partner"`) | 201 `AuthResponse`. Mother crea su grupo familiar; Partner llega con `familyGroupId: null`. `"Doctor"` u otro valor: 400 | Ampliado ✅ |
| POST | `/api/auth/login` | Público | `{ "email", "password" }` | 200 `AuthResponse` con el `familyGroupId` real (`null` si es un Partner sin vincular) y el rol | Ampliado ✅ |
| POST | `/api/family/invitations` | Mother | (sin body) | 201 `{ "code": "HM-9428", "expiresAt": "..." }`. Vence en 24 h y **anula la invitación activa anterior** del grupo. El QR codifica `code` | Nuevo ✅ |
| POST | `/api/family/join` | Partner | `{ "code": "HM-9428" }` | 200 `{ "familyGroupId": "guid", "motherName": "Daniela Gómez" }`. 400 formato inválido, 404 código inexistente, 409 vencido, usado, partner ya vinculado o gestante que ya tiene acompañante | Nuevo ✅ |
| DELETE | `/api/family/partner` | Mother | (sin body) | 204. Revoca el vínculo. 404 si no hay acompañante | Nuevo ✅ |
| GET | `/api/family` | Mother, Partner | (sin body) | 200 `FamilyGroupDto`. 404 si el usuario aún no pertenece a un grupo | Nuevo ✅ |
| POST | `/api/pregnancy` | Mother | `{ "lastMenstrualPeriod": "2026-05-10", "dueDate": "2027-02-14" }` (FPP > FUM y como máximo 308 días después) | 201 `PregnancyDto` | Existente |
| GET | `/api/pregnancy` | Mother | (sin body) | 200 `PregnancyDto[]` (lista vacía si la madre aún no configuró su embarazo) | Existente |
| GET | `/api/pregnancy/{id}` | Mother | (sin body) | 200 `PregnancyDto` / 404 | Existente |
| PUT | `/api/pregnancy/{id}` | Mother (dueña) | `{ "lastMenstrualPeriod", "dueDate" }` (mismas reglas que al crear) | 200 `PregnancyDto` / 400 / 404 | Nuevo ✅ |
| DELETE | `/api/pregnancy/{id}` | Mother (dueña) | (sin body) | 204 / 404 | Nuevo ✅ |
| POST | `/api/pregnancy/{pregnancyId}/symptoms` | Mother | `{ "description", "severity": 1, "occurredAt"? }` | 201 `SymptomDto` | Existente |
| POST | `/api/telemetry` | Mother | `{ "fetalHeartRateBpm": 140, "temperatureCelsius": 36.8, "measuredAt"? }` (FCF de 30 a 250, temperatura de 30 a 45) | 201 `TelemetryDto`. Es una **medición simulada**: no hay sensor real | Existente |
| GET | `/api/telemetry/status` | Mother | (sin body) | 200 `TelemetryDto` con la última medición / 404 `{ "message": "Aún no hay mediciones de salud." }` | Existente |
| GET | `/api/telemetry/history?count=20` | Mother | (sin body) | 200 `TelemetryDto[]`, de la más reciente a la más antigua. `count` de 1 a 500 (20 por defecto) | Nuevo ✅ |
| POST | `/api/triage` | Mother | `{ "content": "texto" }` | 201 `TriageConversationDto`. Inicia una conversación | Existente |
| GET | `/api/triage` | Mother | (sin body) | 200 `TriageConversationSummaryDto[]`, de la más reciente a la más antigua | Nuevo ✅ |
| POST | `/api/triage/{conversationId}/messages` | Mother | `{ "content": "texto" }` | 200 `TriageConversationDto` | Existente |
| GET | `/api/triage/{conversationId}` | Mother | (sin body) | 200 `TriageConversationDto` / 404 | Existente |
| GET | `/api/companion/summary` | Partner | (sin body) | 200 `CompanionSummaryDto`. **Sin valores clínicos crudos ni síntomas.** 409 si no está vinculado | Nuevo ✅ |
| GET | `/api/companion/tips/today` | Partner | (sin body) | 200 `DailyTipDto[]` (3 tips). 409 si no está vinculado | Nuevo ✅ |
| POST | `/api/companion/tips/{id}/done` | Partner | (sin body) | 204. Idempotente. 404 si el tip no existe | Nuevo ✅ |
| POST | `/api/alerts` | Mother | `{ "type": 2, "message"? }` (`message` hasta 500 caracteres) | 201 `QuickAlertDto`. 409 si no hay acompañante vinculado | Nuevo ✅ |
| GET | `/api/alerts?status=pending\|all` | Mother, Partner | (sin body) | 200 `QuickAlertDto[]` del propio grupo, de la más reciente a la más antigua. **`all` por defecto**. 400 si `status` es otro valor. 409 si el partner no está vinculado | Nuevo ✅ |
| POST | `/api/alerts/{id}/respond` | Partner | `{ "response": 1 }` | 200 `QuickAlertDto`. 404 si la alerta no es de su grupo, 409 si ya estaba respondida | Nuevo ✅ |

### Modelos

```json
// AuthResponse
{ "user": { "id": "guid", "email": "dani@mail.com", "fullName": "Daniela Gómez", "isActive": true },
  "familyGroupId": "guid-o-null", "role": "Mother", "accessToken": "eyJ..." }

// FamilyGroupDto
{ "id": "guid", "name": "Familia de Daniela Gómez",
  "members": [ { "userId": "guid", "fullName": "Daniela Gómez", "role": 1, "joinedAt": "..." },
               { "userId": "guid", "fullName": "Carlos Ruiz", "role": 3, "joinedAt": "..." } ] }

// PregnancyDto
{ "id": "guid", "motherUserId": "guid", "lastMenstrualPeriod": "2026-05-10", "dueDate": "2027-02-14",
  "gestationalWeeks": 25, "gestationalDays": 1,
  "symptoms": [ { "id": "guid", "description": "Dolor lumbar", "severity": 2, "occurredAt": "2026-11-02T20:10:00+00:00" } ] }

// TelemetryDto
{ "id": "guid", "motherUserId": "guid", "fetalHeartRateBpm": 140, "temperatureCelsius": 36.8,
  "measuredAt": "2026-11-03T01:00:00+00:00", "healthLight": 1,
  "healthSummary": "Valores dentro del rango de referencia simulado." }

// TriageConversationDto
{ "id": "guid", "motherUserId": "guid", "createdAt": "...",
  "messages": [ { "id": "guid", "author": 1, "content": "...", "sentAt": "..." },
                { "id": "guid", "author": 2, "content": "...", "sentAt": "..." } ],
  "urgency": 3, "guidance": "Esta simulación detectó una posible señal de alarma..." }

// TriageConversationSummaryDto (lastMessage es el último mensaje, normalmente la respuesta del asistente)
{ "id": "guid", "createdAt": "...", "urgency": 2, "lastMessage": "La respuesta de triage es simulada..." }

// CompanionSummaryDto (exactamente estos seis campos)
{ "motherName": "Daniela Gómez", "gestationalWeeks": 25, "gestationalDays": 1,
  "healthLight": 1, "healthSummary": "Valores dentro del rango de referencia simulado.",
  "measuredAt": "2026-11-03T01:00:00+00:00" }

// QuickAlertDto
{ "id": "guid", "type": 2, "message": "Siento mucho cansancio y molestia lumbar, ¿puedes ayudarme con la cena hoy?",
  "createdAt": "...", "status": 1, "response": null, "respondedAt": null }

// DailyTipDto
{ "id": "guid", "title": "Evita que cargue peso o paquetes", "body": "Desde el segundo trimestre...", "done": false }
```

### Reglas que conviene conocer

- **Semáforo:**
  - Rojo: FCF < 100 o > 180, o temperatura ≥ 39.5 °C o < 35 °C.
  - Amarillo: FCF < 110 o > 160, o temperatura ≥ 38 °C o < 36 °C.
  - Verde: el resto.
- **Triage:** detecta palabras clave, no es IA. Antes de comparar se pasa el texto a minúsculas y se le quitan las tildes, así que "Rompí fuente" y "rompi fuente" dan lo mismo.
  - `urgency = 3` (Emergency) con "sangrado abundante", "no puedo respirar", "dificultad para respirar", "convulsiones", "desmayo", "dolor intenso", "se mueve menos", "me estoy desangrando", "rompí fuente", "no siento al bebé", "dolor muy fuerte", "visión borrosa" o "me desmayé".
  - `urgency = 2` con "fiebre", "dolor", "sangrado" o "contracciones".
  - `urgency = 1` en cualquier otro caso.
  - La respuesta del asistente es siempre un texto fijo según la urgencia.
- **SOS automática:** cuando una medición sale en rojo o el triage clasifica un mensaje como Emergency, el backend crea una alerta `type: 4` (Sos) para el acompañante vinculado, si lo hay. El mensaje es:
  - `"Alerta automática: la medición de salud salió en rojo"`, o
  - `"Alerta automática: el triage detectó una posible emergencia"`.

  **No se duplica:** si ya hay una SOS **pendiente** del mismo motivo creada en los últimos 10 minutos, no se crea otra. Si falla la creación de la alerta, la medición o el mensaje de triage igual responden 201/200.
- **Alertas rápidas:** si `message` llega vacío, se usa un texto por defecto según el tipo:
  - Craving: "Tengo un antojo, ¿me ayudas a conseguir algo rico?"
  - TiredOrPain: "Me siento cansada o con molestias, ¿puedes ayudarme?"
  - ComeHere: "¿Puedes venir un momento? Te necesito."
  - Sos: "Necesito ayuda urgente. Comunícate conmigo ahora."
- **Tips del día:** se eligen 3 tips de la semana de gestación actual, y la selección rota cada día. "Hoy" se calcula en **UTC**, también para `done`. Sin embarazo registrado, rotan sobre todo el catálogo; desde la semana 42 en adelante se usan los de la semana 42.
- **Datos que no maneja el backend:** no guarda presión arterial, pulso materno, peso ni sueño. Solo FCF (frecuencia cardíaca fetal) y temperatura.

## D. Decisiones y diferencias con la propuesta anterior

Lo que las apps deben tener en cuenta respecto de la propuesta original de esta sección:

1. **Un solo acompañante por grupo.** Si la gestante ya tiene un partner vinculado, `POST /api/family/join` responde 409 ("Esta gestante ya tiene un acompañante vinculado."). Para cambiar de acompañante, la madre primero llama a `DELETE /api/family/partner`.
2. **`gestationalWeeks` y `gestationalDays` pueden ser `null`** en `CompanionSummaryDto` si la gestante aún no registró su embarazo. `healthLight`, `healthSummary` y `measuredAt` llegan `null` si no hay mediciones. La app debe mostrar un estado vacío en ambos casos.
3. **`GET /api/alerts` devuelve `all` por defecto.** Para ver solo las pendientes hay que pedir `?status=pending` de forma explícita. Cualquier otro valor de `status` da 400.
4. **`FamilyMemberDto` incluye `fullName`**: `{ userId, fullName, role, joinedAt }`. Ahí `role` es **numérico** (1 = Mother, 3 = Partner), a diferencia del `role` en texto de `AuthResponse`.
5. **El login devuelve el `familyGroupId` real.** Antes llegaba siempre `null`. Para un Partner sin vincular sigue llegando `null`.
6. **Autorización por rol en los endpoints que ya existían.** Pregnancy, Telemetry y Triage ahora son solo para Mother: un token de Partner recibe 403. La app de la madre no se ve afectada.
7. **Partner sin vincular:** `/api/companion/summary`, `/api/companion/tips/today` y `GET /api/alerts` responden 409 con `detail: "Aún no estás vinculado a una gestante."`. En cambio, `GET /api/family` responde 404. Con cualquiera de los dos, la app del acompañante debería llevar a la pantalla de vinculación.
8. **Código de invitación:** siempre `HM-` seguido de 4 dígitos. Se acepta en minúsculas y con espacios alrededor. Al crear una invitación nueva, la anterior deja de servir (409 "venció").
9. **Endpoints que no estaban en la propuesta:** `GET /api/family`, `GET /api/triage`, `PUT /api/pregnancy/{id}` y `DELETE /api/pregnancy/{id}`.
10. **Persistencia:** el backend usa SQLite y los datos ya no se borran al reiniciar. En desarrollo no hace falta configurar el secreto JWT.
