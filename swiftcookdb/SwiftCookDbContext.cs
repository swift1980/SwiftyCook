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
        public DbSet<IngredientCategory> IngredientCategories { get; set; }
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
            modelBuilder.Entity<IngredientCategory>().ToTable("IngredientCategory");
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
            modelBuilder.Entity<Cupboard>().HasKey(c => new { c.IngredientId, c.UnitId });

            // Stored as text ("Mass") so the column stays readable in SQL.
            modelBuilder.Entity<Unit>().Property(u => u.Dimension).HasConversion<string>();
            modelBuilder.Entity<Unit>().Property(u => u.ToBaseFactor).HasPrecision(18, 6);
            modelBuilder.Entity<RecipeIngredient>().Property(r => r.Amount).HasPrecision(12, 4);
            modelBuilder.Entity<RecipeIngredient>().Property(r => r.Note).HasMaxLength(255);
            modelBuilder.Entity<Cupboard>().Property(c => c.Amount).HasPrecision(12, 4);
            modelBuilder.Entity<ShoppingList>().Property(s => s.Amount).HasPrecision(12, 4);
            modelBuilder.Entity<Ingredient>()
                .HasOne(i => i.Type)
                .WithMany(t => t.Ingredients)
                .HasForeignKey(i => i.TypeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Additive hierarchy layer above IngredientType (Ticket 9). Restrict (not
            // Cascade) so deleting a category can't silently wipe out ingredient types
            // (and transitively, ingredients) beneath it — category CRUD is deferred
            // anyway, so this only matters for future admin tooling.
            modelBuilder.Entity<IngredientCategory>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.ChildCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IngredientType>()
                .HasOne(t => t.Category)
                .WithMany(c => c.IngredientTypes)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ticket 6: each category's designated catch-all IngredientType for
            // manually-created ingredients. SetNull (not Restrict) since losing a
            // fallback type shouldn't block anything else. No inverse navigation
            // collection on IngredientType — it's a one-off pointer, not a
            // meaningful "categories that use me as fallback" relationship.
            modelBuilder.Entity<IngredientCategory>()
                .HasOne(c => c.FallbackType)
                .WithMany()
                .HasForeignKey(c => c.FallbackTypeId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}