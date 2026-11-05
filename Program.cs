using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ApiRestProject.Api;
using ApiRestProject.Application;
using ApiRestProject.Application.Ports.Out;
using ApiRestProject.Domain;
using ApiRestProject.Infrastructure;
using ApiRestProject.Infrastructure.Persistence;
using ApiRestProject.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---------- Autenticación JWT ----------
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwt.SigningKey(),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = "name",
        };
        o.Events = new JwtBearerEvents
        {
            // Si el usuario fue desactivado o cambió de rol, el token deja de servir de inmediato.
            OnTokenValidated = async ctx =>
            {
                var sub = ctx.Principal?.FindFirstValue("sub") ?? ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var role = ctx.Principal?.FindFirstValue(ClaimTypes.Role);
                var users = ctx.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var user = int.TryParse(sub, out var id) ? await users.GetByIdAsync(id, ctx.HttpContext.RequestAborted) : null;
                if (user is null || !user.IsActive || user.Role.ToString() != role)
                    ctx.Fail("La sesión ya no es válida.");
            },
        };
    });

// Todo requiere sesión salvo lo marcado con [AllowAnonymous] (login y registro).
builder.Services.AddAuthorization(o => o.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser().Build());

// Limita intentos de login/registro por IP para dificultar la fuerza bruta.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// CORS para el frontend web (ajusta los orígenes en appsettings: "Cors:Origins").
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddPolicy("Frontend", p =>
    p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Crea/actualiza la base de datos, el administrador inicial y asigna los datos previos a ese usuario.
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.InitializeAsync(
        scope.ServiceProvider.GetRequiredService<FinanzasDbContext>(),
        scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
        app.Configuration,
        app.Logger);
}

// Detrás de CloudFront/App Runner el TLS termina en AWS: respeta X-Forwarded-* para IP real y esquema.
var forwarded = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwarded.KnownIPNetworks.Clear();
forwarded.KnownProxies.Clear();
app.UseForwardedHeaders(forwarded);

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseHttpsRedirection();
}

app.MapGet("/health", () => Results.Ok("ok")).AllowAnonymous();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.Run();
