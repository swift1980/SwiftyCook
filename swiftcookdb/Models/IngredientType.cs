using System.Collections.Generic;

namespace SwiftCookDb.Models
{
    public class IngredientType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
    }
}