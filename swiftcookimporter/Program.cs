using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SwiftCook.Importer;
using SwiftCookDb;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<SwiftCookDbContext>(options =>
            options.UseMySql("Server=localhost;Port=13306;Database=swiftcookdb;User=swiftchef;Password=GL@D0s;",
                new MySqlServerVersion(new Version(10, 11, 6)))); // adjust for your MariaDB version

        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        services.AddTransient<RecipeImporterService>(); // your importer logic
    })
    .Build();

using var scope = host.Services.CreateScope();
var importer = scope.ServiceProvider.GetRequiredService<RecipeImporterService>();

await importer.ImportFromCsvAsync("C:\\Migrate\\source\\repos\\swift1980\\SwiftCook\\swiftcookimporter\\cocktails.csv");