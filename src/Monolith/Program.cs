using LR0.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddInHouseJwtAuthentication(builder.Configuration);
builder.Services.AddFeatures();
builder.Services.AddValidation();
builder.Services.AddErrorHandling();

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Commerce API v1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapAppEndpoints();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
