using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

#if ODIN_INSPECTOR && !ODIN_INSPECTOR_EDITOR_ONLY
using Sirenix.OdinInspector;
#endif

namespace TrickCore
{
    /// <summary>
    /// Unity (MonoBehavior / ScriptableObject) variant of a DropTable with editor
    /// </summary>
    [Preserve, JsonObject, Serializable]
    public class UnityDropTable<T> : BaseUnityDropTable
    {
        [Serializable]
        public struct Entry
        {
            public T Object;
            public float Weight;

            public bool Equals(Entry other)
            {
                return EqualityComparer<T>.Default.Equals(Object, other.Object) && Weight.Equals(other.Weight);
            }

            public override bool Equals(object obj)
            {
                return obj is Entry other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (EqualityComparer<T>.Default.GetHashCode(Object) * 397) ^ Weight.GetHashCode();
                }
            }

            public static bool operator ==(Entry left, Entry right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Entry left, Entry right)
            {
                return !left.Equals(right);
            }

            public override string ToString()
            {
                return $"[{Weight:F2}] {Object}";
            }
        }
        
        [Serializable]
        public class ReorderableArrayEntry : ReorderableArray<Entry>
        {
            
        }

        /// <summary>
        /// The drop table entries
        /// </summary>
#if ODIN_INSPECTOR && !ODIN_INSPECTOR_EDITOR_ONLY
        [ListDrawerSettings(OnBeginListElementGUI = "BeginList"), ValidateInput("OnValidate")]
        //public List<Entry> Items = new List<Entry>();
        public ReorderableArrayEntry Items = new ReorderableArrayEntry();

        [HorizontalGroup("DropTableButtonsHorizontal")]
        [Button(Name = "OrderBy ASC")]
        public void OrderByAscending()
        {
            EditorOrderByNormalizedWeight(true);
        }
        
        [HorizontalGroup("DropTableButtonsHorizontal")]
        [Button(Name = "OrderBy DESC")]
        public void OrderByDescending()
        {
            EditorOrderByNormalizedWeight(false);
        }
        
        [HorizontalGroup("DropTableButtonsHorizontal")]
        [Button(Name = "Test Generate")]
        public void TestGenerate()
        {
            Debug.Log("[Test Generate]: " + RandomItem());
        }
#else
        [Reorderable()] public ReorderableArrayEntry Items = new ReorderableArrayEntry();
#endif	
        
#if ODIN_INSPECTOR && UNITY_EDITOR && !ODIN_INSPECTOR_EDITOR_ONLY
        private bool OnValidate(ReorderableArrayEntry list)
        {
            IsDirty = true;
            return true;
        }
        
        private void BeginList(int index)
        {
            GUIStyle style = new GUIStyle();
            style.richText = true;

            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.black;
            GUILayout.BeginVertical("box");

            var pair = Items[index];
            List<string> previewTexts = new List<string>();
            previewTexts.Add($"{EditorGetElementHeader(pair)}");
            if (previewTexts.Count > 0) GUILayout.Label($"<color=white>{string.Join("\n", previewTexts)}</color>", style);
            GUILayout.EndVertical();
            GUI.backgroundColor = prevColor;
        }
#endif

        
        /// <summary>
        /// The cached droptable
        /// </summary>
        private DropTable<T> _cachedDropTable;
        
        /// <summary>
        /// Get
        /// </summary>
        /// <param name="fromCache">Gets from the cache, if nothing is cached before it will be generated with the entries</param>
        /// <returns></returns>
        public DropTable<T> GetDropTable(bool fromCache = true)
        {
            // Make sure the entries is not null
            if (Items == null) Items = new ReorderableArrayEntry();
            
            // Check if cache is valid
            if (fromCache && _cachedDropTable != null && !IsDirty) return _cachedDropTable;

            IsDirty = false;
            // Create the drop table
            return _cachedDropTable = Items.ToDropTable(entry => entry.Object, entry => entry.Weight);
        }

        public override string EditorGetElementHeader(object instance)
        {
            if (instance is Entry entry)
            {
                var dropTable = GetDropTable();
                return $"[{dropTable.GetNormalizedWeight(entry.Object)*100:F2}%] {entry.Object}";
            }
            return "Failed to parse element";
        }

        public override void EditorOrderByNormalizedWeight(bool ascending)
        {
            var ordered = ascending
                ? Items.OrderBy(entry => EditorGetElementHeader(entry)).ToList()
                : Items.OrderByDescending(entry => EditorGetElementHeader(entry)).ToList();
            
            Items = new ReorderableArrayEntry();
            
            ordered.ForEach(Items.Add);
            // cache again
            GetDropTable(false);
        }

        public override object RandomItem(IRandomizer randomizer = null)
        {
            if (randomizer == null) randomizer = TrickIRandomizer.Default;
            return randomizer.RandomItem(GetDropTable(true));
        }

        public override object RandomItems(IRandomizer randomizer, int count, bool allowDuplicate)
        {
            if (randomizer == null) randomizer = TrickIRandomizer.Default;
            return randomizer.RandomItems(GetDropTable(true), count, allowDuplicate);
        }

        public override object RandomItems(IRandomizer randomizer, int minItems, int maxItems, bool allowDuplicate)
        {
            if (randomizer == null) randomizer = TrickIRandomizer.Default;
            return randomizer.RandomItems(GetDropTable(true), minItems, maxItems, allowDuplicate);
        }

        public override void Clear()
        {
            Items.Clear();
        }

        public override void AddObject(object item, float weight)
        {
            Items.Add(new Entry {Object = (T) item, Weight = weight});
        }

        public override bool RemoveObject(object item)
        {
            var entry = Items.FirstOrDefault(e => e.Object.Equals(item));
            if (entry.Equals(default(Entry))) return false;
            Items.Remove(entry);
            return true;
        }

        public override List<object> GetItems()
        {
            return Items.Select(e => e.Object)
                .Where(e => e != null)
                .Cast<object>()
                .ToList();
        }

        public override List<(object, float)> GetItemsWithWeights()
        {
            return Items.Select(e => ((object) e.Object, e.Weight)).ToList();
        }

        public T RandomItemAs(IRandomizer randomizer = null)
        {
            randomizer ??= TrickIRandomizer.Default;
            return randomizer.RandomItem(GetDropTable(true));
        }
        
        public List<T> RandomItemsAs(IRandomizer randomizer, int count, bool allowDuplicate)
        {
            randomizer ??= TrickIRandomizer.Default;
            return randomizer.RandomItems(GetDropTable(true), count, allowDuplicate);
        }
        
        public List<T> RandomItemsAs(IRandomizer randomizer, int minItems, int maxItems, bool allowDuplicate)
        {
            randomizer ??= TrickIRandomizer.Default;
            return randomizer.RandomItems(GetDropTable(true), minItems, maxItems, allowDuplicate);
        }
    }
}