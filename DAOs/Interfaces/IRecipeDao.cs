using recipe_catalog_api.Models;
using recipe_catalog_api.Models.Enums;

namespace recipe_catalog_api.DAOs.Interfaces;

public interface IRecipeDao
{
    public Task<Recipe?> GetAsync(int id);

    public Task<List<Recipe>> GetByParentsAsync(List<int> parentIds);

    public Task<List<Recipe>> GetAsync(List<string> courses, List<string> cuisines, List<string> tags, RecipeColumn? sortColumn, bool? reverse, string? name);

    public Task<int> CreateFullAsync(RecipeWithSubRecipesRequest recipe);

    public Task UpdateAsync(int id, RecipeRequest recipe);

    public Task<bool> ExistsAsync(int id);

    public Task DeleteAsync(int id);
}
