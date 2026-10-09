using System.ComponentModel.DataAnnotations;

public class CookLogUpdateDto
{
    [Required]
    public DateOnly? CookedOn { get; set; }

    [Range(1, 1000)]
    public int Servings { get; set; } = 1;

    [StringLength(500)]
    public string? Notes { get; set; }
}
