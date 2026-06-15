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
        Recipe? recipe = await _recipeDao.GetAsync(id);
        if (recipe == null)
        {
            return null;
        }
        return (await _populateAsync(new List<Recipe> { recipe }))[0];
    }

    public async Task<List<Recipe>> GetAsync(List<string> courses, List<string> cuisines, List<string> tags, RecipeColumn? sortColumn, bool? reverse, string? name)
    {
        List<Recipe> recipes = await _recipeDao.GetAsync(courses, cuisines, tags, sortColumn, reverse, name);
        return await _populateAsync(recipes);
    }

    private async Task<List<Recipe>> _populateAsync(List<Recipe> topLevelRecipes)
    {
        if (topLevelRecipes.Count == 0)
        {
            return new List<Recipe>();
        }

        List<int> topLevelIds = topLevelRecipes.Select(recipe => recipe.Id).ToList();
        List<Recipe> children = await _recipeDao.GetByParentsAsync(topLevelIds);

        List<int> allIds = topLevelIds.Concat(children.Select(child => child.Id)).ToList();
        Dictionary<int, List<string>> coursesById = await _courseDao.GetByRecipeIdsAsync(allIds);
        Dictionary<int, List<string>> cuisinesById = await _cuisineDao.GetByRecipeIdsAsync(allIds);
        Dictionary<int, List<string>> tagsById = await _tagDao.GetByRecipeIdsAsync(allIds);

        Dictionary<int, List<Recipe>> childrenByParent = new Dictionary<int, List<Recipe>>();
        foreach (Recipe child in children)
        {
            Recipe populatedChild = _withDetails(child, coursesById, cuisinesById, tagsById);
            int parentId = child.ParentId!.Value;
            if (!childrenByParent.TryGetValue(parentId, out List<Recipe>? siblings))
            {
                siblings = new List<Recipe>();
                childrenByParent[parentId] = siblings;
            }
            siblings.Add(populatedChild);
        }

        List<Recipe> result = new List<Recipe>(topLevelRecipes.Count);
        foreach (Recipe recipe in topLevelRecipes)
        {
            Recipe populated = _withDetails(recipe, coursesById, cuisinesById, tagsById);
            if (childrenByParent.TryGetValue(recipe.Id, out List<Recipe>? subRecipes))
            {
                populated.SubRecipes = subRecipes;
            }
            result.Add(populated);
        }
        return result;
    }

    private static Recipe _withDetails(Recipe recipe, Dictionary<int, List<string>> coursesById, Dictionary<int, List<string>> cuisinesById, Dictionary<int, List<string>> tagsById)
    {
        Recipe populated = recipe.Clone();
        populated.CourseTypes = coursesById.GetValueOrDefault(recipe.Id) ?? new List<string>();
        populated.CuisineTypes = cuisinesById.GetValueOrDefault(recipe.Id) ?? new List<string>();
        populated.Tags = tagsById.GetValueOrDefault(recipe.Id) ?? new List<string>();
        return populated;
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
