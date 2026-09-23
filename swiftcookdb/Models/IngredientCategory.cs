using System.Collections.Generic;

namespace SwiftCookDb.Models
{
    public class IngredientCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }
        public IngredientCategory? ParentCategory { get; set; }

        public ICollection<IngredientCategory> ChildCategories { get; set; } = new List<IngredientCategory>();
        public ICollection<IngredientType> IngredientTypes { get; set; } = new List<IngredientType>();
    }
}
