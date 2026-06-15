using recipe_catalog_api.DAOs.Interfaces;
using recipe_catalog_api.DAOs.Mappers;
using recipe_catalog_api.DAOs.Util;

using Npgsql;

namespace recipe_catalog_api.DAOs;

public class CourseDao(IDatabaseConnectionSupplier databaseConnectionSupplier) : ICourseDao
{

    private readonly IDatabaseConnectionSupplier _databaseConnectionSupplier = databaseConnectionSupplier;

    public async Task CreateAsync(List<string> courses, int recipeId)
    {
        string sql = " INSERT INTO recipe_catalog.course" +
                     " (recipe_id, text)" +
                     " VALUES (@recipeId, @text)";

        foreach (string course in courses)
        {
            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>(){
                new NpgsqlParameter("@recipeId", recipeId),
                new NpgsqlParameter("@text", course)
            };

            await DaoUtil.ExecuteAsync(_databaseConnectionSupplier.GetConnectionString(), sql, parameters);
        }
    }

    public Task DeleteAsync(int recipeId)
    {
        string sql = " DELETE FROM recipe_catalog.course" +
                     " WHERE recipe_id = @recipeId";

        return DaoUtil.ExecuteAsync(_databaseConnectionSupplier.GetConnectionString(), sql, new List<NpgsqlParameter>() { new NpgsqlParameter("@recipeId", recipeId) });
    }

    public Task<Dictionary<int, List<string>>> GetByRecipeIdsAsync(List<int> recipeIds)
    {
        if (recipeIds.Count == 0)
        {
            return Task.FromResult(new Dictionary<int, List<string>>());
        }

        QueryParamList<int> recipeIdParamList = new QueryParamList<int>("recipeId", recipeIds);
        string sql = " SELECT recipe_id, text" +
                     " FROM recipe_catalog.course" +
                     " WHERE recipe_id IN (" + recipeIdParamList.GetQueryString() + ")";

        List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();
        recipeIdParamList.PopulateParamList(parameters);

        return DaoUtil.QueryForLookupAsync(_databaseConnectionSupplier.GetConnectionString(), sql, "recipe_id", "text", parameters);
    }
}
