using System.Diagnostics;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseCors();

var monolithDest = $"http://{builder.Configuration["Monolith:Host"] ?? "localhost"}:{builder.Configuration["Monolith:Port"] ?? "5100"}";

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    context.Request.Headers["X-Correlation-Id"] = correlationId;
    context.Response.Headers["X-Correlation-Id"] = correlationId;

    var sw = Stopwatch.StartNew();
    await next();
    sw.Stop();

    var dest = context.Request.Path.StartsWithSegments("/health") ? "Internal (/health)" : monolithDest;
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("[{CorrelationId}] {Method} {Path} -> {Destination} -> {StatusCode} ({ElapsedMs} ms)",
        correlationId, context.Request.Method, context.Request.Path, dest, context.Response.StatusCode, sw.ElapsedMilliseconds);
});

app.MapWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/health"),
    healthApp => healthApp.Run(async ctx =>
    {
        ctx.Response.StatusCode = 200;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new { service = "ApiGateway", status = "Healthy", port = 5000 });
    })
);

await app.UseOcelot();

app.Run();
