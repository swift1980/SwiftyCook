using SwiftCookDb.Models;

namespace SwiftCookDb.Models
{
    public class Cooked
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; } = null!;

        public DateTime Date { get; set; }
    }
}