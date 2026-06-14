namespace recipe_catalog_api.Models;

public class RecipeWithSubRecipesRequest : FullRecipeRequest
{
    public List<FullRecipeRequest> SubRecipes { get; set; } = [];
}
