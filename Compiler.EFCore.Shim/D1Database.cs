using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cloudflare.Workers;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    /// D1 Database wrapper with EF Core-compatible API
    /// </summary>
    public class D1Database
    {
        private readonly Workers.D1Database _innerDb;

        public D1Database(Workers.D1Database db)
        {
            _innerDb = db;
        }

        /// <summary>
        /// Execute a SELECT query and return typed results
        /// </summary>
        public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(string sql, object[]? parameters = null)
        {
            var result = await _innerDb.Prepare(sql).Bind(parameters).AllAsync();
            
            var results = new List<T>();
            foreach (var row in result.Results)
            {
                var entity = Activator.CreateInstance<T>();
                foreach (var prop in typeof(T).GetProperties())
                {
                    if (row.TryGetValue(prop.Name, out var value) && value != null)
                    {
                        var converted = ConvertValue(value, prop.PropertyType);
                        prop.SetValue(entity, converted);
                    }
                }
                results.Add(entity);
            }
            
            return results;
        }

        /// <summary>
        /// Execute an INSERT, UPDATE, or DELETE command
        /// </summary>
        public async Task<int> ExecuteCommandAsync(string sql, object[]? parameters = null)
        {
            var result = await _innerDb.Prepare(sql).Bind(parameters).RunAsync();
            return result.RowsAffected ?? 0;
        }

        /// <summary>
        /// Execute a transaction
        /// </summary>
        public async Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> action)
        {
            await _innerDb.Prepare("BEGIN TRANSACTION").RunAsync();
            try
            {
                var result = await action();
                await _innerDb.Prepare("COMMIT").RunAsync();
                return result;
            }
            catch
            {
                await _innerDb.Prepare("ROLLBACK").RunAsync();
                throw;
            }
        }

        private static object? ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return null;

            if (targetType == typeof(int) || targetType == typeof(int?))
                return Convert.ToInt32(value);

            if (targetType == typeof(long) || targetType == typeof(long?))
                return Convert.ToInt64(value);

            if (targetType == typeof(double) || targetType == typeof(double?))
                return Convert.ToDouble(value);

            if (targetType == typeof(decimal) || targetType == typeof(decimal?))
                return Convert.ToDecimal(value);

            if (targetType == typeof(bool) || targetType == typeof(bool?))
                return Convert.ToBoolean(value);

            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
            {
                if (DateTime.TryParse(value.ToString(), out var dt))
                    return dt;
                return null;
            }

            if (targetType == typeof(Guid) || targetType == typeof(Guid?))
            {
                if (Guid.TryParse(value.ToString(), out var g))
                    return g;
                return null;
            }

            return value;
        }
    }

    /// <summary>
    /// Extension methods for DbContext with D1 support
    /// </summary>
    public static class DbContextExtensions
    {
        public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> queryable)
        {
            if (queryable is DbSet<T> dbSet)
            {
                return await dbSet.ToListAsync();
            }
            
            // Fallback for in-memory queries
            return queryable.ToList();
        }

        public static async Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> queryable)
        {
            if (queryable is DbSet<T> dbSet)
            {
                return await dbSet.FirstOrDefaultAsync();
            }
            
            return queryable.FirstOrDefault();
        }

        public static async Task<int> CountAsync<T>(this IQueryable<T> queryable)
        {
            if (queryable is DbSet<T> dbSet)
            {
                return await dbSet.CountAsync();
            }
            
            return queryable.Count();
        }

        public static async Task<bool> AnyAsync<T>(this IQueryable<T> queryable)
        {
            if (queryable is DbSet<T> dbSet)
            {
                return await dbSet.AnyAsync();
            }
            
            return queryable.Any();
        }
    }
}
