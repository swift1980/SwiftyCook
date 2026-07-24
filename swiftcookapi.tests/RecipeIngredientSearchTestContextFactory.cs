using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SwiftCookDb;
using SwiftCookDb.Models;

namespace swiftcookapi.tests
{
    /// <summary>
    /// Creates an isolated SQLite in-memory <see cref="SwiftCookDbContext"/> and seeds
    /// a small ingredient/recipe graph used across the search tests.
    ///
    /// Ingredient master list:
    ///   1 Flour, 2 Sugar, 3 Butter, 4 Egg, 5 Salt, 6 Vanilla
    ///
    /// Recipes:
    ///   10 "Cake"     -> Flour(x2 units), Sugar, Butter, Egg     (distinct: 1,2,3,4)
    ///   11 "Cookie"   -> Flour, Sugar, Butter                    (distinct: 1,2,3)
    ///   12 "Omelette" -> Egg, Salt                               (distinct: 4,5)
    /// </summary>
    public sealed class RecipeIngredientSearchTestContextFactory : IDisposable
    {
        private readonly SqliteConnection _connection;

        public RecipeIngredientSearchTestContextFactory()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<SwiftCookDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = new SwiftCookDbContext(options);
            context.Database.EnsureCreated();
            Seed(context);
        }

        public SwiftCookDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<SwiftCookDbContext>()
                .UseSqlite(_connection)
                .Options;
            return new SwiftCookDbContext(options);
        }

        private static void Seed(SwiftCookDbContext context)
        {
            context.Ingredients.AddRange(
                new Ingredient { Id = 1, Name = "Flour", PluralName = "Flour" },
                new Ingredient { Id = 2, Name = "Sugar", PluralName = "Sugar" },
                new Ingredient { Id = 3, Name = "Butter", PluralName = "Butter" },
                new Ingredient { Id = 4, Name = "Egg", PluralName = "Eggs" },
                new Ingredient { Id = 5, Name = "Salt", PluralName = "Salt" },
                new Ingredient { Id = 6, Name = "Vanilla", PluralName = "Vanilla" });

            context.Units.AddRange(
                new Unit { Id = 1, Name = "cup" },
                new Unit { Id = 2, Name = "tbsp" });

            context.Recipes.AddRange(
                new Recipe { Id = 10, Name = "Cake" },
                new Recipe { Id = 11, Name = "Cookie" },
                new Recipe { Id = 12, Name = "Omelette" });

            context.RecipeIngredients.AddRange(
                // Cake: Flour appears TWICE with different units (the 7a duplicate trap)
                new RecipeIngredient { RecipeId = 10, IngredientId = 1, UnitId = 1 },
                new RecipeIngredient { RecipeId = 10, IngredientId = 1, UnitId = 2 },
                new RecipeIngredient { RecipeId = 10, IngredientId = 2, UnitId = 1 },
                new RecipeIngredient { RecipeId = 10, IngredientId = 3, UnitId = 2 },
                new RecipeIngredient { RecipeId = 10, IngredientId = 4, UnitId = 1 },
                // Cookie
                new RecipeIngredient { RecipeId = 11, IngredientId = 1, UnitId = 1 },
                new RecipeIngredient { RecipeId = 11, IngredientId = 2, UnitId = 1 },
                new RecipeIngredient { RecipeId = 11, IngredientId = 3, UnitId = 2 },
                // Omelette
                new RecipeIngredient { RecipeId = 12, IngredientId = 4, UnitId = 1 },
                new RecipeIngredient { RecipeId = 12, IngredientId = 5, UnitId = 1 });

            context.SaveChanges();
        }

        public void Dispose() => _connection.Dispose();
    }
}