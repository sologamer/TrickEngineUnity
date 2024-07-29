using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TrickCore
{
    public static class MonoSingletonExtensions
    {
        private static readonly Dictionary<Type, object> Cache = new Dictionary<Type, object>();

        public static List<string> FindPrefabDirectories()
        {
            var directories = new List<string>();

            // Get all first-level directories under "Assets"
            string[] topLevelDirectories = Directory.GetDirectories("Assets", "*", SearchOption.TopDirectoryOnly);

            foreach (var dir in topLevelDirectories)
            {
                // Look for "Prefabs" directories inside each top-level directory
                string prefabsPath = Path.Combine(dir, "Prefabs");
                if (Directory.Exists(prefabsPath)) directories.Add(prefabsPath);
            }

            return directories;
        }

        public static T FindManagerEditor<T>() where T : MonoSingleton<T>
        {
#if UNITY_EDITOR
            if (Cache.TryGetValue(typeof(T), out var value)) return (T)value;

            // Search for the asset in the found directories
            var findAssets = FindPrefabDirectories().SelectMany(dir => AssetDatabase.FindAssets($"{typeof(T).Name} t:prefab", new[] { dir })).ToList();
            if (findAssets.Count == 0) return null;
            var path = AssetDatabase.GUIDToAssetPath(findAssets[0]);
            var prefab = AssetDatabase.LoadAssetAtPath<T>(path);
            Cache.Add(typeof(T), prefab);
            return prefab;
#else
            return null;
#endif
        }

        public static T FindPrefabEditor<T>(string name) where T : MonoBehaviour
        {
#if UNITY_EDITOR
            if (Cache.TryGetValue(typeof(T), out var value)) return (T)value;

            string findName = string.IsNullOrEmpty(name) ? typeof(T).Name : name;
            var findAssets = FindPrefabDirectories().SelectMany(dir => AssetDatabase.FindAssets($"{findName} t:prefab", new[] { dir })).ToList();
            if (findAssets.Count == 0) return null;

            var path = AssetDatabase.GUIDToAssetPath(findAssets[0]);
            var prefab = AssetDatabase.LoadAssetAtPath<T>(path);
            Cache.Add(typeof(T), prefab);
            return prefab;
#else
            return null;
#endif
        }

        public static void ClearSingletonCache<T>()
        {
            Cache.Remove(typeof(T));
        }

        private static readonly Dictionary<object, object> CachedIds = new Dictionary<object, object>();

#if ODIN_INSPECTOR
        public static ValueDropdownList<T> EditorGetValueDropDownList<T>(object type, Func<ValueDropdownList<T>> func)
        {
            if (CachedIds.TryGetValue(type, out var list)) return (ValueDropdownList<T>)list;
            CachedIds.Add(type, list = func?.Invoke());
            return (ValueDropdownList<T>)list;
        }
#endif

        public static void EditorValueDropDownClearCache(object type)
        {
            if (CachedIds.ContainsKey(type))
                CachedIds.Remove(type);
        }

        public static void EditorValueDropDownClearCacheAll()
        {
            CachedIds.Clear();
        }


        public static bool ValidateEntry<T>(List<T> checkIdList, Type idType = null)
        {
            if (checkIdList == null) return false;

            checkIdList = checkIdList.Distinct().ToList();

            var uniqueMember = typeof(T).GetMemberInfoFromType(true, BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(info => info.GetCustomAttribute<PrimaryKeyAttribute>() != null);

            if (uniqueMember == null)
            {
                Debug.LogError($"[{typeof(T).Name}] Member is null, missing PrimaryKeyAttribute attribute!");
                return false;
            }

            if (uniqueMember.GetTypeFromMember() == typeof(string))
                return false;

            var entriesToFix = checkIdList
                .GroupBy(data => (int)uniqueMember.GetValueFromMember(data))
                .Where(g => g.Key == 0 || g.Count() > 1)
                .SelectMany(g => g.Key == 0 ? g.ToList() : g.Skip(1).ToList()).ToList();

            foreach (T entry in entriesToFix)
            {
                int newId = (checkIdList.Count > 0 ? checkIdList.Max(data => (int)uniqueMember.GetValueFromMember(data)) : 0) + 1;
                uniqueMember.SetValueToMember(entry, newId);
            }

            if (entriesToFix.Count > 0 && idType != null) EditorValueDropDownClearCache(idType);

            return true;
        }

        public static void AddEntryUnique<T, T2>(ref List<T> list, List<T> checkIdList = null)
            where T : new()
            where T2 : struct
        {
            checkIdList ??= list;
            var uniqueMember = typeof(T).GetMemberInfoFromType(true, BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(info => info.GetCustomAttribute<PrimaryKeyAttribute>() != null);

            if (uniqueMember == null)
            {
                Debug.LogError("Member is null, missing DBPrimary attribute!");
                return;
            }

            var newEntry = new T();
            list ??= new List<T>();
            if (uniqueMember.GetTypeFromMember() == typeof(int))
            {
                int newId = (checkIdList.Count > 0 ? checkIdList.Max(data => (int)uniqueMember.GetValueFromMember(data)) : 0) + 1;
                uniqueMember.SetValueToMember(newEntry, newId);
            }

            list.Add(newEntry);
            EditorValueDropDownClearCache(typeof(T2));
        }

        public static void AddEntryUnique<T>(ref List<T> list, List<T> checkIdList = null)
            where T : new()
        {
            checkIdList ??= list;
            var uniqueMember = typeof(T).GetMemberInfoFromType(true, BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(info => info.GetCustomAttribute<PrimaryKeyAttribute>() != null);

            if (uniqueMember == null)
            {
                Debug.LogError("Member is null, missing DBPrimary attribute!");
                return;
            }

            var newEntry = new T();
            list ??= new List<T>();
            if (uniqueMember.GetTypeFromMember() == typeof(int))
            {
                int newId = (checkIdList.Count > 0 ? checkIdList.Max(data => (int)uniqueMember.GetValueFromMember(data)) : 0) + 1;
                uniqueMember.SetValueToMember(newEntry, newId);
            }

            list.Add(newEntry);
        }
    }
}