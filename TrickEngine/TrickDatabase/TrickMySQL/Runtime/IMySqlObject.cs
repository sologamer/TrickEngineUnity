#if UNITY_EDITOR || USE_TRICK_MYSQL
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TrickCore
{
    public interface IMySqlObject
    {
        SqlVerboseTypes Verbosity { get; set; }

        /// <summary>
        /// Opens a new MySqlConnection instance.
        /// </summary>
        /// <param name="command">Mysql command</param>
        /// <returns>The new connection</returns>
        MySqlConnection OpenConnection(MySqlCommand command);

        /// <summary>
        /// Opens a new MySqlConnection instance asynchronously.
        /// </summary>
        /// <param name="command">Mysql command</param>
        /// <returns></returns>
        Task<MySqlConnection> OpenConnectionAsync(MySqlCommand command);

        /// <summary>
        /// Starts a new transaction. A helper class will be returned in order to Commit or Revert the results.
        /// </summary>
        /// <returns>The transaction helper class</returns>
        MySqlTransactionHelper BeginTransaction();

        /// <summary>
        /// Starts a new transaction asynchronously. A helper class will be returned in order to Commit or Revert the results.
        /// </summary>
        /// <returns>The transaction helper class</returns>
        Task<MySqlTransactionHelper> BeginTransactionAsync();

        /// <summary>
        /// Select a scalar object.
        /// </summary>
        /// <param name="commandText">The sql query</param>
        /// <param name="data">The object we selected</param>
        /// <returns>Returns a result object</returns>
        MySqlQueryResult ExecuteScalar(string commandText, out object data);

        /// <summary>
        /// Select a scalar object asynchronously.
        /// </summary>
        /// <param name="commandText">The sql query</param>
        /// <returns>Returns a result object</returns>
        Task<MySqlResultSingle<object>> ExecuteScalarAsync(string commandText);

        /// <summary>
        /// Execute a non query which outputs the <see cref="affectedRows"/>.
        /// </summary>
        /// <param name="commandText">The sql query</param>
        /// <param name="affectedRows">The amount of rows we affected.</param>
        /// <returns>Returns a result object</returns>
        MySqlQueryResult ExecuteNonQuery(string commandText, out int affectedRows);

        /// <summary>
        /// Execute a non query asynchronously. The result object contains the amount of affectedRows as result.
        /// </summary>
        /// <param name="commandText">The sql query</param>
        /// <returns>Returns a result object</returns>
        Task<MySqlResultSingle<int>> ExecuteNonQueryAsync(string commandText);

        /// <summary>
        /// Select multiple objects
        /// </summary>
        /// <typeparam name="T">The type we convert to</typeparam>
        /// <param name="commandText">The sql query</param>
        /// <param name="data">The selected objects</param>
        /// <returns>Returns the result object</returns>
        MySqlQueryResult ExecuteReader<T>(string commandText, out List<T> data);

        /// <summary>
        /// Select multiple objects. The result object contains the selected objects as result.
        /// </summary>
        /// <typeparam name="T">The type we convert to</typeparam>
        /// <param name="commandText">The sql query</param>
        /// <returns>Returns the result object</returns>
        Task<MySqlResultMultiple<T>> ExecuteReaderAsync<T>(string commandText);

        /// <summary>
        /// Select a scalar object.
        /// </summary>
        /// <param name="commandText">The sql query</param>
        /// <param name="data">The object we selected</param>
        /// <returns>Returns a result object</returns>
        MySqlQueryResult ExecuteStoredProcedureScalar(string commandText, List<MySqlParameter> parameters, out object data);

        /// <summary>
        /// Select a scalar object asynchronously.
        /// </summary>
        /// <param name="commandText">The sql query</param>
        /// <returns>Returns a result object</returns>
        Task<MySqlResultSingle<object>> ExecuteStoredProcedureScalarAsync(string commandText, List<MySqlParameter> parameters);

        /// <summary>
        /// Execute a non query which outputs the <see cref="affectedRows"/>.
        /// </summary>
        /// <param name="commandText">The sql query</param>
        /// <param name="affectedRows">The amount of rows we affected.</param>
        /// <returns>Returns a result object</returns>
        MySqlQueryResult ExecuteStoredProcedureNonQuery(string commandText, List<MySqlParameter> parameters, out int affectedRows);

        /// <summary>
        /// Execute a non query asynchronously. The result object contains the amount of affectedRows as result.
        /// </summary>
        /// <param name="commandText">The sql query</param>
        /// <returns>Returns a result object</returns>
        Task<MySqlResultSingle<int>> ExecuteStoredProcedureNonQueryAsync(string commandText, List<MySqlParameter> parameters);

        /// <summary>
        /// Select multiple objects
        /// </summary>
        /// <typeparam name="T">The type we convert to</typeparam>
        /// <param name="commandText">The sql query</param>
        /// <param name="data">The selected objects</param>
        /// <returns>Returns the result object</returns>
        MySqlQueryResult ExecuteStoredProcedureReader<T>(string commandText, List<MySqlParameter> parameters, out List<T> data);

        /// <summary>
        /// Select multiple objects. The result object contains the selected objects as result.
        /// </summary>
        /// <typeparam name="T">The type we convert to</typeparam>
        /// <param name="commandText">The sql query</param>
        /// <returns>Returns the result object</returns>
        Task<MySqlResultMultiple<T>> ExecuteStoredProcedureReaderAsync<T>(string commandText, List<MySqlParameter> parameters);

        /// <summary>
        /// Create a prepared statement
        /// </summary>
        /// <param name="prepareQuery"></param>
        /// <returns></returns>
        MySqlPrepare CreatePrepareStatement(string prepareQuery);

        /// <summary>
        /// Create a prepared statement
        /// </summary>
        /// <param name="prepareQuery"></param>
        /// <returns></returns>
        Task<MySqlPrepare> CreatePrepareStatementAsync(string prepareQuery);

        /// <summary>
        /// Insert an <see cref="IDatabaseObject"/> item to the database.
        /// </summary>
        /// <param name="item">The object we insert</param>
        /// <param name="affectedRows">The amount of rows affected</param>
        /// <returns>Returns the result object</returns>
        MySqlQueryResult InsertItem(IDatabaseObject item, out int affectedRows);

        /// <summary>
        /// Insert an <see cref="IDatabaseObject"/> item to the database.
        /// </summary>
        /// <param name="item">The object we insert</param>
        /// <param name="predicate">Predicate of include/exclude a table</param>
        /// <param name="affectedRows">The amount of rows affected</param>
        /// <returns>Returns the result object</returns>
        MySqlQueryResult InsertItem(IDatabaseObject item, Predicate<string> predicate, out int affectedRows);

        /// <summary>
        /// Insert an <see cref="IDatabaseObject"/> item asynchronously to the database.
        /// </summary>
        /// <param name="item">The object we insert</param>
        /// <param name="predicate">Column predicate to which one we include (true) or exclude (false)</param>
        /// <returns>Returns the result object</returns>
        Task<MySqlResultSingle<int>> InsertItemAsync(IDatabaseObject item, Predicate<string> predicate = null);

        /// <summary>
        /// Insert an <see cref="IDatabaseObject"/> item to the database.
        /// </summary>
        /// <param name="item">The object we insert</param>
        /// <param name="affectedRows">The amount of rows affected</param>
        /// <param name="includeColumns">The columns to include</param>
        /// <returns>Returns the result object</returns>
        MySqlQueryResult InsertItemInclude(IDatabaseObject item, out int affectedRows, params string[] includeColumns);

        /// <summary>
        /// Insert an <see cref="IDatabaseObject"/> item asynchronously to the database.
        /// </summary>
        /// <param name="item">The object we insert</param>
        /// <param name="includeColumns">The columns to include</param>
        /// <returns>Returns the result object</returns>
        Task<MySqlResultSingle<int>> InsertItemIncludeAsync(IDatabaseObject item, params string[] includeColumns);

        /// <summary>
        /// Insert an <see cref="IDatabaseObject"/> item to the database.
        /// </summary>
        /// <param name="item">The object we insert</param>
        /// <param name="affectedRows">The amount of rows affected</param>
        /// <param name="excludeColumns">The columns to exclude</param>
        /// <returns>Returns the result object</returns>
        MySqlQueryResult InsertItemExclude(IDatabaseObject item, out int affectedRows, params string[] excludeColumns);

        /// <summary>
        /// Insert an <see cref="IDatabaseObject"/> item asynchronously to the database.
        /// </summary>
        /// <param name="item">The object we insert</param>
        /// <param name="excludeColumns">The columns to exclude</param>
        /// <returns>Returns the result object</returns>
        Task<MySqlResultSingle<int>> InsertItemExcludeAsync(IDatabaseObject item, params string[] excludeColumns);


        /// <summary>
        /// Update an <see cref="IDatabaseObject"/> item from the database.
        /// </summary>
        /// <param name="item">The object we update</param>
        /// <param name="affectedRows">The amount of rows affected</param>
        /// <returns>Returns the result object</returns>
        MySqlQueryResult UpdateItem(IDatabaseObject item, out int affectedRows);

        /// <summary>
        /// Update an <see cref="IDatabaseObject"/> item from the database.
        /// </summary>
        /// <param name="item">The object we update</param>
        /// <param name="predicate">Predicate of include/exclude a table</param>
        /// <param name="affectedRows">The amount of rows affected</param>
        /// <returns>Returns the result object</returns>
        MySqlQueryResult UpdateItem(IDatabaseObject item, Predicate<string> predicate, out int affectedRows);

        /// <summary>
        /// Update an <see cref="IDatabaseObject"/> item asynchronously from the database.
        /// </summary>
        /// <param name="item">The object we update</param>
        /// <param name="predicate">Column predicate to which one we include (true) or exclude (false)</param>
        /// <returns>Returns the result object</returns>
        Task<MySqlResultSingle<int>> UpdateItemAsync(IDatabaseObject item, Predicate<string> predicate = null);

        /// <summary>
        /// Update an <see cref="IDatabaseObject"/> item from the database.
        /// </summary>
        /// <param name="item">The object we update</param>
        /// <param name="affectedRows">The amount of rows affected</param>
        /// <param name="includeColumns">The columns to include</param>
        /// <returns>Returns the result object</returns>
        MySqlQueryResult UpdateItemInclude(IDatabaseObject item, out int affectedRows, params string[] includeColumns);

        /// <summary>
        /// Update an <see cref="IDatabaseObject"/> item asynchronously from the database.
        /// </summary>
        /// <param name="item">The object we update</param>
        /// <param name="includeColumns">The columns to include</param>
        /// <returns>Returns the result object</returns>
        Task<MySqlResultSingle<int>> UpdateItemIncludeAsync(IDatabaseObject item, params string[] includeColumns);

        /// <summary>
        /// Update an <see cref="IDatabaseObject"/> item from the database.
        /// </summary>
        /// <param name="item">The object we update</param>
        /// <param name="affectedRows">The amount of rows affected</param>
        /// <param name="excludeColumns">The columns to exclude</param>
        /// <returns>Returns the result object</returns>
        MySqlQueryResult UpdateItemExclude(IDatabaseObject item, out int affectedRows, params string[] excludeColumns);

        /// <summary>
        /// Update an <see cref="IDatabaseObject"/> item asynchronously from the database.
        /// </summary>
        /// <param name="item">The object we update</param>
        /// <param name="excludeColumns">The columns to exclude</param>
        /// <returns>Returns the result object</returns>
        Task<MySqlResultSingle<int>> UpdateItemExcludeAsync(IDatabaseObject item, params string[] excludeColumns);

        /// <summary>
        /// Delete an <see cref="IDatabaseObject"/> item from the database.
        /// </summary>
        /// <param name="item">The object we update</param>
        /// <param name="affectedRows">The amount of rows affected</param>
        /// <returns>Returns the result object</returns>
        MySqlQueryResult DeleteItem(IDatabaseObject item, out int affectedRows);

        /// <summary>
        /// Delete an <see cref="IDatabaseObject"/> item asynchronously from the database.
        /// </summary>
        /// <param name="item">The object we update</param>
        /// <param name="predicate">Column predicate to which one we include (true) or exclude (false)</param>
        /// <returns>Returns the result object</returns>
        Task<MySqlResultSingle<int>> DeleteItemAsync(IDatabaseObject item);
    }
}
#endif