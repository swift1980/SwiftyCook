using Microsoft.EntityFrameworkCore;
using SwiftCookDb.Models;

namespace SwiftCookDb
{
    public class SwiftCookDbContext : DbContext
    {
        public SwiftCookDbContext(DbContextOptions<SwiftCookDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Tool> Tools { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<IngredientType> IngredientTypes { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<RecipeCategory> RecipeCategories { get; set; }
        public DbSet<RecipeTag> RecipeTags { get; set; }
        public DbSet<RecipeTool> RecipeTools { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<RecipeInstruction> RecipeInstructions { get; set; }
        public DbSet<Nutrition> Nutrition { get; set; }
        public DbSet<Cooked> Cooked { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<ShoppingList> ShoppingLists { get; set; }
        public DbSet<Cupboard> Cupboards { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Map table names exactly as in init.sql
            modelBuilder.Entity<Category>().ToTable("Category");
            modelBuilder.Entity<Tag>().ToTable("Tag");
            modelBuilder.Entity<Tool>().ToTable("Tool");
            modelBuilder.Entity<Ingredient>().ToTable("Ingredient");
            modelBuilder.Entity<IngredientType>().ToTable("IngredientType");
            modelBuilder.Entity<Unit>().ToTable("Unit");
            modelBuilder.Entity<Recipe>().ToTable("Recipe");
            modelBuilder.Entity<RecipeCategory>().ToTable("RecipeCategory");
            modelBuilder.Entity<RecipeTag>().ToTable("RecipeTag");
            modelBuilder.Entity<RecipeTool>().ToTable("RecipeTool");
            modelBuilder.Entity<RecipeIngredient>().ToTable("RecipeIngredient");
            modelBuilder.Entity<RecipeInstruction>().ToTable("RecipeInstruction");
            modelBuilder.Entity<Nutrition>().ToTable("Nutrition");
            modelBuilder.Entity<Cooked>().ToTable("Cooked");
            modelBuilder.Entity<Note>().ToTable("Note");
            modelBuilder.Entity<ShoppingList>().ToTable("ShoppingList");
            modelBuilder.Entity<Cupboard>().ToTable("Cupboard");

            modelBuilder.Entity<RecipeCategory>().HasKey(rc => new { rc.RecipeId, rc.CategoryId });
            modelBuilder.Entity<RecipeTag>().HasKey(rt => new { rt.RecipeId, rt.TagId });
            modelBuilder.Entity<RecipeTool>().HasKey(rt => new { rt.RecipeId, rt.ToolId });
            modelBuilder.Entity<RecipeIngredient>().HasKey(ri => new { ri.RecipeId, ri.IngredientId, ri.UnitId });
            modelBuilder.Entity<RecipeInstruction>().HasKey(ri => new { ri.RecipeId, ri.Position });
            modelBuilder.Entity<Cupboard>().HasKey(c => c.IngredientId);
            modelBuilder.Entity<Ingredient>()
                .HasOne(i => i.Type)
                .WithMany(t => t.Ingredients)
                .HasForeignKey(i => i.TypeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}