#if UNITY_EDITOR || USE_TRICK_MYSQL
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using TrickCore;

namespace TrickCore
{
    public abstract class MySqlPrepareObject : IDisposable
    {
        public IMySqlObject Sql { get; }

        private bool Disposed { get; set; }
        public bool Succeed { get; set; }
        public MySqlCommand PrepareCommand { get; set; }
        
        public MySqlPrepareObject(IMySqlObject sql, MySqlCommand prepareCommand)
        {
            Sql = sql;
            PrepareCommand = prepareCommand;
            PrepareCommand.Disposed += OnDisposed;
        }

        private void OnDisposed(object sender, EventArgs e)
        {
            PrepareCommand.Disposed -= OnDisposed;
            PrepareCommand = null;
        }
        
        public MySqlQueryResult ExecuteScalar(IEnumerable<MySqlParameter> parameters, out object data)
        {
            MySqlQueryResult result = new MySqlQueryResult(Sql);
            try
            {
                SetParameters(parameters);
                data = PrepareCommand.ExecuteScalar();
                result.SetSucceed();
            }
            catch (Exception e)
            {
                result.SetFail(PrepareCommand, e);
                data = null;
            }
            return result;
        }

        public async Task<MySqlResultSingle<object>> ExecuteScalarAsync(IEnumerable<MySqlParameter> parameters)
        {
            MySqlResultSingle<object> result = new MySqlResultSingle<object>(Sql);

            try
            {
                SetParameters(parameters);
                result.SetResult(await PrepareCommand.ExecuteScalarAsync());
                result.SetSucceed();
            }
            catch (Exception e)
            {
                result.SetFail(PrepareCommand, e);
            }

            return result;
        }

        public MySqlQueryResult ExecuteNonQuery(IEnumerable<MySqlParameter> parameters, out int affectedRows)
        {
            MySqlQueryResult result = new MySqlQueryResult(Sql);
            try
            {
                SetParameters(parameters);
                affectedRows = PrepareCommand.ExecuteNonQuery();
                result.SetSucceed();
            }
            catch (Exception e)
            {
                result.SetFail(PrepareCommand, e);
                affectedRows = 0;
            }
            return result;
        }


        public async Task<MySqlResultSingle<int>> ExecuteNonQueryAsync(IEnumerable<MySqlParameter> parameters)
        {
            MySqlResultSingle<int> result = new MySqlResultSingle<int>(Sql);
            try
            {
                SetParameters(parameters);
                result.SetResult(await PrepareCommand.ExecuteNonQueryAsync());
                result.SetSucceed();
            }
            catch (Exception e)
            {
                result.SetFail(PrepareCommand, e);
            }
            return result;
        }

        public MySqlQueryResult ExecuteReader<T>(List<MySqlParameter> parameters, out List<T> data)
        {
            MySqlQueryResult result = new MySqlQueryResult(Sql);

            try
            {
                InternalExecuteReader(parameters, out var objData);
                data = objData.Select(MySqlHelper.ToObject<T>).ToList();
                result.SetSucceed();
            }
            catch (Exception e)
            {
                result.SetFail(PrepareCommand, e);
                data = new List<T>();
            }

            return result;
        }

        private void InternalExecuteReader(IEnumerable<MySqlParameter> parameters, out List<Dictionary<string, object>> data)
        {
            SetParameters(parameters);
            data = new List<Dictionary<string, object>>();
            using (MySqlDataReader reader = PrepareCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    Dictionary<string, object> dict = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        dict.Add(reader.GetName(i), reader[i]);
                    }
                    data.Add(dict);
                }
            }
        }

        public async Task<MySqlResultMultiple<T>> ExecuteReaderAsync<T>(List<MySqlParameter> parameters)
        {
            MySqlResultMultiple<T> result = new MySqlResultMultiple<T>(Sql);

            try
            {
                List<Dictionary<string, object>> objData = await InternalExecuteReaderAsync(parameters);
                result.SetResult(objData.Select(MySqlHelper.ToObject<T>).ToList());
                result.SetSucceed();
            }
            catch (Exception e)
            {
                result.SetFail(PrepareCommand, e);
            }

            return result;
        }

        private async Task<List<Dictionary<string, object>>> InternalExecuteReaderAsync(IEnumerable<MySqlParameter> parameters)
        {
            SetParameters(parameters);
            using (DbDataReader reader = await PrepareCommand.ExecuteReaderAsync())
            {
                List<Dictionary<string, object>> data = new List<Dictionary<string, object>>();
                while (reader.Read())
                {
                    Dictionary<string, object> dict = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        dict.Add(reader.GetName(i), reader[i]);
                    }
                    data.Add(dict);
                }

                return data;
            }
        }

        private void SetParameters(IEnumerable<MySqlParameter> parameters)
        {
            PrepareCommand.Parameters.Clear();
            if (parameters == null) return;
            foreach (MySqlParameter parameter in parameters)
            {
                if (!PrepareCommand.Parameters.Contains(parameter.ParameterName))
                    PrepareCommand.Parameters.Add(parameter);
            }
        }

        public virtual void Close()
        {
            if (Disposed) return;
            Disposed = true;
            Succeed = false;

            if (PrepareCommand == null) return;

            try
            {
                PrepareCommand.Connection?.Close();
                PrepareCommand.Dispose();
            }
            finally
            {
                PrepareCommand = null;
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
    public class MySqlPrepare : MySqlPrepareObject
    {
        public MySqlPrepare(IMySqlObject sql, MySqlCommand prepareCommand) : base(sql, prepareCommand)
        {

        }
    }
}
#endif