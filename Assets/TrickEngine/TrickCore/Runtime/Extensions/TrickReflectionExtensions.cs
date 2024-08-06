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
        public static Func<object, object> UnitySpecificDeepClone;

        private static readonly Dictionary<Type, MemberInfo[]> MemberInfoTypeCache = new Dictionary<Type, MemberInfo[]>();
        private static readonly object LockObject = new object();


        public static T TrickDeepClone<T>(this T original, HashSet<object> visited = null)
        {
            try
            {
                if (original == null) return default(T);
                var type = original.GetType();

                // Skip cloning for Unity objects, strings, or value types (including Nullable<>)
                if (typeof(Object).IsAssignableFrom(type) || type == typeof(string) || type.IsValueType) return original;
                
                if (TryCloneUnitySpecific(original, out var unitySpecificClone)) return unitySpecificClone;

                visited ??= new HashSet<object>();
                if (!visited.Add(original)) return original; // Prevent infinite recursion

                // Handling for arrays
                if (type.IsArray)
                {
                    var elementType = type.GetElementType();
                    var array = original as Array;
                    var clonedArray = Array.CreateInstance(elementType, array.Length);
                    for (int i = 0; i < array.Length; i++)
                    {
                        clonedArray.SetValue(TrickDeepClone(array.GetValue(i), visited), i);
                    }

                    return (T)(object)clonedArray; // Cast back to generic type
                }

                // Handling for IList and IDictionary
                if (typeof(IDictionary).IsAssignableFrom(type))
                {
                    var dictionary = (IDictionary)Activator.CreateInstance(type);
                    foreach (DictionaryEntry entry in (IDictionary)original)
                    {
                        dictionary.Add(TrickDeepClone(entry.Key, visited), TrickDeepClone(entry.Value, visited));
                    }

                    return (T)dictionary;
                }

                if (typeof(IList).IsAssignableFrom(type))
                {
                    var list = (IList)Activator.CreateInstance(type);
                    foreach (var item in (IList)original)
                    {
                        list.Add(TrickDeepClone(item, visited));
                    }

                    return (T)list;
                }

                // Handle complex objects
                var clone = Activator.CreateInstance(type);
                foreach (var memberInfo in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (memberInfo is FieldInfo field)
                    {
                        var fieldValue = field.GetValue(original);
                        field.SetValue(clone, TrickDeepClone(fieldValue, visited));
                    }
                    else if (memberInfo is PropertyInfo property && property.CanRead && property.CanWrite)
                    {
                        var propertyValue = property.GetValue(original);
                        property.SetValue(clone, TrickDeepClone(propertyValue, visited));
                    }
                }

                return (T)clone;
            }
            catch (Exception e)
            {
                // Handle or log the exception as needed
                Debug.LogError($"Failed to deep clone object of type {typeof(T).FullName}");
                Debug.LogException(e);
                return default;
            }
        }


        private static bool TryCloneUnitySpecific<T>(T original, out T clonedObject)
        {
            if (original is AnimationCurve originalCurve)
            {
                var clonedCurve = new AnimationCurve();
                clonedCurve.keys = originalCurve.keys; // This copies all the curve keys
                clonedCurve.preWrapMode = originalCurve.preWrapMode;
                clonedCurve.postWrapMode = originalCurve.postWrapMode;

                clonedObject = (T)(object)clonedCurve; // Cast back to T
                return true; // Indicate that cloning was handled
            }
            
            if (original is Gradient originalGradient)
            {
                var clonedGradient = new Gradient();
                clonedGradient.alphaKeys = originalGradient.alphaKeys;
                clonedGradient.colorKeys = originalGradient.colorKeys;

                clonedObject = (T)(object)clonedGradient; // Cast back to T
                return true; // Indicate that cloning was handled
            }

#if UNITY_ADDRESSABLES
            if (original is AssetReference)
            {
                clonedObject = original;
                return true;
            }
#endif
            
            if (UnitySpecificDeepClone != null)
            {
                clonedObject = (T)UnitySpecificDeepClone(original);
                return clonedObject != null;
            }

            // Add similar blocks for other Unity-specific types that require special handling

            clonedObject = default;
            return false; // No specific cloning was done
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