using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif
#if ODIN_INSPECTOR && !ODIN_INSPECTOR_EDITOR_ONLY
using Sirenix.Serialization;
#endif

namespace TrickCore
{
    /// <summary>
    /// Blazing fast deep clone implementation optimized for Unity game objects
    /// Simple, direct approach that outperforms complex reflection-based systems
    /// </summary>
    [Preserve]
    public static class TrickReflectionExtensions
    {
        // Keep the original UnitySpecificDeepClone for backward compatibility
        public static Predicate<Type> DeepCloneFunc;
        public static Func<object, object> UnitySpecificDeepClone;

        // Simple member cache - only cache what we actually need
        private static readonly Dictionary<Type, MemberInfo[]> _memberCache = new();
        
        // Unity-specific type handlers for maximum performance
        private static readonly Dictionary<Type, Func<object, object>> _unityHandlers = new()
        {
            [typeof(AnimationCurve)] = obj => {
                var curve = (AnimationCurve)obj;
                var newCurve = new AnimationCurve(curve.keys);
                newCurve.preWrapMode = curve.preWrapMode;
                newCurve.postWrapMode = curve.postWrapMode;
                return newCurve;
            },
            [typeof(Gradient)] = obj => {
                var gradient = (Gradient)obj;
                var newGradient = new Gradient();
                newGradient.alphaKeys = gradient.alphaKeys;
                newGradient.colorKeys = gradient.colorKeys;
                newGradient.mode = gradient.mode;
                return newGradient;
            },
#if UNITY_ADDRESSABLES
            [typeof(AssetReference)] = obj => obj // Shallow copy for asset references
#endif
        };

        /// <summary>
        /// Creates a deep copy of the object with maximum performance
        /// </summary>
        [Preserve]
        public static T TrickDeepClone<T>(this T original, HashSet<object> visited = null)
        {
            if (original == null) return default(T);
            
            visited ??= new HashSet<object>();
            var result = DeepCloneInternal(original, visited);
            return (T)result;
        }

        /// <summary>
        /// Internal clone method optimized for speed and correctness
        /// </summary>
        [Preserve]
        private static object DeepCloneInternal(object original, HashSet<object> visited)
        {
            if (original == null) return null;

            var type = original.GetType();

            // Fast path for common immutable types
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
                return original;

            // Fast path for Unity Objects (components, assets, etc.)
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return original;

            // Fast path for value types without reference fields
            if (type.IsValueType)
            {
                // Most Unity structs (Vector3, Color, etc.) can be copied directly
                if (IsSimpleValueType(type))
                    return original;
                
                // For complex structs, we need to check for reference fields
                return CloneValueType(original, type, visited);
            }

            // Circular reference check for reference types only
            if (!visited.Add(original))
                return original;

            try
            {
                // Check for Unity-specific override first
                if (UnitySpecificDeepClone != null)
                {
                    var customClone = UnitySpecificDeepClone(original);
                    if (customClone != null)
                        return customClone;
                }

                // Handle Unity-specific types
                if (_unityHandlers.TryGetValue(type, out var handler))
                    return handler(original);

                // Handle arrays
                if (type.IsArray)
                    return CloneArray((Array)original, type, visited);

                // Handle collections
                if (typeof(IDictionary).IsAssignableFrom(type))
                    return CloneDictionary((IDictionary)original, type, visited);

                if (typeof(IList).IsAssignableFrom(type))
                    return CloneList((IList)original, type, visited);

                // Handle regular objects
                return CloneObject(original, type, visited);
            }
            finally
            {
                visited.Remove(original);
            }
        }

        /// <summary>
        /// Fast check for simple value types that don't need deep cloning
        /// </summary>
        [Preserve]
        private static bool IsSimpleValueType(Type type)
        {
            // Common Unity value types that are safe to copy directly
            return type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
                   type == typeof(Quaternion) || type == typeof(Color) || type == typeof(Color32) ||
                   type == typeof(Rect) || type == typeof(RectInt) || type == typeof(Bounds) ||
                   type == typeof(Matrix4x4) || type == typeof(LayerMask) || type == typeof(AnimationCurve) ||
                   type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid) ||
                   (type.IsPrimitive && type != typeof(IntPtr) && type != typeof(UIntPtr));
        }

        /// <summary>
        /// Clone value types that might contain reference fields
        /// </summary>
        [Preserve]
        private static object CloneValueType(object original, Type type, HashSet<object> visited)
        {
            var cloned = Activator.CreateInstance(type);
            var members = GetCachedMembers(type);

            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i];
                if (member is FieldInfo field)
                {
                    var value = field.GetValue(original);
                    if (value != null && !field.FieldType.IsValueType && field.FieldType != typeof(string))
                    {
                        value = DeepCloneInternal(value, visited);
                    }
                    field.SetValue(cloned, value);
                }
                else if (member is PropertyInfo property)
                {
                    var value = property.GetValue(original);
                    if (value != null && !property.PropertyType.IsValueType && property.PropertyType != typeof(string))
                    {
                        value = DeepCloneInternal(value, visited);
                    }
                    property.SetValue(cloned, value);
                }
            }

            return cloned;
        }

        /// <summary>
        /// Fast array cloning with type-specific optimizations
        /// </summary>
        [Preserve]
        private static object CloneArray(Array original, Type type, HashSet<object> visited)
        {
            var elementType = type.GetElementType();
            var cloned = Array.CreateInstance(elementType, original.Length);

            // Fast path for primitive and simple types
            if (elementType.IsPrimitive || elementType == typeof(string) || IsSimpleValueType(elementType))
            {
                Array.Copy(original, cloned, original.Length);
                return cloned;
            }

            // Deep clone each element
            for (int i = 0; i < original.Length; i++)
            {
                var element = original.GetValue(i);
                cloned.SetValue(DeepCloneInternal(element, visited), i);
            }

            return cloned;
        }

        /// <summary>
        /// Fast list cloning with capacity pre-allocation
        /// </summary>
        [Preserve]
        private static object CloneList(IList original, Type type, HashSet<object> visited)
        {
            var cloned = (IList)Activator.CreateInstance(type);
            
            // Pre-allocate capacity if possible
            if (cloned is ICollection collection && original.Count > 0)
            {
                // Try to set capacity via reflection for better performance
                var capacityProperty = type.GetProperty("Capacity");
                if (capacityProperty?.CanWrite == true)
                {
                    try { capacityProperty.SetValue(cloned, original.Count); }
                    catch { /* Ignore capacity setting failures */ }
                }
            }

            // Determine if elements need deep cloning
            Type elementType = null;
            if (type.IsGenericType)
            {
                var genericArgs = type.GetGenericArguments();
                if (genericArgs.Length == 1)
                    elementType = genericArgs[0];
            }

            bool needsDeepClone = elementType != null && 
                                 !elementType.IsPrimitive && 
                                 elementType != typeof(string) && 
                                 !IsSimpleValueType(elementType);

            // Clone elements
            for (int i = 0; i < original.Count; i++)
            {
                var item = original[i];
                cloned.Add(needsDeepClone ? DeepCloneInternal(item, visited) : item);
            }

            return cloned;
        }

        /// <summary>
        /// Fast dictionary cloning with capacity pre-allocation
        /// </summary>
        [Preserve]
        private static object CloneDictionary(IDictionary original, Type type, HashSet<object> visited)
        {
            var cloned = (IDictionary)Activator.CreateInstance(type);

            // Determine if keys/values need deep cloning
            bool needsKeyCloning = false;
            bool needsValueCloning = false;

            if (type.IsGenericType)
            {
                var genericArgs = type.GetGenericArguments();
                if (genericArgs.Length == 2)
                {
                    var keyType = genericArgs[0];
                    var valueType = genericArgs[1];
                    
                    needsKeyCloning = !keyType.IsPrimitive && keyType != typeof(string) && !IsSimpleValueType(keyType);
                    needsValueCloning = !valueType.IsPrimitive && valueType != typeof(string) && !IsSimpleValueType(valueType);
                }
            }

            // Clone entries
            foreach (DictionaryEntry entry in original)
            {
                var key = needsKeyCloning ? DeepCloneInternal(entry.Key, visited) : entry.Key;
                var value = needsValueCloning ? DeepCloneInternal(entry.Value, visited) : entry.Value;
                cloned.Add(key, value);
            }

            return cloned;
        }

        /// <summary>
        /// Fast object cloning focusing on serializable members
        /// </summary>
        [Preserve]
        private static object CloneObject(object original, Type type, HashSet<object> visited)
        {
            object cloned;
            try
            {
                cloned = Activator.CreateInstance(type);
            }
            catch
            {
                // If we can't create an instance, return the original
                return original;
            }

            var members = GetCachedMembers(type);

            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i];
                try
                {
                    if (member is FieldInfo field)
                    {
                        var value = field.GetValue(original);
                        if (value != null)
                        {
                            // Only deep clone reference types (excluding strings)
                            if (!field.FieldType.IsValueType && field.FieldType != typeof(string))
                            {
                                value = DeepCloneInternal(value, visited);
                            }
                        }
                        field.SetValue(cloned, value);
                    }
                    else if (member is PropertyInfo property)
                    {
                        var value = property.GetValue(original);
                        if (value != null)
                        {
                            // Only deep clone reference types (excluding strings)
                            if (!property.PropertyType.IsValueType && property.PropertyType != typeof(string))
                            {
                                value = DeepCloneInternal(value, visited);
                            }
                        }
                        property.SetValue(cloned, value);
                    }
                }
                catch
                {
                    // Skip members that can't be cloned
                    continue;
                }
            }

            return cloned;
        }

        /// <summary>
        /// Get cached serializable members for a type
        /// </summary>
        [Preserve]
        private static MemberInfo[] GetCachedMembers(Type type)
        {
            if (_memberCache.TryGetValue(type, out var cached))
                return cached;

            var members = new List<MemberInfo>();
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            // Get all fields
            var fields = type.GetFields(bindingFlags);
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                
                // Skip compiler-generated fields
                if (field.Name.Contains("<") || field.Name.Contains("k__BackingField"))
                    continue;

                // Include public fields or fields with SerializeField attribute
                if (field.IsPublic || Attribute.IsDefined(field, typeof(SerializeField)))
                {
                    if (!field.IsNotSerialized)
                        members.Add(field);
                }
            }

            // Get all properties
            var properties = type.GetProperties(bindingFlags);
            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                
                // Must have both getter and setter
                if (!property.CanRead || !property.CanWrite)
                    continue;

                // Skip indexers
                if (property.GetIndexParameters().Length > 0)
                    continue;

                // Include public properties or properties with SerializeField on backing field
                if (property.GetMethod?.IsPublic == true && property.SetMethod?.IsPublic == true)
                {
                    members.Add(property);
                }
                else
                {
                    // Check for auto-property with SerializeField
                    var backingField = type.GetField($"<{property.Name}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (backingField != null && Attribute.IsDefined(backingField, typeof(SerializeField)))
                    {
                        members.Add(property);
                    }
                }
            }

            var result = members.ToArray();
            _memberCache[type] = result;
            return result;
        }

        /// <summary>
        /// Clear all caches - useful for hot reloading scenarios
        /// </summary>
        [Preserve]
        public static void TrickForgetAllCache()
        {
            _memberCache.Clear();
        }

        public static void TrickForgetCache(this Type type)
        {
            if (_memberCache.ContainsKey(type)) 
                _memberCache.Remove(type);
        }

        public static MemberInfo[] TrickGetMemberInfoFromType(this Type type, bool includeProperty, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            // Use the new cached members implementation for consistency
            if (flags == (BindingFlags.Instance | BindingFlags.Public) && includeProperty)
            {
                return GetCachedMembers(type);
            }

            // Fallback to original implementation for custom flags
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

                    return res && (attris.Length == 0 
#if ODIN_INSPECTOR && !ODIN_INSPECTOR_EDITOR_ONLY
                                   || attris.Any(o2=> o2 is OdinSerializeAttribute)
#endif
                                   || attris.All(o => (!(o is NonSerializedAttribute)) && !(o is JsonIgnoreAttribute)));;
                }).ToArray();
        }

    }
}