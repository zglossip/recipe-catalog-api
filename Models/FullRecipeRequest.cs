using System.ComponentModel.DataAnnotations;

namespace recipe_catalog_api.Models;

public class FullRecipeRequest : RecipeRequest
{
    [Required]
    public List<Ingredient> Ingredients { get; set; } = [];

    [Required]
    public List<string> Instructions { get; set; } = [];

    public int? ParentId { get; set; }
}
