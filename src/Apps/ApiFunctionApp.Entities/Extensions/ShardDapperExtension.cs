using Dapper;
using ApiFunctionApp.Entities.Shards;

namespace ApiFunctionApp.Entities.Extensions
{
    public static class ShardDapperExtensions
    {
        public static async Task<int> ExecuteSqlAsync(this Shard shard, int shardId, string sql, object? parameters = null, int commandTimeout = 300)
        {
            await using var connection = await shard.OpenConnectionAsync(shardId);
            var transaction = await connection.BeginTransactionAsync();
            var result = await connection.ExecuteAsync(sql, parameters, transaction);
            await transaction.CommitAsync();
            await connection.CloseAsync();

            return result;
        }

        public static async Task<IEnumerable<T>> QuerySqlAsync<T>(this Shard shard, int shardId, string sql, object? parameters = null, int commandTimeout = 300)
        {
            await using var connection = await shard.OpenConnectionAsync(shardId);
            var result = await connection.QueryAsync<T>(sql, parameters);
            await connection.CloseAsync();

            return result ?? new List<T>();
        }

        public static async Task<T> QuerySingleSqlAsync<T>(this Shard shard, int shardId, string sql, object? parameters = null, int commandTimeout = 300)
        {
            await using var connection = await shard.OpenConnectionAsync(shardId);
            var result = await connection.QuerySingleAsync<T>(sql, parameters);
            await connection.CloseAsync();

            return result;
        }
    }
}
