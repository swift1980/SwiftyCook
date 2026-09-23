using System.Collections.Generic;

namespace SwiftCookDb.Models
{
    public class IngredientType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public IngredientCategory? Category { get; set; }

        public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
    }
}