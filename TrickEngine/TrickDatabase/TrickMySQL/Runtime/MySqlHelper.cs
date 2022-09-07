#if UNITY_EDITOR || USE_TRICK_MYSQL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MySql.Data.MySqlClient;

namespace TrickCore
{
    public static class MySqlHelper
    {
        public static MySqlObject GetInstance(string host, string user, string pass, string dbName, SqlVerboseTypes verboseType)
        {
            return new MySqlObject(host, user, pass, dbName, verboseType);
        }
        
        public static MySqlObject GetInstance(string connectionString, SqlVerboseTypes verboseType)
        {
            return new MySqlObject(connectionString, verboseType);
        }

        public static DateTime GetServerTime(this IMySqlObject sql)
        {
            return sql.ExecuteScalar("SELECT NOW()", out var serverTimeStamp).Succeed ? (DateTime)serverTimeStamp : new DateTime();
        }

        public static TimeSpan GetServerTimeDifference(this IMySqlObject sql)
        {
            return ((sql.ExecuteScalar("SELECT NOW()", out var serverTimeStamp).Succeed ? (DateTime)serverTimeStamp : new DateTime()) - DateTime.Now);
        }

        public static bool IsConnected(this IMySqlObject sql, out string error)
        {
            error = null;

            var res = sql.ExecuteScalar("SELECT 1", out var data);
            if (res.Succeed && Convert.ToInt32(data) == 1)
                return true;

            if (res.LastError != null)
                error = res.LastError.Message;

            return false;
        }

        /// <summary>
        /// Converts a dictionary to object <typeparamref name="T"/> using reflection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static T ToObject<T>(IDictionary<string, object> dict)
        {
            if (dict == null) return default(T);

            T obj = Activator.CreateInstance<T>();
            MemberInfo[] members = obj.GetMemberInfos(true, true);
            foreach (KeyValuePair<string, object> pair in dict)
            {
                MemberInfo member = members.FirstOrDefault(info => info.GetKeyName() == pair.Key);
                if (member != null) ReflectionHelperExtension.SetValueToMember(member, obj, pair.Value);
            }
            return obj;
        }
        
        public static string GetPreparedStatement<T>(StatementType statementType, params string[] columns) where T : IDatabaseObject
        {
            var type = typeof(T);
            var memberInfos = type.GetMemberInfoFromType(true).Where(member =>
            {
                string memberTableName = member.DeclaringType.GetTableName();
                return memberTableName != null && memberTableName == type.GetTableName();
            }).ToList();

            string tableName = type.GetTableName();

            if (string.IsNullOrEmpty(tableName))
            {
                Logger.Network.LogError($"The object '{type}' has no tableName set.");
                return null;
            }

            StringBuilder query = new StringBuilder();

            switch (statementType)
            {
                case StatementType.Select:
                    string selector = columns != null && columns.Length > 0 ? string.Join(",", columns) : "*";
                    query = new StringBuilder($"SELECT {selector} FROM `{tableName}`");
                    break;
                case StatementType.Insert:
                    query = new StringBuilder($"INSERT INTO `{tableName}` SET ");
                    break;
                case StatementType.Update:
                    query = new StringBuilder($"UPDATE `{tableName}` SET ");
                    break;
                case StatementType.Delete:
                    query = new StringBuilder($"DELETE FROM `{tableName}`");
                    break;
            }

            switch (statementType)
            {
                case StatementType.Insert:
                case StatementType.Update:
                    foreach (MemberInfo member in memberInfos)
                    {
                        string key = member.GetKeyName();
                        string memberName = $"@{key}".ToLower();

                        // Always ignore primary key if any
                        if (type.GetPrimaryName() != null && key == type.GetPrimaryName())
                            continue;

                        query.Append($"`{key}`={memberName},");
                    }

                    if (query[query.Length - 1] == ',')
                        query = query.Remove(query.Length - 1, 1);

                    break;
            }
            
            switch (statementType)
            {
                case StatementType.Select:
                case StatementType.Update:
                case StatementType.Delete:
                {
                    query.Append($" WHERE `{type.GetPrimaryName()}`=@{type.GetPrimaryName().ToLower()}");
                }
                    break;
            }

            return query.ToString();
        }

        public static string ToDateTimeDatabase(this DateTime dateTime)
        {
            return $"{dateTime:yyyy-MM-dd HH:mm:ss}";
        }

        public static IEnumerable<MySqlParameter> GetDefaultParametersFromType<T>()
        {
            return typeof(T).GetDefaultParametersFromType();
        }

        public static IEnumerable<MySqlParameter> GetDefaultParametersFromType(this Type type)
        {
            var memberInfos = type.GetMemberInfoFromType(true).Where(member =>
            {
                string memberTableName = member.DeclaringType.GetTableName();
                return memberTableName != null && memberTableName == type.GetTableName();
            }).ToList();

            string tableName = type.GetTableName();

            if (string.IsNullOrEmpty(tableName))
            {
                Logger.Network.LogError($"The object '{type}' has no tableName set.");
                return new List<MySqlParameter>();
            }

            List<MySqlParameter> parameters = new List<MySqlParameter>();

            foreach (MemberInfo member in memberInfos)
            {
                string key = member.GetKeyName();

                // Always ignore primary key if any
                if (type.GetPrimaryName() != null && key == type.GetPrimaryName())
                    continue;

                Type memberType = ReflectionHelperExtension.GetTypeFromMember(member);
                if (memberType == null) continue;
                // TODO: fix
                if (memberType == typeof(bool)) parameters.Add(new MySqlParameter(key, MySqlDbType.Bit));
                else if (memberType == typeof(short)) parameters.Add(new MySqlParameter(key, MySqlDbType.Int16));
                else if (memberType == typeof(int)) parameters.Add(new MySqlParameter(key, MySqlDbType.Int32));
                else if (memberType == typeof(long)) parameters.Add(new MySqlParameter(key, MySqlDbType.Int64));
                else if (memberType == typeof(ushort)) parameters.Add(new MySqlParameter(key, MySqlDbType.UInt16));
                else if (memberType == typeof(uint)) parameters.Add(new MySqlParameter(key, MySqlDbType.UInt32));
                else if (memberType == typeof(ulong)) parameters.Add(new MySqlParameter(key, MySqlDbType.UInt64));
                else if (memberType == typeof(double)) parameters.Add(new MySqlParameter(key, MySqlDbType.Double));
                else if (memberType == typeof(decimal)) parameters.Add(new MySqlParameter(key, MySqlDbType.Decimal));
                else if (memberType == typeof(string)) parameters.Add(new MySqlParameter(key, MySqlDbType.String));
                else if (memberType == typeof(DateTime)) parameters.Add(new MySqlParameter(key, MySqlDbType.DateTime));
            }
            
            return parameters;
        }
    }
}
#endif