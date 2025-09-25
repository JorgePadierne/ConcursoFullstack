using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Classroom_Dashboard_Backend.Services;
using Serilog;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// CORS - restrict to allowed origins from configuration (supports string[] or CSV string)
var allowedOriginsSection = builder.Configuration.GetSection("AllowedOrigins");
string[] allowedOrigins = Array.Empty<string>();
if (allowedOriginsSection.Exists())
{
    var arr = allowedOriginsSection.Get<string[]>();
    if (arr != null && arr.Length > 0)
    {
        allowedOrigins = arr;
    }
}
if (allowedOrigins.Length == 0)
{
    var allowedOriginsCsv = builder.Configuration["AllowedOrigins"];
    if (!string.IsNullOrWhiteSpace(allowedOriginsCsv))
    {
        allowedOrigins = allowedOriginsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.AllowAnyHeader().AllowAnyMethod();
        }
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ApiPolicy", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
});

// We'll set up HealthChecks after computing the final connection string below

// Data Protection (for encrypting refresh tokens) - persist keys to disk (suitable for containers)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "keys")));

// EF Core - PostgreSQL (default) using connection string from configuration with pooling and retries
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    // Fallback to DATABASE_URL environment variable (can be URI or key-value)
    connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
}
if (string.IsNullOrWhiteSpace(connectionString))
{
    Log.Warning("No database connection string found. Set ConnectionStrings:Default or DATABASE_URL.");
}

// Normalize URI form to key-value form for Npgsql if necessary
static string NormalizeConnectionString(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
    var trimmed = raw.Trim();
    if (trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(trimmed);
        var userInfo = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? "");
        var pass = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? "");
        var host = uri.Host;
        var port = uri.IsDefaultPort ? 5432 : uri.Port;
        var database = uri.AbsolutePath.TrimStart('/');

        // Parse query parameters
        var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var sslmode = q["sslmode"] ?? "Require";
        var channelBinding = q["channel_binding"]; // Neon often uses "require"

        var parts = new List<string>
        {
            $"Host={host}",
            $"Port={port}",
            $"Database={database}",
            $"Username={user}",
            $"Password={pass}",
            $"SSL Mode={sslmode}",
            "Trust Server Certificate=true"
        };
        if (!string.IsNullOrWhiteSpace(channelBinding))
        {
            parts.Add($"Channel Binding={channelBinding}");
        }
        return string.Join(';', parts);
    }
    return trimmed;
}

var normalizedConnectionString = NormalizeConnectionString(connectionString);

// Health Checks now that we have the final connection string
builder.Services.AddHealthChecks().AddNpgSql(normalizedConnectionString);
builder.Services.AddDbContextPool<Classroom_Dashboard_Backend.Models.ClassroomDBContext>(options =>
{
    options.UseNpgsql(normalizedConnectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    });
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

// App services
builder.Services.AddScoped<GoogleClassroomService>();

// Authentication - JWT Bearer
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty)
            ),
            ValidateIssuerSigningKey = true
        };
    });

// Authorization
builder.Services.AddAuthorization();

// App
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

// Global error handling
app.UseExceptionHandler("/error");

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Support reverse proxies (e.g., Nginx, Azure App Service frontends)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                      Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

// Allow disabling HTTPS redirection via config/env for local Docker/dev scenarios
var disableHttpsRedirect = builder.Configuration.GetValue<bool>("DisableHttpsRedirect")
    || (Environment.GetEnvironmentVariable("DISABLE_HTTPS_REDIRECT")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false);
if (!disableHttpsRedirect)
{
    app.UseHttpsRedirection();
}
app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseCors("DefaultCors");
app.UseAuthentication();
app.UseAuthorization();

// Health checks
app.MapHealthChecks("/health");

app.MapControllers().RequireRateLimiting("ApiPolicy");

app.Run();
