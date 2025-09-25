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

## Notas de despliegue (Azure App Service ejemplo)
- Setea variables en `Settings > Configuration`.
- `WEBSITE_HTTPLOGGING_RETENTION_DAYS=7` (opcional logs)
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`
- Habilita HTTPS y redirección.

## Estructura relevante
- Program.cs: configuración de servicios/middleware (JWT, CORS, Serilog, HealthChecks, RateLimiting, DataProtection, EF Core)
- Services/GoogleClassroomService.cs: integración con Google Classroom (con refresh de tokens)
- Controlers/*: endpoints REST (auth, me, courses, dashboard, coord, notifications, test, error)
- Models/*: modelos EF Core y DbContext

## Postman/HTTP tests
Se incluye `test-requests.http` para pruebas rápidas (VS Code/IDE compatibles).

---
Si quieres, puedo preparar `docker-compose.yml` con Postgres y la API para entornos de staging y un archivo `.env.sample` con variables comunes.
