using SwiftCookDb.Models;

namespace SwiftCookDb.Models
{
    public class RecipeInstruction
    {
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; } = null!;

        public int Position { get; set; }
        public string Step { get; set; } = string.Empty;
    }
}