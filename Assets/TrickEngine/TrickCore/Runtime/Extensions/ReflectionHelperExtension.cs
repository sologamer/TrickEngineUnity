using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

#if ODIN_INSPECTOR && !ODIN_INSPECTOR_EDITOR_ONLY
    using Sirenix.OdinInspector;
#endif

namespace TrickCore
{
    public static class ReflectionHelperExtension
    {
        public static object ResolveInstanceFromMemberInfo(this object instance, MemberInfo targetMemberInfo, out bool result)
        {
            if (instance == null)
            {
                result = false;
                return null;
            }

            var list = instance.GetMemberInfosFromInstance(true);
            var found = list.FirstOrDefault(pair => pair.Key == targetMemberInfo);
            if (found.Key != null)
            {
                result = true;
                return found.Value;
            }

            foreach (KeyValuePair<MemberInfo, object> pair in list)
            {
                var newInstance = ResolveInstanceFromMemberInfo(pair.Key.GetValueFromMember(pair.Value), targetMemberInfo, out result);
                if (result) return newInstance;
            }

            result = false;
            return null;
        }

        /// <summary>
        /// Gets the name of the json property, otherwise the name of the memberinfo
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static string GetJsonPropertyName(this MemberInfo member)
        {
            JsonPropertyAttribute jsonProperty = member.GetAttribute<JsonPropertyAttribute>();
            return jsonProperty != null ? jsonProperty.PropertyName : member.Name;
        }

        /// <summary>
        /// Gets the memberinfos from an instance
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="includeProperty"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static KeyValuePair<MemberInfo, object>[] GetMemberInfosFromInstance(this object instance, bool includeProperty, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            return instance == null ? Array.Empty<KeyValuePair<MemberInfo, object>>() : GetMemberInfosFromType(instance.GetType(), instance, includeProperty, flags);
        }
    
        /// <summary>
        /// Gets the memberinfos from an instance
        /// </summary>
        /// <param name="type"></param>
        /// <param name="instance"></param>
        /// <param name="includeProperty"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static KeyValuePair<MemberInfo, object>[] GetMemberInfosFromType(this Type type, object instance, bool includeProperty, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            List<KeyValuePair<MemberInfo, object>> list = type.GetMembers(flags)
                .Where(info => info.MemberType == MemberTypes.Field || includeProperty && info.MemberType == MemberTypes.Property).Where(info =>
                {
                    var attris = info.GetCustomAttributes(true);
                    if (attris.Any(o => o is ExcludeEditorMemberAttribute)) return false;
                    if (attris.Any(o => o is IncludeEditorMemberAttribute)) return true;
                    bool res = true;

                    if (info.MemberType == MemberTypes.Property)
                    {
                        PropertyInfo prop = (PropertyInfo)info;
                        res = includeProperty && prop.GetGetMethod() != null;
                    }

                    return res && IsAttributeValid(attris);
                }).Select((info, i) => new KeyValuePair<MemberInfo, object>(info, instance)).ToList();


            for (int i = 0; i < list.Count; i++)
            {
                KeyValuePair<MemberInfo, object> pair = list[i];

                var attris = pair.Key.GetCustomAttributes(true);
                if (attris.Any(o => o is IncludeEditorMemberAttribute))
                {
                    var filter = (IncludeEditorMemberAttribute)attris.First(o => o is IncludeEditorMemberAttribute);

                    switch (filter.IncludeType)
                    {
                        case IncludeObjectType.MemberSelf:
                        {
                            // Do nothing
                        }
                            break;
                        case IncludeObjectType.MemberContent:
                        {
                            list.RemoveAt(i);
                            switch (filter.SortingType)
                            {
                                case SortType.Insert:
                                    list.InsertRange(i, GetMemberInfosFromType(pair.Key.GetTypeFromMember(), instance != null ? pair.Key.GetValueFromMember(pair.Value) : null, filter.IncludeProperty));
                                    break;
                                case SortType.Add:
                                    list.AddRange(GetMemberInfosFromType(pair.Key.GetTypeFromMember(), instance != null ? pair.Key.GetValueFromMember(pair.Value) : null, filter.IncludeProperty));
                                    break;
                            }
                            i--;
                        }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return list.ToArray();
        }

        private static bool IsAttributeValid(object[] attributes)
        {
            return attributes.Length == 0 || 
#if !NO_UNITY
                   attributes.Any(o => o is MinMaxRangeAttribute || o.GetType().Name == "OdinSerializeAttribute") ||
#endif
                   attributes.All(o => o is not NonSerializedAttribute && o is not JsonIgnoreAttribute);
        }

        /// <summary>
        /// Gets the memberinfo from a type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="includeProperty"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static MemberInfo[] GetMemberInfosByTypeOnly(this Type type, bool includeProperty, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
        {
            List<MemberInfo> list = type.GetMembers(bindingFlags)
                .Where(info => info.MemberType == MemberTypes.Field ||
                               info.MemberType == MemberTypes.Property).Where(info =>
                {
                    var attris = info.GetCustomAttributes(true);
                    if (attris.Any(o => o is ExcludeEditorMemberAttribute)) return false;
                    if (attris.Any(o => o is IncludeEditorMemberAttribute)) return true;

                    bool res = true;

                    if (info.MemberType == MemberTypes.Property)
                    {
                        PropertyInfo prop = (PropertyInfo) info;
                        res = includeProperty && prop.GetGetMethod() != null;
                    }

                    return res && IsAttributeValid(attris);
                }).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                MemberInfo member = list[i];

                var attris = member.GetCustomAttributes(true);
                if (attris.Any(o => o is IncludeEditorMemberAttribute))
                {
                    var filter = (IncludeEditorMemberAttribute) attris.First(o => o is IncludeEditorMemberAttribute);

                    switch (filter.IncludeType)
                    {
                        case IncludeObjectType.MemberSelf:
                        {
                            // Do nothing
                        }
                            break;
                        case IncludeObjectType.MemberContent:
                        {
                            list.RemoveAt(i);
                            switch (filter.SortingType)
                            {
                                case SortType.Insert:
                                    list.InsertRange(i, GetMemberInfosByTypeOnly(member.GetTypeFromMember(), filter.IncludeProperty));
                                    break;
                                case SortType.Add:
                                    list.AddRange(GetMemberInfosByTypeOnly(member.GetTypeFromMember(), filter.IncludeProperty));
                                    break;
                            }
                            i--;
                        }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return list.ToArray();
        }

        public static T GetAttribute<T>(this MemberInfo memberInfo) where T : Attribute
        {
            return memberInfo != null ? memberInfo.GetCustomAttributes(false).OfType<T>().FirstOrDefault() : default(T);
        }

        static List<Type> GetBaseTypes(object instance)
        {
            List<Type> list = new List<Type>();
            Type t = instance.GetType();
            while (t != null)
            {
                list.Add(t);
                t = t.BaseType;
                if (t == typeof(object)) break;
            }
            return list;
        }
        public static MemberInfo[] GetMemberInfos(this object instance, bool includeProperty, bool includeBaseType, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            if (instance == null)
                return new MemberInfo[0];

            if (!includeBaseType) return GetMemberInfoFromType(instance.GetType(), includeProperty, flags);
            
            List<MemberInfo> infos = new List<MemberInfo>();
            List<Type> types = GetBaseTypes(instance);
            foreach (var type in types)
            {
                infos.AddRange(GetMemberInfoFromType(type, includeProperty, flags));
            }
            return infos.GroupBy(info => info.Name).Select(group => group.Last()).ToArray();
        }

        public static MemberInfo[] GetMemberInfoFromType(this Type type, bool includeProperty, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            return type.GetMembers(flags)
                .Where(info => info.MemberType == MemberTypes.Field ||
                               info.MemberType == MemberTypes.Property).Where(info =>
                {
                    var attris = info.GetCustomAttributes(false);
                    bool res = true;

                    if (info.MemberType == MemberTypes.Property)
                    {
                        PropertyInfo prop = (PropertyInfo)info;
                        res = includeProperty && prop.GetGetMethod() != null && prop.GetSetMethod() != null;
                    }

                    return res && IsAttributeValid(attris);
                }).ToArray();
        }

        /// <summary>
        /// Gets all derived types, using the the types of the assembly
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetDerivedTypes(this Type type)
        {
            return Assembly.GetAssembly(type).GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && !myType.IsInterface && myType.IsSubclassOf(type));
        }
        
        /// <summary>
        /// Gets the type of a member
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static Type GetTypeFromMember(this MemberInfo member)
        {
            if (member == null) return null;

            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
            }

            return null;
        }

        /// <summary>
        /// Gets a value from a member info, support FieldInfo and PropertyInfo
        /// </summary>
        /// <param name="member"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static object GetValueFromMember(this MemberInfo member, object instance)
        {
            if (member == null) return null;

            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).GetValue(instance);
                case MemberTypes.Property:
                    if (((PropertyInfo) member) is {CanRead: true} prop)
                    {
                        return prop.GetValue(instance, null);
                    }
                    break;
            }

            return null;
        }
        
        /// <summary>
        /// Sets a value to a MemberInfo
        /// </summary>
        /// <param name="member"></param>
        /// <param name="instance"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool SetValueToMember(this MemberInfo member, object instance, object value)
        {
            if (member == null) return false;

            switch (member.MemberType)
            {
                case MemberTypes.Field:
                {
                        FieldInfo field = (FieldInfo)member;
                        //Type nullableType = Nullable.GetUnderlyingType(field.FieldType);
                        //Type fieldType = nullableType != null ? nullableType : field.FieldType;
                        Type fieldType = field.FieldType;

                        if (fieldType == typeof(short)) field.SetValue(instance, Convert.ToInt16(value));
                        else if (fieldType == typeof(byte)) field.SetValue(instance, Convert.ToByte(value));
                        else if (fieldType == typeof(sbyte)) field.SetValue(instance, Convert.ToSByte(value));
                        else if (fieldType == typeof(int)) field.SetValue(instance, Convert.ToInt32(value));
                        else if (fieldType == typeof(long)) field.SetValue(instance, Convert.ToInt64(value));
                        else if (fieldType == typeof(ushort)) field.SetValue(instance, Convert.ToUInt16(value));
                        else if (fieldType == typeof(uint)) field.SetValue(instance, Convert.ToUInt32(value));
                        else if (fieldType == typeof(ulong)) field.SetValue(instance, Convert.ToUInt64(value));
                        else if (fieldType == typeof(float)) field.SetValue(instance, Convert.ToSingle(value));
                        else if (fieldType == typeof(short?)) field.SetValue(instance, value == DBNull.Value || value == null ? (short?)null : Convert.ToInt16(value));
                        else if (fieldType == typeof(byte?)) field.SetValue(instance, value == DBNull.Value || value == null ? (byte?)null : Convert.ToByte(value));
                        else if (fieldType == typeof(sbyte?)) field.SetValue(instance, value == DBNull.Value || value == null ? (sbyte?)null : Convert.ToSByte(value));
                        else if (fieldType == typeof(int?)) field.SetValue(instance, value == DBNull.Value || value == null ? (int?)null : Convert.ToInt32(value));
                        else if (fieldType == typeof(long?)) field.SetValue(instance, value == DBNull.Value || value == null ? (long?)null : Convert.ToInt64(value));
                        else if (fieldType == typeof(ushort?)) field.SetValue(instance, value == DBNull.Value || value == null ? (ushort?)null : Convert.ToUInt16(value));
                        else if (fieldType == typeof(uint?)) field.SetValue(instance, value == DBNull.Value || value == null ? (uint?)null : Convert.ToUInt32(value));
                        else if (fieldType == typeof(ulong?)) field.SetValue(instance, value == DBNull.Value || value == null ? (ulong?)null : Convert.ToUInt64(value));
                        else if (fieldType == typeof(float?)) field.SetValue(instance, value == DBNull.Value || value == null ? (float?)null : Convert.ToSingle(value));
                        else if (fieldType == typeof(DBNull)) field.SetValue(instance, null);
                        else if (fieldType.IsEnum)
                        {
                            object enumValue = value != null ? Enum.Parse(fieldType, $"{value}", true) : null;
                            field.SetValue(instance, enumValue);
                        }
                        else field.SetValue(instance, value == DBNull.Value ? null : value);
                        return true;
                    }
                case MemberTypes.Property:
                {
                        PropertyInfo property = (PropertyInfo)member;
                        if (!property.CanWrite) return false;
                        //Type nullableType = Nullable.GetUnderlyingType(property.PropertyType);
                        //Type propertyType = nullableType != null ? nullableType : property.PropertyType;
                        Type propertyType = property.PropertyType;

                        if (propertyType == typeof(short)) property.SetValue(instance, Convert.ToInt16(value), null);
                        else if (propertyType == typeof(byte)) property.SetValue(instance, Convert.ToByte(value), null);
                        else if (propertyType == typeof(sbyte)) property.SetValue(instance, Convert.ToSByte(value), null);
                        else if (propertyType == typeof(int)) property.SetValue(instance, Convert.ToInt32(value), null);
                        else if (propertyType == typeof(long)) property.SetValue(instance, Convert.ToInt64(value), null);
                        else if (propertyType == typeof(ushort)) property.SetValue(instance, Convert.ToUInt16(value), null);
                        else if (propertyType == typeof(uint)) property.SetValue(instance, Convert.ToUInt32(value), null);
                        else if (propertyType == typeof(ulong)) property.SetValue(instance, Convert.ToUInt64(value), null);
                        else if (propertyType == typeof(float)) property.SetValue(instance, Convert.ToSingle(value), null);
                        else if (propertyType == typeof(short?)) property.SetValue(instance, value == DBNull.Value || value == null ? (short?)null : Convert.ToInt16(value), null);
                        else if (propertyType == typeof(byte?)) property.SetValue(instance, value == DBNull.Value || value == null ? (byte?)null : Convert.ToByte(value), null);
                        else if (propertyType == typeof(sbyte?)) property.SetValue(instance, value == DBNull.Value || value == null ? (sbyte?)null : Convert.ToSByte(value), null);
                        else if (propertyType == typeof(int?)) property.SetValue(instance, value == DBNull.Value || value == null ? (int?)null : Convert.ToInt32(value), null);
                        else if (propertyType == typeof(long?)) property.SetValue(instance, value == DBNull.Value || value == null ? (long?)null : Convert.ToInt64(value), null);
                        else if (propertyType == typeof(ushort?)) property.SetValue(instance, value == DBNull.Value || value == null ? (ushort?)null :  Convert.ToUInt16(value), null);
                        else if (propertyType == typeof(uint?)) property.SetValue(instance, value == DBNull.Value || value == null ? (uint?)null : Convert.ToUInt32(value), null);
                        else if (propertyType == typeof(ulong?)) property.SetValue(instance, value == DBNull.Value || value == null ? (ulong?)null : Convert.ToUInt64(value), null);
                        else if (propertyType == typeof(float?)) property.SetValue(instance, value == DBNull.Value || value == null ? (float?)null : Convert.ToSingle(value));
                        else if (propertyType == typeof(DBNull)) property.SetValue(instance, null, null);
                        else if (propertyType.IsEnum)
                        {
                            object enumValue = value != null ? Enum.Parse(propertyType, $"{value}", true) : null;
                            property.SetValue(instance, enumValue, null);
                        }
                        else property.SetValue(instance, value == DBNull.Value ? null : value, null);
                        return true;
                    }
            }

            return false;
        }

        public static MemberInfo GetMemberByTag(this object instance, string tag, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            if (instance == null) return null;
            return GetMemberByTag(instance.GetType(), tag, flags);
        }

        public static MemberInfo GetMemberByTag(this Type type, string tag, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            if (type == null) return null;

            return type.GetMemberInfoFromType(true).FirstOrDefault(info =>
            {
                var attribute = info.GetAttribute<TagAttribute>();
                return attribute != null && string.Equals(tag, attribute.Tag, StringComparison.InvariantCultureIgnoreCase);
            });
        }

        public static object GetValueFromMemberTag(this object instance, string tag, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            if (instance == null) return null;
            var member = GetMemberByTag(instance, tag, flags);
            return member != null ? member.GetValueFromMember(instance) : null;
        }

        public static bool SetValueFromMemberTag(this object instance, object value, string tag, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            if (instance == null) return false;
            var member = GetMemberByTag(instance, tag, flags);
            return member != null && member.SetValueToMember(instance, value);
        }
    }
}