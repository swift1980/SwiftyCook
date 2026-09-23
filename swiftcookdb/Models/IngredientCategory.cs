using System.Collections.Generic;

namespace SwiftCookDb.Models
{
    public class IngredientCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }
        public IngredientCategory? ParentCategory { get; set; }

        // Designated catch-all IngredientType for this category, used by
        // Ticket 6's manual ingredient entry to type newly-created ingredients
        // without prompting the user to pick a specific type.
        public int? FallbackTypeId { get; set; }
        public IngredientType? FallbackType { get; set; }

        public ICollection<IngredientCategory> ChildCategories { get; set; } = new List<IngredientCategory>();
        public ICollection<IngredientType> IngredientTypes { get; set; } = new List<IngredientType>();
    }
}
