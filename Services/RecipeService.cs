using recipe_catalog_api.Models;
using recipe_catalog_api.DAOs.Interfaces;
using recipe_catalog_api.Services.Interfaces;
using recipe_catalog_api.Models.Enums;

namespace recipe_catalog_api.Services;

public class RecipeService(IRecipeDao recipeDao, ICourseDao courseDao, ICuisineDao cuisineDao, ITagDao tagDao) : IRecipeService
{
    private readonly IRecipeDao _recipeDao = recipeDao;
    private readonly ICourseDao _courseDao = courseDao;
    private readonly ICuisineDao _cuisineDao = cuisineDao;
    private readonly ITagDao _tagDao = tagDao;

    public async Task<Recipe?> GetAsync(int id)
    {
        return await _getPopulatedRecipeAsync(await _recipeDao.GetAsync(id));
    }

    public async Task<List<Recipe>> GetAsync(List<string> courses, List<string> cuisines, List<string> tags, RecipeColumn? sortColumn, bool? reverse, string? name)
    {
        List<Recipe> recipes = await _recipeDao.GetAsync(courses, cuisines, tags, sortColumn, reverse, name);
        List<Recipe> result = new List<Recipe>(recipes.Count);
        foreach (Recipe recipe in recipes)
        {
            Recipe? newRecipe = await _getPopulatedRecipeAsync(recipe);
            if (newRecipe != null) result.Add(newRecipe);
        }
        return result;
    }

    private async Task<Recipe?> _getPopulatedRecipeAsync(Recipe? recipe)
    {
        if (recipe == null)
        {
            return null;
        }
        Recipe newRecipe = await _populateRecipeDetailsAsync(recipe);

        List<Recipe> subRecipes = await _recipeDao.GetByParentAsync(recipe.Id);
        foreach (Recipe subRecipe in subRecipes)
        {
            newRecipe.SubRecipes.Add(await _populateRecipeDetailsAsync(subRecipe));
        }

        return newRecipe;
    }

    private async Task<Recipe> _populateRecipeDetailsAsync(Recipe recipe)
    {
        Recipe newRecipe = recipe.Clone();
        newRecipe.CourseTypes = await _courseDao.GetAsync(recipe.Id);
        newRecipe.CuisineTypes = await _cuisineDao.GetAsync(recipe.Id);
        newRecipe.Tags = await _tagDao.GetAsync(recipe.Id);
        return newRecipe;
    }

    public Task<int> CreateFullAsync(RecipeWithSubRecipesRequest recipe) => _recipeDao.CreateFullAsync(recipe);

    public async Task UpdateAsync(int id, RecipeRequest recipe)
    {
        await _recipeDao.UpdateAsync(id, recipe);
        await _courseDao.DeleteAsync(id);
        await _courseDao.CreateAsync(recipe.CourseTypes, id);
        await _cuisineDao.DeleteAsync(id);
        await _cuisineDao.CreateAsync(recipe.CuisineTypes, id);
        await _tagDao.DeleteAsync(id);
        await _tagDao.CreateAsync(recipe.Tags, id);
    }

    public Task<bool> ExistsAsync(int id) => _recipeDao.ExistsAsync(id);

    public Task DeleteAsync(int id) => _recipeDao.DeleteAsync(id);
}
