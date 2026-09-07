using Microsoft.EntityFrameworkCore;
using SwiftCookDb;

var builder = WebApplication.CreateBuilder(args);

// Build the connection string from DB_* environment variables (set by docker-compose)
// when present; otherwise fall back to the ConnectionStrings:SwiftCookDatabase setting
// in appsettings.json (used for local, non-containerized development).
var dbHost = builder.Configuration["DB_HOST"];
var dbUser = builder.Configuration["DB_USER"];
var dbPassword = builder.Configuration["DB_PASSWORD"];
var dbName = builder.Configuration["DB_NAME"];

var connectionString = !string.IsNullOrEmpty(dbHost)
    ? $"Server={dbHost};Port=3306;Database={dbName};User={dbUser};Password={dbPassword};"
    : builder.Configuration.GetConnectionString("SwiftCookDatabase");

// Register EF Core DbContext (Pomelo provider targeting MariaDB)
builder.Services.AddDbContext<SwiftCookDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MariaDbServerVersion(new Version(11, 4, 0))));

builder.Services.AddControllers();

// CORS allowed origins are config-driven (Cors:AllowedOrigins), defaulting to the
// Vue dev server if not configured. Override via appsettings/environment per environment.
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register Swagger (API documentation)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddScoped<swiftcookapi.Services.IRecipeIngredientSearchService,
    swiftcookapi.Services.RecipeIngredientSearchService>();

var app = builder.Build();

app.UseCors("AllowUI");

// Use Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SwiftCook API v1");
        c.RoutePrefix = "swagger"; // UI at http://localhost:5000/swagger
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
