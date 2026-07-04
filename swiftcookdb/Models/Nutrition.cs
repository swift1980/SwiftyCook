using SwiftCookDb.Models;

namespace SwiftCookDb.Models
{
    public class Nutrition
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; } = null!;

        public int? Calories { get; set; }
        public float? Carbs { get; set; }
        public float? Protein { get; set; }
    }
}