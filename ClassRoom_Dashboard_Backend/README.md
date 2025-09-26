# Classroom Dashboard Backend (ASP.NET Core)

Backend ASP.NET Core Web API para integrar Google Classroom con dashboards por rol (alumno/profesor/coordinador), con OAuth2 (Google), JWT, EF Core (PostgreSQL), y endpoints REST.

## Requisitos
- .NET SDK 9.0 (o 8.0 si ajustas `TargetFramework`)
- PostgreSQL 13+
- Cuenta Google Cloud con OAuth Client (tipo Web Application)
- Node.js (para el frontend, si aplica)

## Variables de entorno (Producción)
Configura como variables de entorno o en `appsettings.Production.json` (usa placeholders ya preparados):

- DATABASE_URL = Host=...;Database=...;Username=...;Password=...
- JWT_ISSUER = ClassroomDashboard
- JWT_AUDIENCE = ClassroomDashboardAudience
- JWT_SECRET_KEY = clave-larga-aleatoria
- GOOGLE_CLIENT_ID = your-client-id.apps.googleusercontent.com
- GOOGLE_CLIENT_SECRET = your-client-secret
- GOOGLE_REDIRECT_URI = https://tu-backend.example.com/auth/oauth2/callback
- CORS_ALLOWED_ORIGINS = https://tu-frontend.example.com,https://otro-dominio-permitido.com

En desarrollo, usa `ClassRoom_Dashboard_Backend/ClassRoom_Dashboard_Backend/appsettings.Development.json`.

## Migraciones (si las necesitas)
Si necesitas generar/actualizar la BD (solo si no existe):

```bash
# Crear migración inicial
 dotnet ef migrations add InitialCreate --project ClassRoom_Dashboard_Backend/ClassRoom_Dashboard_Backend.csproj
# Aplicar migraciones
 dotnet ef database update --project ClassRoom_Dashboard_Backend/ClassRoom_Dashboard_Backend.csproj
```

## Ejecutar localmente
```bash
cd ClassRoom_Dashboard_Backend/ClassRoom_Dashboard_Backend
# Development
set ASPNETCORE_ENVIRONMENT=Development
 dotnet run
```
La API expondrá por defecto un puerto dinámico (ver consola). Endpoints útiles:
- GET /api/test/health
- GET /api/test/generate-jwt
- GET /api/me (JWT)
- GET /api/courses (JWT)
- GET /api/dashboard/alumno (JWT)
- GET /api/coord/metrics (JWT + rol coordinator)
- POST /api/notifications/send (JWT)
- GET /health (health check)

## Docker (Producción)
Se incluye `Dockerfile` multi-stage.

```bash
# Construir imagen
 docker build -t classroom-dashboard-backend:latest .

# Ejecutar contenedor
 docker run -d --name classroom-backend \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e DATABASE_URL="Host=postgres;Database=classroom_dashboard;Username=postgres;Password=apc5tdrd" \
  -e JWT_ISSUER="ClassroomDashboard" \
  -e JWT_AUDIENCE="ClassroomDashboardAudience" \
  -e JWT_SECRET_KEY="cambia-esta-clave" \
  -e GOOGLE_CLIENT_ID="..." \
  -e GOOGLE_CLIENT_SECRET="..." \
  -e GOOGLE_REDIRECT_URI="https://tu-backend.example.com/auth/oauth2/callback" \
  -e CORS_ALLOWED_ORIGINS="https://tu-frontend.example.com" \
  classroom-dashboard-backend:latest
```

En producción, coloca un proxy reverso (Nginx/Azure Front Door) delante. La app ya está preparada con `UseForwardedHeaders` y HSTS.

## Seguridad
- JWT obligatorio en endpoints `/api/*` (salvo `auth/*` y `api/test/health`).
- CORS configurado por `AllowedOrigins` (array o CSV) en configuración.
- DataProtection keys persistidas en `/app/keys` (adecuado para contenedores). Monta volumen si escalas réplicas.
- Rate limiting básico: 100 req/min por ventana con cola de 10.
- Serilog con sinks de consola y archivo (rolling daily, `logs/app-*.log`).

## OAuth Google (pasos)
1. Crea un OAuth Client en Google Cloud Console (tipo Web application).
2. Agrega Redirect URIs:
   - Desarrollo: https://localhost:5001/auth/oauth2/callback
   - Producción: https://tu-backend.example.com/auth/oauth2/callback
3. Copia ClientId/Secret a variables de entorno.
4. Flujo: Frontend -> GET /auth/google -> Google -> /auth/oauth2/callback -> API retorna `{ token: jwt }`.

## Despliegue en Render.com

### Variables de Entorno para Render.com

En el dashboard de Render.com, ve a tu servicio web y configura las siguientes variables de entorno:

**Base de Datos:**
- `DATABASE_URL` = postgresql://postgres:apc5tdrd@db-classroom-dashboard:5432/classroom_dashboard

**JWT:**
- `JWT_ISSUER` = ClassroomDashboard
- `JWT_AUDIENCE` = ClassroomDashboardAudience
- `JWT_SECRET_KEY` = 8f3a0c2e6f9d4b17a2c15e8b1d7f4c3a9b0e5d6c7f12a34b56c78d90e1f23456a7b8c9d0e1f2233445566778899aabbccddeeff00112233445566778899

**Google OAuth:**
- `GOOGLE_CLIENT_ID` = 374302406745-6g4p5asgaumaourg5clgod56j8fsaock.apps.googleusercontent.com
- `GOOGLE_CLIENT_SECRET` = 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
- `GOOGLE_REDIRECT_URI` = https://concursofullstack.onrender.com/auth/oauth2/callback

**CORS:**
- `CORS_ALLOWED_ORIGINS` = https://concursofullstack.onrender.com,http://localhost:5173,http://localhost:3000

**Configuración de Entorno:**
- `ASPNETCORE_ENVIRONMENT` = Production

### Pasos para Despliegue:

1. **Base de Datos PostgreSQL:**
   - Crea un nuevo servicio PostgreSQL en Render.com
   - Copia la `External Database URL` y úsala como `DATABASE_URL`

2. **Servicio Web:**
   - Crea un nuevo Web Service en Render.com
   - Conecta el repositorio GitHub
   - Configura las variables de entorno arriba mencionadas
   - Render detectará automáticamente que es un proyecto .NET

3. **Actualizar URLs:**
   - Una vez desplegado, actualiza `GOOGLE_REDIRECT_URI` con la URL real de tu backend
   - Actualiza `CORS_ALLOWED_ORIGINS` si es necesario

### URLs de Producción:
- **Frontend:** https://concursofullstack.onrender.com
- **Backend:** https://concursofullstack.onrender.com (o tu URL específica de Render)
- **Google OAuth Redirect:** https://concursofullstack.onrender.com/auth/oauth2/callback

## Estructura relevante
- Program.cs: configuración de servicios/middleware (JWT, CORS, Serilog, HealthChecks, RateLimiting, DataProtection, EF Core)
- Services/GoogleClassroomService.cs: integración con Google Classroom (con refresh de tokens)
- Controlers/*: endpoints REST (auth, me, courses, dashboard, coord, notifications, test, error)
- Models/*: modelos EF Core y DbContext

## Postman/HTTP tests
Se incluye `test-requests.http` para pruebas rápidas (VS Code/IDE compatibles).

---
Si quieres, puedo preparar `docker-compose.yml` con Postgres y la API para entornos de staging y un archivo `.env.sample` con variables comunes.
