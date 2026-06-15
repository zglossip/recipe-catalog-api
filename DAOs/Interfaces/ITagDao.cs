namespace recipe_catalog_api.DAOs.Interfaces;

public interface ITagDao
{
    public Task<Dictionary<int, List<string>>> GetByRecipeIdsAsync(List<int> recipeIds);

    public Task DeleteAsync(int recipeId);

    public Task CreateAsync(List<string> tags, int recipeId);
}
