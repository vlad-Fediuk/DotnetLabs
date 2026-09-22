using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using AuthService.Data;
using AuthService.Features.Auth;
using AuthService.Features.Users;
using AuthService.Models;
using ECommerce.Shared.Handlers;
using ECommerce.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AuthDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IBaseRepository<Role>, BaseRepository<Role>>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService.Features.Auth.AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddJwtAuth(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Auth Service API",
        Version = "v1",
        Description = "Microservice for Authentication and User Management"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", null, null),
            new List<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@ecommerce.com");
    if (admin != null)
    {
        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
        await db.SaveChangesAsync();
    }
}

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth Service API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapUserEndpoints();

app.MapGet("/health", async (AuthDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { service = "AuthService", status = "Healthy", database = "Connected", port = 5001 })
            : Results.Json(new { service = "AuthService", status = "Degraded", database = "Unavailable" }, statusCode: 503);
    }
    catch (Exception ex)
    {
        return Results.Json(new { service = "AuthService", status = "Unhealthy", error = ex.Message }, statusCode: 503);
    }
})
.WithName("HealthCheck")
.WithTags("Health")
.WithSummary("Check service and database health");

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
