using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

#if CODESTAGE
using CodeStage.AntiCheat.ObscuredTypes;
#endif

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif
#if ODIN_INSPECTOR && !ODIN_INSPECTOR_EDITOR_ONLY
using Sirenix.Serialization;
#endif

namespace TrickCore
{
    public static class TrickReflectionExtensions
    {
        public static Predicate<Type> DeepCloneFunc;
        private static readonly Dictionary<Type, MemberInfo[]> MemberInfoTypeCache = new Dictionary<Type, MemberInfo[]>();
        private static readonly object LockObject = new object();

        /// <summary>
        /// Fully deepclones an object using Activator.CreateInstance and Reflection
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="visitSet"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T TrickDeepClone<T>(this T obj, HashSet<object> visitSet = null)
        {
            if (obj == null) return default;
            visitSet ??= new HashSet<object>();
            if (!visitSet.Contains(obj)) visitSet.Add(obj);

            T2 CloneIfNotContained<T2>(T2 subObj)
            {
                // already exists, don't clone again. otherwise deepclone it, this is to avoid circular references
                return visitSet.Contains(subObj) ? subObj : TrickDeepClone(subObj, visitSet);
            }
            
            var type = obj is Type objType ? objType : obj.GetType();

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return obj;
#if CODESTAGE
            if (typeof(IObscuredType).IsAssignableFrom(type))
                return obj;
#endif
            if (typeof(string).IsAssignableFrom(type))
                return obj;
            // If the type comes from Unity, return it (no deep-copy allowed)
            if (typeof(Object).IsAssignableFrom(type) ||
                typeof(AnimationCurve).IsAssignableFrom(type) ||
                (
#if UNITY_ADDRESSABLES
                    !typeof(AssetReference).IsAssignableFrom(type) &&
#endif
                 type.FullName != null &&
                 type.FullName.StartsWith("UnityEngine")))
            {
                return obj;
            }
            
            try
            {
                object instance;
#if UNITY_ADDRESSABLES
                if (typeof(AssetReference).IsAssignableFrom(type))
                {
                    if (obj is AssetReference ar)
                    {
                        instance = Activator.CreateInstance(type, new object[]
                        {
                            ar.AssetGUID
                        });
                    }
                    else
                    {
                        instance = Activator.CreateInstance(type);
                    }
                }
                else
#endif
                {
                    if (typeof(Type).IsAssignableFrom(type))
                        return default;
                    
                    instance = Activator.CreateInstance(type);
                }

                if (typeof(IDictionary).IsAssignableFrom(type))
                {
                    if (type.IsGenericType && obj is IDictionary dict &&
                        Activator.CreateInstance(type) is IDictionary newDict)
                    {
                        var ks = dict.Keys.Cast<object>().ToList();
                        var vs = dict.Values.Cast<object>().ToList();
                        if(ks.Count == vs.Count)
                            for (int i = 0; i < ks.Count; i++)
                            {
                                var k = ks[i];
                                var v = vs[i];

                                newDict.Add(TrickDeepClone(k, visitSet), TrickDeepClone(v, visitSet));
                            }
                        return (T) newDict;
                    }
                }
                else if (typeof(IList).IsAssignableFrom(type))
                {
                    if (type.IsGenericType && obj is IList list && Activator.CreateInstance(type) is IList newList)
                    {
                        foreach (var o in list.Cast<object>().ToList()) newList.Add(TrickDeepClone(o, visitSet));
                        return (T) newList;
                    }
                }

                foreach (var info in type.TrickGetMemberInfoFromType(false,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    Type t = info.GetTypeFromMember();
                    if (t == null) continue;
                    if (typeof(Object).IsAssignableFrom(type) ||
                        typeof(AnimationCurve).IsAssignableFrom(type) ||
                        (type.FullName != null && type.FullName.StartsWith("UnityEngine")))
                    {
                        info.SetValueToMember(instance, info.GetValueFromMember(obj));
                    }
                    else if (typeof(IDictionary).IsAssignableFrom(t))
                    {
                        var value = info.GetValueFromMember(obj);
                        if (t.IsGenericType && value is IDictionary dict &&
                            Activator.CreateInstance(t) is IDictionary newDict)
                        {
                            var ks = dict.Keys.Cast<object>().ToList();
                            var vs = dict.Values.Cast<object>().ToList();
                            if (ks.Count == vs.Count)
                                for (int i = 0; i < ks.Count; i++)
                                {
                                    var k = ks[i];
                                    var v = vs[i];

                                    newDict.Add(TrickDeepClone(k, visitSet), TrickDeepClone(v, visitSet));
                                }

                            info.SetValueToMember(instance, newDict);
                        }
                        else
                        {
                            info.SetValueToMember(instance, value);
                        }
                    }
                    else if (typeof(IList).IsAssignableFrom(t))
                    {
                        var value = info.GetValueFromMember(obj);
                        if (t.IsGenericType && value is IList list && Activator.CreateInstance(t) is IList newList)
                        {
                            foreach (var o in list.Cast<object>().ToList()) newList.Add(TrickDeepClone(o, visitSet));
                            info.SetValueToMember(instance, newList);
                        }
                        else
                        {
                            info.SetValueToMember(instance, value);
                        }
                    }
                    else
                    {
                        if (t.IsValueType)
                        {
                            info.SetValueToMember(instance, info.GetValueFromMember(obj));
                        }
                        else
                        {
                            if (t.IsClass)
                            {
                                info.SetValueToMember(instance, CloneIfNotContained(info.GetValueFromMember(obj)));
                            }
                            else
                            {
                                info.SetValueToMember(instance, info.GetValueFromMember(obj));
                            }
                        }
                    }
                }

                return (T) instance;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return default;
            }
        }

        public static void TrickForgetCache(this Type type)
        {
            lock (LockObject)
            {
                if (MemberInfoTypeCache.ContainsKey(type)) MemberInfoTypeCache.Remove(type);
            }
        }

        public static void TrickForgetAllCache()
        {
            lock (LockObject)
            {
                MemberInfoTypeCache.Clear();
            }
        }

        public static MemberInfo[] TrickGetMemberInfoFromType(this Type type, bool includeProperty, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            lock (LockObject)
            {
                if (!MemberInfoTypeCache.TryGetValue(type, out var cache))
                {
                    MemberInfoTypeCache[type] = cache = type.GetMembers(flags)
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

                            return res && (attris.Length == 0 
#if ODIN_INSPECTOR && !ODIN_INSPECTOR_EDITOR_ONLY
                                           || attris.Any(o2=> o2 is OdinSerializeAttribute)
#endif
                                           || attris.All(o => (!(o is NonSerializedAttribute)) && !(o is JsonIgnoreAttribute)));;
                        }).ToArray();
                }
                return cache;
            }
        }

    }
}