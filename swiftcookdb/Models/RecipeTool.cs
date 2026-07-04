using SwiftCookDb.Models;

namespace SwiftCookDb.Models
{
    public class RecipeTool
    {
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; } = null!;

        public int ToolId { get; set; }
        public Tool Tool { get; set; } = null!;
    }
}