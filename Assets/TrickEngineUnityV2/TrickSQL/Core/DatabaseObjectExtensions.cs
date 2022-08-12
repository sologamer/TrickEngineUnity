using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Dapper.Contrib.Extensions;
using Newtonsoft.Json;
using TrickCore;

namespace TrickCore
{
    public static class DatabaseObjectExtensions
    {
        public static T GetCachedJsonData<T>(this IDatabaseObject instance, ref ICacheObject cachedObject, string originalData) where T : new()
        {
            if (cachedObject == null) cachedObject = new CachedJsonData<T>();
            return cachedObject is CachedJsonData<T> cache ? cache.Get(originalData) : default(T);
        }

        public static bool SetCachedJsonData<T>(this IDatabaseObject instance, ref ICacheObject cachedObject, ref string originalData, object value) where T : new()
        {
            if (cachedObject == null) cachedObject = new CachedJsonData<T>();
            if (cachedObject is CachedJsonData<T> cache)
            {
                cache.Set(ref originalData, value != null ? value.SerializeToJson(false, false) : "{}");
                return true;
            }
            return false;
        }

        public static T ConvertDatabaseObject<T>(this IDatabaseObject from) where T : IDatabaseObject
        {
            return @from.SerializeToJson(false, true).DeserializeJson<T>();
        }

        public static T CopyDatabaseObject<T>(this T from) where T : IDatabaseObject
        {
            return @from.SerializeToJson(false, true).DeserializeJson<T>();
        }

        public static string GetPrimaryName(this Type type)
        {
            var primaryKey = GetPrimaryFieldInfo(type);
            if (primaryKey == null) return null;
            JsonPropertyAttribute jsonProperty = primaryKey.GetAttribute<JsonPropertyAttribute>();
            return jsonProperty != null ? jsonProperty.PropertyName : primaryKey.Name;
        }

        public static string GetPrimaryName(this IDatabaseObject obj)
        {
            return GetPrimaryName(obj.GetType());
        }

        /// <summary>
        /// Gets the value of the primary key
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="dbValue">If true, the output is ready for query</param>
        /// <returns></returns>
        public static object GetPrimaryValue(this IDatabaseObject obj, bool dbValue = false)
        {
            var primaryKey = GetPrimaryFieldInfo(obj.GetType());
            object value = primaryKey == null ? null : ReflectionHelperExtension.GetValueFromMember(primaryKey, obj);

            if (dbValue)
            {
                StringBuilder query = new StringBuilder();
                if (value is DateTime dateTime)
                {
                    query.Append($"'{dateTime:yyyy-MM-dd HH:mm:ss}'");
                }
                else if (ReflectionHelperExtension.GetTypeFromMember(primaryKey) == typeof(string))
                {
                    string str = (string)value;
                    query.Append($"{(str != null ? ("'" + Regex.Escape(str) + "'") : "NULL")}");
                }
                else
                {
                    query.Append($"{(IsNumber(value) ? value : value != null ? ("'" + value + "'") : "NULL")}");
                }
                return query.ToString();
            }
            return value;
        }

        public static MemberInfo GetPrimaryFieldInfo(this Type type)
        {
            if (type == null) return null;
            MemberInfo primaryKey = type.GetMemberInfoFromType(true).FirstOrDefault(info => info.GetCustomAttributes(false).Any(o => o is KeyAttribute || o is PrimaryKeyAttribute));
            return primaryKey;
        }

        public static void SetPrimaryValue(this IDatabaseObject obj, object value)
        {
            var primaryKey = GetPrimaryFieldInfo(obj.GetType());
            if (primaryKey != null) primaryKey.SetValueToMember(obj, value);
        }

        public static string GetTableName(this IDatabaseObject obj)
        {
            return GetTableName(obj.GetType());
        }

        public static string GetTableName(this Type type)
        {
            var tableAttribute = type.GetAttribute<TableAttribute>();
            return tableAttribute?.Name;
        }

        public static string GetKeyName(this MemberInfo member)
        {
            JsonPropertyAttribute jsonProperty = member.GetAttribute<JsonPropertyAttribute>();
            return jsonProperty != null ? jsonProperty.PropertyName : member.Name;
        }

        public static List<MemberInfo> GetDatabaseMemberInfos(this IDatabaseObject obj)
        {
            return obj.GetMemberInfos(true, true).ToList();
            //.Where(member =>
            //    {
            //        string memberTableName = member.DeclaringType.GetTableName();
            //        return memberTableName != null && memberTableName == obj.GetTableName();
            //    }
        }


        /// <summary>
        /// Check if an object is a number type
        /// </summary>
        /// <param name="value">The object</param>
        /// <returns>Returns true if its a number</returns>
        public static bool IsNumber(this object value)
        {
            return value is sbyte
                   || value is byte
                   || value is short
                   || value is ushort
                   || value is int
                   || value is uint
                   || value is long
                   || value is ulong
                   || value is float
                   || value is double
                   || value is decimal;
        }
        
    }
}