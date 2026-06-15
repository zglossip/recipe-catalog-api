using recipe_catalog_api.DAOs.Mappers;

using Npgsql;

namespace recipe_catalog_api.DAOs.Util;

public class DaoUtil
{

    public static async Task<T?> QueryAsync<T>(string connectionString, string sql, AbstractMapper<T> mapper, List<NpgsqlParameter>? parameters = null)
    {
        List<T> results = await QueryForListAsync(connectionString, sql, mapper, parameters);

        if (results.Count > 1)
        {
            throw new Exception($"More than one result for query: {sql}");
        }
        return results.Count == 1 ? results[0] : default;
    }

    public static async Task<List<T>> QueryForListAsync<T>(string connectionString, string sql, AbstractMapper<T> mapper, List<NpgsqlParameter>? parameters = null)
    {
        List<T> list = new List<T>();

        await using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
        await using NpgsqlCommand command = new NpgsqlCommand(sql, connection);

        parameters?.ForEach(p => command.Parameters.Add(p));

        await connection.OpenAsync();

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(mapper.Invoke(reader));
        }

        return list;
    }

    public static async Task<Dictionary<int, List<string>>> QueryForLookupAsync(string connectionString, string sql, string keyColumn, string valueColumn, List<NpgsqlParameter>? parameters = null)
    {
        Dictionary<int, List<string>> lookup = new Dictionary<int, List<string>>();

        await using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
        await using NpgsqlCommand command = new NpgsqlCommand(sql, connection);

        parameters?.ForEach(p => command.Parameters.Add(p));

        await connection.OpenAsync();

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            int key = reader.GetInt32(reader.GetOrdinal(keyColumn));
            string value = reader.GetString(reader.GetOrdinal(valueColumn));

            if (!lookup.TryGetValue(key, out List<string>? values))
            {
                values = new List<string>();
                lookup[key] = values;
            }
            values.Add(value);
        }

        return lookup;
    }

    public static async Task ExecuteAsync(string connectionString, string sql, List<NpgsqlParameter>? parameters = null)
    {
        await using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
        await using NpgsqlCommand command = new NpgsqlCommand(sql, connection);

        parameters?.ForEach(p => command.Parameters.Add(p));

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }

    public static async Task<int> CreateAsync(string connectionString, string sql, List<NpgsqlParameter>? parameters = null)
    {
        await using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
        await using NpgsqlCommand command = new NpgsqlCommand(sql, connection);

        parameters?.ForEach(p => command.Parameters.Add(p));

        await connection.OpenAsync();
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}
