using Microsoft.EntityFrameworkCore;
using SwiftCookDb;

var builder = WebApplication.CreateBuilder(args);

// Register EF Core DbContext
builder.Services.AddDbContext<SwiftCookDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("SwiftCookDatabase"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("SwiftCookDatabase"))));

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
