#if UNITY_EDITOR || USE_TRICK_MYSQL
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TrickCore
{
    /// <summary>
    /// A MySql object helper class for doing queries. Supports regular text queries, stored procedures, prepared statements.
    /// </summary>
    public class MySqlObject : IMySqlObject
    {
        private const int SqlQueryTimeout = 10;
        private const int SqlConnectionTimeout = 10;

        /// <summary>
        /// The log verbosity
        /// </summary>
        public SqlVerboseTypes Verbosity { get; set; }

        /// <summary>
        /// The connection string
        /// </summary>
        private string ConnectionString { get; set; }

        /// <summary>
        /// The active transaction
        /// </summary>
        private MySqlTransactionHelper _currentTransactionHelper;

        public MySqlObject(string dbHostName, string dbUser, string dbPass, string dbName, SqlVerboseTypes verbosity = SqlVerboseTypes.LogErrors)
        {
            ConnectionString = "Server=" + dbHostName + ";UserId=" + dbUser + ";Password=" + dbPass +
                               ";Database=" + dbName + ";ConnectionTimeout=" + SqlConnectionTimeout;
            Verbosity = verbosity;
        }
        
        public MySqlObject(string connectionString, SqlVerboseTypes verbosity = SqlVerboseTypes.LogErrors)
        {
            ConnectionString = connectionString;
            Verbosity = verbosity;
        }

        /// <inheritdoc />
        public MySqlConnection OpenConnection(MySqlCommand command)
        {
            if (_currentTransactionHelper == null || _currentTransactionHelper.IsDisposed)
            {
                MySqlConnection client = new MySqlConnection(ConnectionString);
                if (command != null) command.Connection = client;
                client.Open();
                return client;
            }

            if (command != null)
            {
                command.Connection = _currentTransactionHelper.Connection;
                command.Transaction = _currentTransactionHelper.ActiveTransaction;
            }

            return _currentTransactionHelper.Connection;
        }

        /// <inheritdoc />
        public async Task<MySqlConnection> OpenConnectionAsync(MySqlCommand command)
        {
            if (_currentTransactionHelper == null || _currentTransactionHelper.IsDisposed)
            {
                MySqlConnection client = new MySqlConnection(ConnectionString);
                if (command != null) command.Connection = client;

                TaskCompletionSource<object> waitSource = new TaskCompletionSource<object>();

                void OnStateChange(object sender, StateChangeEventArgs args)
                {
                    switch (args.CurrentState)
                    {
                        // Wait until one of these state is set
                        case ConnectionState.Open:
                        case ConnectionState.Closed:
                        case ConnectionState.Broken:
                            waitSource.SetResult(null);
                            break;
                        // For these results below, we keep waiting.
                        case ConnectionState.Connecting:
                        case ConnectionState.Executing:
                        case ConnectionState.Fetching:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                client.StateChange += OnStateChange;
                await client.OpenAsync();
                await waitSource.Task;
                client.StateChange -= OnStateChange;
                return client;
            }

            if (command != null)
            {
                command.Connection = _currentTransactionHelper.Connection;
                command.Transaction = _currentTransactionHelper.ActiveTransaction;
            }

            return _currentTransactionHelper.Connection;
        }

        /// <inheritdoc />
        public MySqlTransactionHelper BeginTransaction()
        {
            MySqlConnection connection = OpenConnection(null);
            if (connection == null) return null;
            _currentTransactionHelper = new MySqlTransactionHelper(connection.BeginTransaction());
            return _currentTransactionHelper;
        }

        /// <inheritdoc />
        public async Task<MySqlTransactionHelper> BeginTransactionAsync()
        {
            MySqlConnection connection = await OpenConnectionAsync(null);
            if (connection == null) return null;
            _currentTransactionHelper = new MySqlTransactionHelper(await connection.BeginTransactionAsync());
            return _currentTransactionHelper;
        }

        /// <inheritdoc />
        public MySqlQueryResult ExecuteScalar(string commandText, out object data)
        {
            data = null;

            CreateCommand(commandText, CommandType.Text, null, out var command, out var result);

            try
            {
                if (Verbosity.HasFlag(SqlVerboseTypes.LogQueries)) Logger.Sql.LogInfo(command.CommandText);
                OpenConnection(command);
                data = command.ExecuteScalar();
                result.SetSucceed();
            }
            catch (Exception ex)
            {
                result.SetFail(command, ex);
            }
            finally
            {
                FinishExecute(result);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<MySqlResultSingle<object>> ExecuteScalarAsync(string commandText)
        {
            CreateCommandAsyncSingle<object>(commandText, CommandType.Text, null, out var command, out var result);

            try
            {
                if (Verbosity.HasFlag(SqlVerboseTypes.LogQueries)) Logger.Sql.LogInfo(command.CommandText);
                await OpenConnectionAsync(command);
                result.SetResult(await command.ExecuteScalarAsync());
                result.SetSucceed();
            }
            catch (Exception ex)
            {
                result.SetFail(command, ex);
            }
            finally
            {
                FinishExecute(result);
            }

            return result;
        }

        /// <inheritdoc />
        public MySqlQueryResult ExecuteNonQuery(string commandText, out int affectedRows)
        {
            affectedRows = 0;

            CreateCommand(commandText, CommandType.Text, null, out var command, out var result);

            try
            {
                if (Verbosity.HasFlag(SqlVerboseTypes.LogQueries)) Logger.Sql.LogInfo(command.CommandText);
                OpenConnection(command);
                affectedRows = command.ExecuteNonQuery();
                result.SetSucceed();
            }
            catch (Exception ex)
            {
                result.SetFail(command, ex);
            }
            finally
            {
                FinishExecute(result);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<MySqlResultSingle<int>> ExecuteNonQueryAsync(string commandText)
        {
            CreateCommandAsyncSingle<int>(commandText, CommandType.Text, null, out var command, out var result);

            try
            {
                if (Verbosity.HasFlag(SqlVerboseTypes.LogQueries)) Logger.Sql.LogInfo(command.CommandText);
                await OpenConnectionAsync(command);
                result.SetResult(await command.ExecuteNonQueryAsync());
                result.SetSucceed();
            }
            catch (Exception ex)
            {
                result.SetFail(command, ex);
            }
            finally
            {
                FinishExecute(result);
            }

            return result;
        }

        /// <inheritdoc />
        public MySqlQueryResult ExecuteReader<T>(string commandText, out List<T> data)
        {
            data = new List<T>();

            CreateCommand(commandText, CommandType.Text, null, out var command, out var result);

            try
            {
                if (Verbosity.HasFlag(SqlVerboseTypes.LogQueries)) Logger.Sql.LogInfo(command.CommandText);
                OpenConnection(command);
                using (DbDataReader reader = command.ExecuteReader())
                {
                    List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
                    while (reader.Read())
                    {
                        Dictionary<string, object> dict = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            dict.Add(reader.GetName(i), reader[i]);
                        }

                        list.Add(dict);
                    }

                    data = typeof(T) == typeof(Dictionary<string, object>)
                        ? list.Cast<T>().ToList()
                        : list.Select(MySqlHelper.ToObject<T>).ToList();

                    result.SetSucceed();
                }
            }
            catch (Exception ex)
            {
                result.SetFail(command, ex);
            }
            finally
            {
                FinishExecute(result);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<MySqlResultMultiple<T>> ExecuteReaderAsync<T>(string commandText)
        {
            CreateCommandAsyncMultiple<T>(commandText, CommandType.Text, null, out var command, out var result);

            try
            {
                if (Verbosity.HasFlag(SqlVerboseTypes.LogQueries)) Logger.Sql.LogInfo(command.CommandText);
                await OpenConnectionAsync(command);
                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
                    while (reader.Read())
                    {
                        Dictionary<string, object> dict = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            dict.Add(reader.GetName(i), reader[i]);
                        }

                        list.Add(dict);
                    }

                    result.SetResult(typeof(T) == typeof(Dictionary<string, object>)
                        ? list.Cast<T>().ToList()
                        : list.Select(MySqlHelper.ToObject<T>).ToList());

                    result.SetSucceed();
                }
            }
            catch (Exception ex)
            {
                result.SetFail(command, ex);
            }
            finally
            {
                FinishExecute(result);
            }

            return result;
        }

        /// <inheritdoc />
        public MySqlQueryResult ExecuteStoredProcedureScalar(string commandText, List<MySqlParameter> parameters, out object data)
        {
            data = null;

            CreateCommand(commandText, CommandType.StoredProcedure, parameters, out var command, out var result);

            try
            {
                if (Verbosity.HasFlag(SqlVerboseTypes.LogQueries)) Logger.Sql.LogInfo(command.CommandText);
                OpenConnection(command);
                data = command.ExecuteScalar();
                result.SetSucceed();
            }
            catch (Exception ex)
            {
                result.SetFail(command, ex);
            }
            finally
            {
                FinishExecute(result);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<MySqlResultSingle<object>> ExecuteStoredProcedureScalarAsync(string commandText, List<MySqlParameter> parameters)
        {
            CreateCommandAsyncSingle<object>(commandText, CommandType.StoredProcedure, parameters, out var command, out var result);

            try
            {
                if (Verbosity.HasFlag(SqlVerboseTypes.LogQueries)) Logger.Sql.LogInfo(command.CommandText);
                await OpenConnectionAsync(command);
                result.SetResult(await command.ExecuteScalarAsync());
                result.SetSucceed();
            }
            catch (Exception ex)
            {
                result.SetFail(command, ex);
            }
            finally
            {
                FinishExecute(result);
            }

            return result;
        }

        /// <inheritdoc />
        public MySqlQueryResult ExecuteStoredProcedureNonQuery(string commandText, List<MySqlParameter> parameters, out int affectedRows)
        {
            affectedRows = 0;

            CreateCommand(commandText, CommandType.StoredProcedure, parameters, out var command, out var result);

            try
            {
                if (Verbosity.HasFlag(SqlVerboseTypes.LogQueries)) Logger.Sql.LogInfo(command.CommandText);
                OpenConnection(command);
                affectedRows = command.ExecuteNonQuery();
                result.SetSucceed();
            }
            catch (Exception ex)
            {
                result.SetFail(command, ex);
            }
            finally
            {
                FinishExecute(result);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<MySqlResultSingle<int>> ExecuteStoredProcedureNonQueryAsync(string commandText, List<MySqlParameter> parameters)
        {
            CreateCommandAsyncSingle<int>(commandText, CommandType.StoredProcedure, parameters, out var command, out var result);

            try
            {
                if (Verbosity.HasFlag(SqlVerboseTypes.LogQueries)) Logger.Sql.LogInfo(command.CommandText);
                await OpenConnectionAsync(command);
                result.SetResult(await command.ExecuteNonQueryAsync());
                result.SetSucceed();
            }
            catch (Exception ex)
            {
                result.SetFail(command, ex);
            }
            finally
            {
                FinishExecute(result);
            }

            return result;
        }

        /// <inheritdoc />
        public MySqlQueryResult ExecuteStoredProcedureReader<T>(string commandText, List<MySqlParameter> parameters, out List<T> data)
        {
            data = new List<T>();

            CreateCommand(commandText, CommandType.StoredProcedure, parameters, out var command, out var result);

            try
            {
                if (Verbosity.HasFlag(SqlVerboseTypes.LogQueries)) Logger.Sql.LogInfo(command.CommandText);
                OpenConnection(command);
                using (DbDataReader reader = command.ExecuteReader())
                {
                    List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
                    while (reader.Read())
                    {
                        Dictionary<string, object> dict = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            dict.Add(reader.GetName(i), reader[i]);
                        }

                        list.Add(dict);
                    }

                    data = typeof(T) == typeof(Dictionary<string, object>)
                        ? list.Cast<T>().ToList()
                        : list.Select(MySqlHelper.ToObject<T>).ToList();
                    
                    result.SetSucceed();
                }
            }
            catch (Exception ex)
            {
                result.SetFail(command, ex);
            }
            finally
            {
                FinishExecute(result);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<MySqlResultMultiple<T>> ExecuteStoredProcedureReaderAsync<T>(string commandText, List<MySqlParameter> parameters)
        {
            CreateCommandAsyncMultiple<T>(commandText, CommandType.StoredProcedure, parameters, out var command, out var result);

            try
            {
                if (Verbosity.HasFlag(SqlVerboseTypes.LogQueries)) Logger.Sql.LogInfo(command.CommandText);
                await OpenConnectionAsync(command);
                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
                    while (reader.Read())
                    {
                        Dictionary<string, object> dict = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            dict.Add(reader.GetName(i), reader[i]);
                        }

                        list.Add(dict);
                    }

                    result.SetResult(typeof(T) == typeof(Dictionary<string, object>)
                        ? list.Cast<T>().ToList()
                        : list.Select(MySqlHelper.ToObject<T>).ToList());
                    
                    result.SetSucceed();
                }
            }
            catch (Exception ex)
            {
                result.SetFail(command, ex);
            }
            finally
            {
                FinishExecute(result);
            }

            return result;
        }

        public MySqlPrepare CreatePrepareStatement(string prepareQuery)
        {
            MySqlPrepare prepare = new MySqlPrepare(this, new MySqlCommand
                {
                    CommandTimeout = SqlQueryTimeout,
                    CommandType = CommandType.Text,
                    CommandText = prepareQuery
                }
            );

            try
            {
                MySqlConnection client = OpenConnection(prepare.PrepareCommand);
                if (client.State == ConnectionState.Open)
                {
                    prepare.Succeed = true;
                }
                else
                {
                    prepare.Succeed = false;
                    client.Close();
                }
            }
            catch (Exception e)
            {
                Logger.Sql.LogWarning("[CreatePrepareStatement] " + e.Message);
                prepare.Succeed = false;
            }

            return prepare;
        }

        public async Task<MySqlPrepare> CreatePrepareStatementAsync(string prepareQuery)
        {
            MySqlPrepare prepare = new MySqlPrepare(this, new MySqlCommand
                {
                    CommandTimeout = SqlQueryTimeout,
                    CommandType = CommandType.Text,
                    CommandText = prepareQuery
                }
            );

            try
            {
                MySqlConnection client = await OpenConnectionAsync(prepare.PrepareCommand);
                if (client.State == ConnectionState.Open)
                {
                    prepare.Succeed = true;
                }
                else
                {
                    prepare.Succeed = false;
                    client.Close();
                }
            }
            catch (Exception e)
            {
                Logger.Sql.LogWarning("[CreatePrepareStatementAsync] " + e.Message);
                prepare.Succeed = false;
            }

            return prepare;
        }

        /// <inheritdoc />
        public MySqlQueryResult InsertItem(IDatabaseObject item, out int affectedRows)
        {
            return InsertItem(item, null, out affectedRows);
        }

        /// <inheritdoc />
        public MySqlQueryResult InsertItem(IDatabaseObject item, Predicate<string> predicate, out int affectedRows)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var memberInfos = item.GetDatabaseMemberInfos();
            string tableName = item.GetTableName();

            affectedRows = 0;
            if (string.IsNullOrEmpty(tableName))
            {
                return Fail<Exception>($"The object '{item.GetType()}' has no tableName set.");
            }

            StringBuilder query = new StringBuilder($"INSERT INTO `{tableName}` SET ");
            foreach (MemberInfo member in memberInfos)
            {
                string key = member.GetKeyName();
                object value = ReflectionHelperExtension.GetValueFromMember(member, item);

                if (predicate != null && predicate(key)) continue;

                // Always ignore primary key if any
                if (item.GetPrimaryName() != null && key == item.GetPrimaryName())
                    continue;

                if (value is DateTime dateTime)
                {
                    query.Append($"`{key}`='{dateTime.ToDateTimeDatabase()}',");
                }
                else if (ReflectionHelperExtension.GetTypeFromMember(member) == typeof(string))
                {
                    string str = (string)value;
                    query.Append($"`{key}`={(str != null ? ("'" + Regex.Escape(str) + "'") : "NULL")},");
                }
                else if (ReflectionHelperExtension.GetTypeFromMember(member) == typeof(byte[]))
                {
                    byte[] data = (byte[])value;
                    query.Append($"`{key}`={(data != null ? ("'" + Convert.ToBase64String(data) + "'") : "NULL")},");
                }
                else
                {
                    query.Append($"`{key}`={(value.IsNumber() ? value : value != null ? ("'" + value + "'") : "NULL")},");
                }
            }
            if (query[query.Length - 1] == ',')
                query = query.Remove(query.Length - 1, 1);

            MySqlQueryResult queryResult = ExecuteNonQuery(query.ToString(), out affectedRows);
            if (queryResult.LastCommand != null) item.SetPrimaryValue(queryResult.LastCommand.LastInsertedId);
            return queryResult;
        }

        /// <inheritdoc />
        public async Task<MySqlResultSingle<int>> InsertItemAsync(IDatabaseObject item, Predicate<string> predicate = null)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var memberInfos = item.GetDatabaseMemberInfos();
            string tableName = item.GetTableName();

            if (string.IsNullOrEmpty(tableName))
            {
                return await FailSingleAsync<int, Exception>($"No tableName set for type '{tableName}'. Missing DBTableAttribute attribute."); ;
            }

            StringBuilder query = new StringBuilder($"INSERT INTO `{tableName}` SET ");
            foreach (MemberInfo member in memberInfos)
            {
                string key = member.GetKeyName();
                object value = ReflectionHelperExtension.GetValueFromMember(member, item);

                if (predicate != null && predicate(key)) continue;

                // Always ignore primary key if any
                if (item.GetPrimaryName() != null && key == item.GetPrimaryName())
                    continue;

                if (value is DateTime dateTime)
                {
                    query.Append($"`{key}`='{dateTime.ToDateTimeDatabase()}',");
                }
                else if (ReflectionHelperExtension.GetTypeFromMember(member) == typeof(string))
                {
                    string str = (string)value;
                    query.Append($"`{key}`={(str != null ? ("'" + Regex.Escape(str) + "'") : "NULL")},");
                }
                else if (ReflectionHelperExtension.GetTypeFromMember(member) == typeof(byte[]))
                {
                    byte[] data = (byte[])value;
                    query.Append($"`{key}`={(data != null ? ("'" + Convert.ToBase64String(data) + "'") : "NULL")},");
                }
                else
                {
                    query.Append($"`{key}`={(value.IsNumber() ? value : value != null ? ("'" + value + "'") : "NULL")},");
                }
            }

            if (query[query.Length - 1] == ',')
                query = query.Remove(query.Length - 1, 1);

            MySqlResultSingle<int> result = await ExecuteNonQueryAsync(query.ToString());
            if (result.LastCommand != null) item.SetPrimaryValue(result.LastCommand.LastInsertedId);
            return result;
        }

        /// <inheritdoc />
        public MySqlQueryResult InsertItemInclude(IDatabaseObject item, out int affectedRows, params string[] includeColumns)
        {
            return InsertItem(item, delegate (string s)
            {
                return !includeColumns.Any(column => string.Equals(column, s));
            }, out affectedRows);
        }

        /// <inheritdoc />
        public Task<MySqlResultSingle<int>> InsertItemIncludeAsync(IDatabaseObject item, params string[] includeColumns)
        {
            return InsertItemAsync(item, delegate (string s)
            {
                return !includeColumns.Any(column => string.Equals(column, s));
            });
        }

        /// <inheritdoc />
        public MySqlQueryResult InsertItemExclude(IDatabaseObject item, out int affectedRows, params string[] excludeColumns)
        {
            return InsertItem(item, delegate (string s)
            {
                return excludeColumns.Any(column => string.Equals(column, s));
            }, out affectedRows);
        }

        /// <inheritdoc />
        public Task<MySqlResultSingle<int>> InsertItemExcludeAsync(IDatabaseObject item, params string[] excludeColumns)
        {
            return InsertItemAsync(item, delegate (string s)
            {
                return excludeColumns.Any(column => string.Equals(column, s));
            });
        }

        /// <inheritdoc />
        public MySqlQueryResult UpdateItem(IDatabaseObject item, out int affectedRows)
        {
            return UpdateItem(item, null, out affectedRows);
        }

        /// <inheritdoc />
        public MySqlQueryResult UpdateItem(IDatabaseObject item, Predicate<string> predicate, out int affectedRows)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var memberInfos = item.GetDatabaseMemberInfos();
            string tableName = item.GetTableName();

            affectedRows = 0;
            if (string.IsNullOrEmpty(tableName))
            {
                return Fail<Exception>($"The object '{item.GetType()}' has no tableName set.");
            }

            StringBuilder query = new StringBuilder($"UPDATE `{tableName}` SET ");
            foreach (MemberInfo member in memberInfos)
            {
                string key = member.GetKeyName();
                object value = ReflectionHelperExtension.GetValueFromMember(member, item);

                if (predicate != null && predicate(key)) continue;

                if (value is DateTime dateTime)
                {
                    query.Append($"`{key}`='{dateTime.ToDateTimeDatabase()}',");
                }
                else if (ReflectionHelperExtension.GetTypeFromMember(member) == typeof(string))
                {
                    string str = (string)value;
                    query.Append($"`{key}`={(str != null ? ("'" + Regex.Escape(str) + "'") : "NULL")},");
                }
                else if (ReflectionHelperExtension.GetTypeFromMember(member) == typeof(byte[]))
                {
                    byte[] data = (byte[])value;
                    query.Append($"`{key}`={(data != null ? ("'" + Convert.ToBase64String(data) + "'") : "NULL")},");
                }
                else
                {
                    query.Append($"`{key}`={(value.IsNumber() ? value : value != null ? ("'" + value + "'") : "NULL")},");
                }
            }

            if (query[query.Length - 1] == ',')
                query = query.Remove(query.Length - 1, 1);

            var primaryKey = item.GetPrimaryName();
            var primaryValue = item.GetPrimaryValue();
            query.Append($" WHERE `{primaryKey}`={(primaryValue.IsNumber() ? primaryValue : primaryValue != null ? ("'" + primaryValue + "'") : "NULL")}");

            return ExecuteNonQuery(query.ToString(), out affectedRows);
        }

        /// <inheritdoc />
        public Task<MySqlResultSingle<int>> UpdateItemAsync(IDatabaseObject item, Predicate<string> predicate = null)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var memberInfos = item.GetDatabaseMemberInfos();
            string tableName = item.GetTableName();

            if (string.IsNullOrEmpty(tableName))
            {
                return FailSingleAsync<int, Exception>($"No tableName set for type '{tableName}'. Missing DBTableAttribute attribute."); ;
            }

            StringBuilder query = new StringBuilder($"UPDATE `{tableName}` SET ");
            foreach (MemberInfo member in memberInfos)
            {
                string key = member.GetKeyName();
                object value = ReflectionHelperExtension.GetValueFromMember(member, item);

                if (key == item.GetPrimaryName()) continue;
                if (predicate != null && predicate(key)) continue;

                if (value is DateTime dateTime)
                {
                    query.Append($"`{key}`='{dateTime.ToDateTimeDatabase()}',");
                }
                else if (ReflectionHelperExtension.GetTypeFromMember(member) == typeof(string))
                {
                    string str = (string)value;
                    query.Append($"`{key}`={(str != null ? ("'" + Regex.Escape(str) + "'") : "NULL")},");
                }
                else if (ReflectionHelperExtension.GetTypeFromMember(member) == typeof(byte[]))
                {
                    byte[] data = (byte[])value;
                    query.Append($"`{key}`={(data != null ? ("'" + Convert.ToBase64String(data) + "'") : "NULL")},");
                }
                else
                {
                    query.Append($"`{key}`={(value.IsNumber() ? value : value != null ? ("'" + value + "'") : "NULL")},");
                }
            }

            if (query[query.Length - 1] == ',')
                query = query.Remove(query.Length - 1, 1);

            var primaryKey = item.GetPrimaryName();
            var primaryValue = item.GetPrimaryValue();
            query.Append($" WHERE `{primaryKey}`={(primaryValue.IsNumber() ? primaryValue : primaryValue != null ? ("'" + primaryValue + "'") : "NULL")}");

            return ExecuteNonQueryAsync(query.ToString());
        }

        /// <inheritdoc />
        public MySqlQueryResult UpdateItemInclude(IDatabaseObject item, out int affectedRows, params string[] includeColumns)
        {
            return UpdateItem(item, delegate (string s)
            {
                return !includeColumns.Any(column => string.Equals(column, s));
            }, out affectedRows);
        }

        /// <inheritdoc />
        public Task<MySqlResultSingle<int>> UpdateItemIncludeAsync(IDatabaseObject item, params string[] includeColumns)
        {
            return UpdateItemAsync(item, delegate (string s)
            {
                return !includeColumns.Any(column => string.Equals(column, s));
            });
        }

        /// <inheritdoc />
        public MySqlQueryResult UpdateItemExclude(IDatabaseObject item, out int affectedRows, params string[] excludeColumns)
        {
            return UpdateItem(item, delegate (string s)
            {
                return excludeColumns.Any(column => string.Equals(column, s));
            }, out affectedRows);
        }

        /// <inheritdoc />
        public Task<MySqlResultSingle<int>> UpdateItemExcludeAsync(IDatabaseObject item, params string[] excludeColumns)
        {
            return UpdateItemAsync(item, delegate (string s)
            {
                return excludeColumns.Any(column => string.Equals(column, s));
            });
        }

        /// <inheritdoc />
        public MySqlQueryResult DeleteItem(IDatabaseObject item, out int affectedRows)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            affectedRows = 0;

            string tableName = item.GetTableName();
            if (string.IsNullOrEmpty(tableName))
            {
                return Fail<Exception>($"The object '{item.GetType()}' has no tableName set.");
            }

            string primaryKey = item.GetPrimaryName();
            if (string.IsNullOrEmpty(primaryKey))
            {
                return Fail<Exception>($"The object '{item.GetType()}' has no primaryKey set.");
            }

            StringBuilder query = new StringBuilder($"DELETE FROM `{tableName}` WHERE `{primaryKey}`={item.GetPrimaryValue()} ");

            return ExecuteNonQuery(query.ToString(), out affectedRows);
        }

        /// <inheritdoc />
        public Task<MySqlResultSingle<int>> DeleteItemAsync(IDatabaseObject item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            string tableName = item.GetTableName();
            if (string.IsNullOrEmpty(tableName))
            {
                return FailSingleAsync<int, Exception>($"The object '{item.GetType()}' has no tableName set."); ;
            }
            string primaryKey = item.GetPrimaryName();
            if (string.IsNullOrEmpty(primaryKey))
            {
                return FailSingleAsync<int, Exception>($"The object '{item.GetType()}' has no primaryKey set.");
            }

            StringBuilder query = new StringBuilder($"DELETE FROM `{tableName}` WHERE `{primaryKey}`={item.GetPrimaryValue()} ");

            return ExecuteNonQueryAsync(query.ToString());
        }
        
        private void CreateCommand(string commandText, CommandType commandType, List<MySqlParameter> parameters, out MySqlCommand command,
            out MySqlQueryResult result)
        {
            command = new MySqlCommand(commandText, _currentTransactionHelper?.Connection, _currentTransactionHelper?.ActiveTransaction)
            {
                CommandType = commandType,
                CommandTimeout = SqlQueryTimeout,
            };

            if (parameters != null)
                foreach (var parameter in parameters)
                    command.Parameters.Add(parameter);

            result = new MySqlQueryResult(this)
            {
                LastCommand = command
            };
        }

        /// <summary>
        /// Finish execute query.
        /// </summary>
        /// <param name="result"></param>
        private void FinishExecute(IMySqlResult result)
        {
            // If we don't have a transaction, we just close the connection
            if (_currentTransactionHelper == null || _currentTransactionHelper.IsDisposed)
            {
                result.LastCommand?.Connection?.Close();
            }
            else
            {
                // Keep the transaction active
                if (!result.Succeed)
                {
                    _currentTransactionHelper.HasError = true;
                }
            }

            // Debug errors
            if (!result.Succeed && Verbosity.HasFlag(SqlVerboseTypes.LogErrors))
            {
                if (result.LastCommand != null) Logger.Sql.LogError("Command: " + result.LastCommand.CommandText);
                if (result.LastError != null) Logger.Sql.LogException(result.LastError);
            }
        }

        private void CreateCommandAsyncSingle<T>(string commandText, CommandType commandType, List<MySqlParameter> parameters, out MySqlCommand command,
            out MySqlResultSingle<T> result)
        {
            command = new MySqlCommand(commandText, _currentTransactionHelper?.Connection, _currentTransactionHelper?.ActiveTransaction)
            {
                CommandType = commandType,
                CommandTimeout = SqlQueryTimeout
            };

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                    command.Parameters.Add(parameter);
            }

            result = new MySqlResultSingle<T>(this)
            {
                LastCommand = command,
            };
        }

        private void CreateCommandAsyncMultiple<T>(string commandText, CommandType commandType, List<MySqlParameter> parameters, out MySqlCommand command,
            out MySqlResultMultiple<T> result)
        {
            command = new MySqlCommand(commandText, _currentTransactionHelper?.Connection, _currentTransactionHelper?.ActiveTransaction)
            {
                CommandType = commandType,
                CommandTimeout = SqlQueryTimeout
            };

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                    command.Parameters.Add(parameter);
            }

            result = new MySqlResultMultiple<T>(this)
            {
                LastCommand = command,
            };
        }

        private MySqlQueryResult Fail<TException>(string reason) where TException : Exception, new()
        {
            MySqlQueryResult fail = new MySqlQueryResult(this);
            fail.SetFail(new MySqlCommand(), Activator.CreateInstance(typeof(TException), reason) as Exception);
            return fail;
        }

        private Task<MySqlResultSingle<T>> FailSingleAsync<T, TException>(string reason) where TException : Exception, new()
        {
            TaskCompletionSource<MySqlResultSingle<T>> taskSource = new TaskCompletionSource<MySqlResultSingle<T>>();
            MySqlResultSingle<T> fail = new MySqlResultSingle<T>(this);
            fail.SetFail(new MySqlCommand(), Activator.CreateInstance(typeof(TException), reason) as Exception);
            taskSource.SetResult(fail);
            return taskSource.Task;
        }
    }
}
#endif