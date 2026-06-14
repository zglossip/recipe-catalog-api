using recipe_catalog_api.Models;

namespace recipe_catalog_api.DAOs.Mappers;

public class RecipeMapper : AbstractMapper<Recipe>
{
    public RecipeMapper() : base(reader =>
    {
        string? source = null;
        if (!reader.IsDBNull(reader.GetOrdinal("source")))
        {
            source = reader.GetString(reader.GetOrdinal("source"));
        }

        int? parentId = null;
        if (!reader.IsDBNull(reader.GetOrdinal("parent_id")))
        {
            parentId = reader.GetInt32(reader.GetOrdinal("parent_id"));
        }

        return new Recipe(
            reader.GetInt32(reader.GetOrdinal("id")),
            reader.GetString(reader.GetOrdinal("name")),
            reader.GetInt32(reader.GetOrdinal("serving_amount")),
            reader.GetString(reader.GetOrdinal("serving_name")),
            source,
            reader.GetDateTime(reader.GetOrdinal("uploaded")),
            parentId
        );
    })
    { }
}
