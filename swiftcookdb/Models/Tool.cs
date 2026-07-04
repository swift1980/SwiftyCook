namespace SwiftCookDb.Models
{
    public class Tool
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<RecipeTool> RecipeTools { get; set; } = new List<RecipeTool>();
    }
}