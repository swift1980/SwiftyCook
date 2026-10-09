public class UnitDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Abbreviation { get; set; }
    public string Dimension { get; set; } = "Other";
    public decimal? ToBaseFactor { get; set; }
}
