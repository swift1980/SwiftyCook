using Microsoft.EntityFrameworkCore;
using SwiftCookDb;

var builder = WebApplication.CreateBuilder(args);

// Register EF Core DbContext (Pomelo provider targeting MariaDB)
builder.Services.AddDbContext<SwiftCookDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("SwiftCookDatabase"),
        new MariaDbServerVersion(new Version(11, 4, 0))));

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", policy =>
    {
        policy.WithOrigins("http://localhost:5173")  // Vue dev server
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
