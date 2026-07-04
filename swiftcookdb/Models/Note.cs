using SwiftCookDb.Models;

namespace SwiftCookDb.Models
{
    public class Note
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; } = null!;

        public string NoteText { get; set; } = string.Empty;
    }
}