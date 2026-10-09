using System.ComponentModel.DataAnnotations;

public class ShoppingListPurchaseDto
{
    /// <summary>The shopping list rows the user confirmed; exactly these are moved to the Cupboard.</summary>
    [Required, MinLength(1)]
    public List<int> ItemIds { get; set; } = new();
}
