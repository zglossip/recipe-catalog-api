using recipe_catalog_api.Models;
using recipe_catalog_api.Models.Enums;
using recipe_catalog_api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace recipe_catalog_api.Controllers;

[ApiController]
[Route("recipe")]
public class RecipeController(IIngredientService ingredientService, IInstructionService instructionService, IRecipeService recipeService) : ControllerBase
{

    private readonly IIngredientService _ingredientService = ingredientService;
    private readonly IInstructionService _instructionService = instructionService;
    private readonly IRecipeService _recipeService = recipeService;

    [HttpGet("{id}")]
    public async Task<ActionResult<Recipe>> Get(int id)
    {
        Recipe? recipe = await _recipeService.GetAsync(id);

        if (recipe == null)
        {
            return NotFound();
        }

        return recipe;
    }

    [HttpGet]
    public async Task<ActionResult<List<Recipe>>> Get([FromQuery(Name = "course")] List<string> courses, [FromQuery(Name = "cuisine")] List<string> cuisines, [FromQuery(Name = "tag")] List<string> tags, [FromQuery(Name = "sort")] string? sort, [FromQuery(Name = "reverse")] bool? reverse, [FromQuery(Name = "name")] string? name)
    {
        RecipeColumn? sortColumn = null;

        if (sort == "id")
        {
            sortColumn = RecipeColumn.ID;
        }
        else if (sort == "name")
        {
            sortColumn = RecipeColumn.NAME;
        }

        List<Recipe> recipes = await _recipeService.GetAsync(courses, cuisines, tags, sortColumn, reverse, name);

        return recipes;
    }

    [HttpPost]
    public async Task<IActionResult> CreateFull(RecipeWithSubRecipesRequest recipe)
    {
        int id = await _recipeService.CreateFullAsync(recipe);
        return StatusCode(201, new { id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, RecipeRequest recipe)
    {
        if (!await _recipeService.ExistsAsync(id))
        {
            return NotFound();
        }

        await _recipeService.UpdateAsync(id, recipe);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _recipeService.ExistsAsync(id))
        {
            return NotFound();
        }

        await _recipeService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id}/ingredients")]
    public async Task<ActionResult<IngredientList>> GetIngredients(int id)
    {
        IngredientList ingredientList = await _ingredientService.GetAsync(id);

        return ingredientList;
    }

    [HttpPut("{id}/ingredients")]
    public async Task<IActionResult> UpdateIngredients(int id, IngredientList ingredientList)
    {
        if (!await _recipeService.ExistsAsync(id))
        {
            return NotFound();
        }

        ingredientList.RecipeId = id;
        await _ingredientService.UpdateAsync(ingredientList);
        return NoContent();
    }

    [HttpGet("{id}/instructions")]
    public async Task<ActionResult<InstructionList>> GetInstructions(int id)
    {
        InstructionList instructionList = await _instructionService.GetAsync(id);

        return instructionList;
    }

    [HttpPut("{id}/instructions")]
    public async Task<IActionResult> UpdateInstructions(int id, InstructionList instructionList)
    {
        if (!await _recipeService.ExistsAsync(id))
        {
            return NotFound();
        }

        instructionList.RecipeId = id;
        await _instructionService.UpdateAsync(instructionList);
        return NoContent();
    }
}
