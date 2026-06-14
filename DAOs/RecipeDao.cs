using recipe_catalog_api.Models;
using recipe_catalog_api.DAOs.Interfaces;
using recipe_catalog_api.DAOs.Mappers;
using recipe_catalog_api.DAOs.Util;

using Npgsql;
using recipe_catalog_api.Models.Enums;

namespace recipe_catalog_api.DAOs;

public class RecipeDao(IDatabaseConnectionSupplier databaseConnectionSupplier) : IRecipeDao
{

    private readonly IDatabaseConnectionSupplier _databaseConnectionSupplier = databaseConnectionSupplier;

    public Task<Recipe?> GetAsync(int id)
    {
        string sql = " SELECT id, name, serving_amount, serving_name, source, uploaded, parent_id" +
                     " FROM recipe_catalog.recipe" +
                     " WHERE id = @recipeId";

        return DaoUtil.QueryAsync(_databaseConnectionSupplier.GetConnectionString(), sql, new RecipeMapper(), new List<NpgsqlParameter>() { new NpgsqlParameter("@recipeId", id) });
    }

    public Task<List<Recipe>> GetByParentAsync(int parentId)
    {
        string sql = " SELECT id, name, serving_amount, serving_name, source, uploaded, parent_id" +
                     " FROM recipe_catalog.recipe" +
                     " WHERE parent_id = @parentId";

        return DaoUtil.QueryForListAsync(_databaseConnectionSupplier.GetConnectionString(), sql, new RecipeMapper(), new List<NpgsqlParameter>() { new NpgsqlParameter("@parentId", parentId) });
    }

    public Task<List<Recipe>> GetAsync(List<string> courses, List<string> cuisines, List<string> tags, RecipeColumn? sortColumn, bool? reverse, string? name)
    {
        QueryParamList<string> courseQueryParamList = new QueryParamList<string>("course", courses);
        QueryParamList<string> cuisineQueryParamList = new QueryParamList<string>("cuisine", cuisines);
        QueryParamList<string> tagQueryParamList = new QueryParamList<string>("tag", tags);

        string courseQuery = courseQueryParamList.GetQueryString();
        string cuisineQuery = cuisineQueryParamList.GetQueryString();
        string tagQuery = tagQueryParamList.GetQueryString();

        if (string.IsNullOrEmpty(courseQuery))
        {
            courseQuery = "''"; // replace empty list with default value
        }

        if (string.IsNullOrEmpty(cuisineQuery))
        {
            cuisineQuery = "''"; // replace empty list with default value
        }

        if (string.IsNullOrEmpty(tagQuery))
        {
            tagQuery = "''"; // replace empty list with default value
        }

        string sql = "SELECT r.id, r.name, r.serving_amount, r.serving_name, r.source, r.uploaded, r.parent_id " +
                     "FROM recipe_catalog.recipe r " +
                     "WHERE (@courseLength = 0 OR (SELECT COUNT(*) AS courseCount " +
                     "        FROM recipe_catalog.course " +
                     "        WHERE text IN (" + courseQuery + ") " +
                     "          AND recipe_id = r.id) = @courseLength) " +
                     "  AND (@cuisineLength = 0 OR (SELECT COUNT(*) AS cuisineCount " +
                     "        FROM recipe_catalog.cuisine " +
                     "        WHERE text IN (" + cuisineQuery + ") " +
                     "          AND recipe_id = r.id) = @cuisineLength) " +
                     "  AND (@tagLength = 0 OR (SELECT COUNT(*) AS tagCount " +
                     "        FROM recipe_catalog.tag " +
                     "        WHERE text IN (" + tagQuery + ") " +
                     "          AND recipe_id = r.id) = @tagLength)" +
                     "  AND (@name IS NULL OR UPPER(r.name) LIKE UPPER(@name))";

        string orderByColumn = sortColumn switch
        {
            RecipeColumn.NAME => "r.name",
            _ => "r.id"
        };
        string direction = (reverse ?? false) ? "DESC" : "ASC";
        sql += $" ORDER BY {orderByColumn} {direction}";

        List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();
        courseQueryParamList.PopulateParamList(parameters);
        cuisineQueryParamList.PopulateParamList(parameters);
        tagQueryParamList.PopulateParamList(parameters);
        parameters.Add(new NpgsqlParameter("@courseLength", courses.Count));
        parameters.Add(new NpgsqlParameter("@cuisineLength", cuisines.Count));
        parameters.Add(new NpgsqlParameter("@tagLength", tags.Count));
        parameters.Add(new NpgsqlParameter("@name", NpgsqlTypes.NpgsqlDbType.Text)
        {
            Value = name == null ? (object)DBNull.Value : "%" + name + "%"
        });

        return DaoUtil.QueryForListAsync(_databaseConnectionSupplier.GetConnectionString(), sql, new RecipeMapper(), parameters);
    }

    public async Task<int> CreateFullAsync(RecipeWithSubRecipesRequest recipe)
    {
        await using NpgsqlConnection connection = new NpgsqlConnection(_databaseConnectionSupplier.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();

        try
        {
            int id = await InsertFullRecipeAsync(connection, transaction, recipe, recipe.ParentId);

            foreach (FullRecipeRequest subRecipe in recipe.SubRecipes)
            {
                await InsertFullRecipeAsync(connection, transaction, subRecipe, id);
            }

            await transaction.CommitAsync();
            return id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<int> InsertFullRecipeAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, FullRecipeRequest recipe, int? parentId)
    {
        await using NpgsqlCommand recipeCmd = new(
            "INSERT INTO recipe_catalog.recipe (name, serving_amount, serving_name, source, parent_id) VALUES (@name, @servingAmount, @servingName, @source, @parentId) RETURNING id",
            connection, transaction);
        recipeCmd.Parameters.Add(new NpgsqlParameter("@name", recipe.Name));
        recipeCmd.Parameters.Add(new NpgsqlParameter("@servingAmount", recipe.ServingAmount));
        recipeCmd.Parameters.Add(new NpgsqlParameter("@servingName", recipe.ServingName));
        recipeCmd.Parameters.Add(new NpgsqlParameter("@source", recipe.Source == null ? DBNull.Value : recipe.Source));
        recipeCmd.Parameters.Add(new NpgsqlParameter("@parentId", parentId == null ? DBNull.Value : parentId));
        int id = Convert.ToInt32(await recipeCmd.ExecuteScalarAsync());

        foreach (string course in recipe.CourseTypes)
        {
            await using NpgsqlCommand cmd = new(
                "INSERT INTO recipe_catalog.course (recipe_id, text) VALUES (@recipeId, @text)",
                connection, transaction);
            cmd.Parameters.Add(new NpgsqlParameter("@recipeId", id));
            cmd.Parameters.Add(new NpgsqlParameter("@text", course));
            await cmd.ExecuteNonQueryAsync();
        }

        foreach (string cuisine in recipe.CuisineTypes)
        {
            await using NpgsqlCommand cmd = new(
                "INSERT INTO recipe_catalog.cuisine (recipe_id, text) VALUES (@recipeId, @text)",
                connection, transaction);
            cmd.Parameters.Add(new NpgsqlParameter("@recipeId", id));
            cmd.Parameters.Add(new NpgsqlParameter("@text", cuisine));
            await cmd.ExecuteNonQueryAsync();
        }

        foreach (string tag in recipe.Tags)
        {
            await using NpgsqlCommand cmd = new(
                "INSERT INTO recipe_catalog.tag (recipe_id, text) VALUES (@recipeId, @text)",
                connection, transaction);
            cmd.Parameters.Add(new NpgsqlParameter("@recipeId", id));
            cmd.Parameters.Add(new NpgsqlParameter("@text", tag));
            await cmd.ExecuteNonQueryAsync();
        }

        int position = 0;
        foreach (Ingredient ingredient in recipe.Ingredients)
        {
            await using NpgsqlCommand cmd = new(
                "INSERT INTO recipe_catalog.INGREDIENT (RECIPE_ID, POSITION, NAME, QUANTITY, UOM, NOTES) VALUES (@recipeId, @position, @name, @quantity, @uom, @notes)",
                connection, transaction);
            cmd.Parameters.Add(new NpgsqlParameter("@recipeId", id));
            cmd.Parameters.Add(new NpgsqlParameter("@position", position++));
            cmd.Parameters.Add(new NpgsqlParameter("@name", ingredient.Name));
            cmd.Parameters.Add(new NpgsqlParameter("@quantity", ingredient.Quantity));
            cmd.Parameters.Add(new NpgsqlParameter("@uom", ingredient.Uom == null ? DBNull.Value : ingredient.Uom));
            cmd.Parameters.Add(new NpgsqlParameter("@notes", ingredient.Notes == null ? DBNull.Value : ingredient.Notes));
            await cmd.ExecuteNonQueryAsync();
        }

        position = 0;
        foreach (string instruction in recipe.Instructions)
        {
            await using NpgsqlCommand cmd = new(
                "INSERT INTO recipe_catalog.INSTRUCTION (RECIPE_ID, POSITION, TEXT) VALUES (@recipeId, @position, @text)",
                connection, transaction);
            cmd.Parameters.Add(new NpgsqlParameter("@recipeId", id));
            cmd.Parameters.Add(new NpgsqlParameter("@position", position++));
            cmd.Parameters.Add(new NpgsqlParameter("@text", instruction));
            await cmd.ExecuteNonQueryAsync();
        }

        return id;
    }

    public Task UpdateAsync(int id, RecipeRequest recipe)
    {
        string sql = " UPDATE recipe_catalog.recipe" +
                     " SET name = @name, serving_amount = @servingAmount, serving_name = @servingName, source = @source" +
                     " WHERE id = @recipeId";

        List<NpgsqlParameter> parameters = new List<NpgsqlParameter>()
        {
            new NpgsqlParameter("@name", recipe.Name),
            new NpgsqlParameter("@servingAmount", recipe.ServingAmount),
            new NpgsqlParameter("@servingName", recipe.ServingName),
            new NpgsqlParameter("@source", recipe.Source == null ? DBNull.Value : recipe.Source),
            new NpgsqlParameter("@recipeId", id)
        };

        return DaoUtil.ExecuteAsync(_databaseConnectionSupplier.GetConnectionString(), sql, parameters);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        string sql = "SELECT 1 FROM recipe_catalog.recipe WHERE id = @recipeId LIMIT 1";
        await using NpgsqlConnection connection = new NpgsqlConnection(_databaseConnectionSupplier.GetConnectionString());
        await using NpgsqlCommand command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter("@recipeId", id));
        await connection.OpenAsync();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync();
    }

    public Task DeleteAsync(int id)
    {
        string sql = " DELETE FROM recipe_catalog.recipe" +
                     " WHERE id = @recipeId";

        return DaoUtil.ExecuteAsync(_databaseConnectionSupplier.GetConnectionString(), sql, new List<NpgsqlParameter>() { new NpgsqlParameter("@recipeId", id) });
    }
}
