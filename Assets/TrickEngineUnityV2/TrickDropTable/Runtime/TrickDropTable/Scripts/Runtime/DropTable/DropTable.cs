using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TrickCore
{
    /// <summary>
    /// The DropTable class is used to get a random object sorted by their weight.
    /// </summary>
    [UnityEngine.Scripting.Preserve, Serializable]
    public abstract class DropTable
    {
        public IRandomizer Randomizer { get; set; }

        public abstract object GetNormalizedItem(double roll, List<object> excludeList = null);

        public abstract void AddObject(object item, double weight);
        public abstract bool Fill(object item);

        /// <summary>
        /// Change the weight of an item
        /// </summary>
        /// <param name="item">The item to find</param>
        /// <param name="weight">The new weight</param>
        public virtual void Set(object item, double weight)
        {
            
        }

        /// <summary>
        /// Remove an item from the droptable
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <returns>Returns true if item is removed successfully</returns>
        public virtual bool Remove(object item)
        {
            return false;
        }

        /// <summary>
        /// Clear the drop table
        /// </summary>
        public virtual void Clear()
        {
            
        }
    }

    /// <summary>
    /// The DropTable class is used to get a random object sorted by their weight.
    /// </summary>
    /// <typeparam name="T">The type of the object</typeparam>
    [UnityEngine.Scripting.Preserve, Serializable, JsonObject]
    public class DropTable<T> : DropTable, IEnumerable<T>
    {
        [UnityEngine.Scripting.Preserve, Serializable, JsonObject]
        public class Item
        {
            /// <summary>
            /// The object
            /// </summary>
            [JsonProperty(PropertyName = "o")]
#if ODIN_INSPECTOR
            [HideLabel]
#endif
            public T Object;

            /// <summary>
            /// The weight
            /// </summary>
            [JsonProperty(PropertyName = "w")]
            public float Weight = 1.0f;

            /// <summary>
            /// The weight normalized to 100% (1.0f)
            /// </summary>
#if ODIN_INSPECTOR
            [ShowIf("@UnityEngine.Application.isPlaying")]
#endif
            [JsonIgnore]
            public float NormalizedWeight;

            public Item()
            {
            }

            public Item(T obj, double weight)
            {
                Object = obj;
                Weight = (float) weight;
            }
        }
#if ODIN_INSPECTOR
        [ListDrawerSettings(OnBeginListElementGUI = "BeginList")]
#endif
        public List<Item> Items = new List<Item>();

        public DropTable()
        {
        }

        public DropTable(int capacity)
        {
            Items.Capacity = capacity;
        }

#if UNITY_EDITOR
        private void BeginList(int index)
        {
            GUIStyle style = new GUIStyle();
            style.richText = true;

            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.black;
            GUILayout.BeginVertical("box");

            var pair = Items[index];
            List<string> previewTexts = new List<string>();

            if (pair == null) previewTexts.Add("null");
            else previewTexts.Add($"{(pair.NormalizedWeight * 100):F2}% - {pair.Object}");
            
            if (previewTexts.Count > 0) GUILayout.Label($"<color=white>{string.Join("\n", previewTexts)}</color>", style);
            GUILayout.EndVertical();
            GUI.backgroundColor = prevColor;
        }
        #endif
        
#if ODIN_INSPECTOR
        [Button, HorizontalGroup("DropTableHorizontal")]
#endif
        public void NormalizeWeights()
        {
            if (Items == null) return;
            UpdateDropTable(Items);
            foreach (var item in Items) item.Weight = item.NormalizedWeight;
        }

#if ODIN_INSPECTOR
        [Button, HorizontalGroup("DropTableHorizontal")]
#endif
        public void SortByWeight()
        {
            if (Items == null) return;
            UpdateDropTable(Items);
            Items = Items.OrderByDescending(item => item.NormalizedWeight).ToList();
        }

        /// <summary>
        /// Add an item to the droptable with a rate which indicates the chance of their drop
        /// </summary>
        /// <param name="item">The item object</param>
        /// <param name="weight">The rate of their drop</param>
        public void Add(T item, double weight)
        {
            Items.Add(new Item(item, weight));
            UpdateDropTable(Items);
        }

        /// <summary>
        /// Add an item to the droptable with a rate which indicates the chance of their drop
        /// </summary>
        /// <param name="item">The item object</param>
        /// <param name="weight">The rate of their drop</param>
        public override void AddObject(object item, double weight)
        {
            Items.Add(new Item((T) item, weight));
            UpdateDropTable(Items);
        }

        /// <summary>
        /// Tries to add the item to the droptable with the remaining weight.
        /// </summary>
        /// <param name="item">The item to fill</param>
        /// <returns>Returns true if the item can be filled into the droptable else it will return false</returns>
        public override bool Fill(object item)
        {
            double sumWeight = Items.Sum(i => i.Weight);
            if (sumWeight < 1.0f)
            {
                double restWeight = 1.0f - sumWeight;
                Items.Add(new Item((T) item, restWeight));
                UpdateDropTable(Items);

                return true;
            }

            return false;
        }

        
        /// <summary>
        /// Get normalized weights from each item in the drop table
        /// </summary>
        /// <returns></returns>
        public List<KeyValuePair<T, double>> GetAllNormalizedWeights()
        {
            return Items.Select(item => new KeyValuePair<T, double>(item.Object, item.NormalizedWeight)).ToList();
        }
        
        public List<T> ToList()
        {
            return Items.Select(item => item.Object).ToList();
        }

        public override void Set(object item, double weight)
        {
            Item o = Items.Find(obj => Equals(obj.Object, item));
            if (o != null)
            {
                o.Weight = (float) weight;
                UpdateDropTable(Items);
            }
        }

        public override bool Remove(object item)
        {
            int removed = Items.RemoveAll(obj => Equals(obj.Object, item));
            if (removed > 0)
            {
                UpdateDropTable(Items);
                return true;
            }
            return false;
        }

        public override void Clear()
        {
            Items.Clear();
        }

        public override string ToString()
        {
            string str = string.Empty;
            for (var i = 0; i < Items.Count; i++)
            {
                Item item = Items[i];

                str += $"Object:{item.Object} - Weight:{item.Weight} NormalizedWeight:{item.NormalizedWeight}";

                if (i != Items.Count - 1)
                    str += Environment.NewLine;
            }
            return str;
        }

        /// <summary>
        /// Update drop table. Recalculates the normalized weight from an item list
        /// </summary>
        protected void UpdateDropTable(List<Item> items)
        {
            // Update normalized weight
            double sumWeight = items.Sum(i => i.Weight);

            if (sumWeight <= 0)
            {
                float averageWeight = 1.0f / items.Count;
                foreach (Item obj in items)
                {
                    obj.NormalizedWeight = averageWeight;
                }
            }
            else
            {
                double multiplier = 1.0f / sumWeight;
                foreach (Item obj in items)
                {
                    obj.NormalizedWeight = (float) (obj.Weight * multiplier);
                }
            }
        }
        
        public double GetNormalizedWeight(object obj)
        {
            // Update normalized weights
            UpdateDropTable(Items);
            double normalizedWeight = (from item in Items where Equals(item.Object, obj) select item.NormalizedWeight).FirstOrDefault();
            return double.IsNaN(normalizedWeight) ? 0.0f : normalizedWeight;
        }
        
        public override object GetNormalizedItem(double roll, List<object> excludeList = null)
        {
            double currentWeight = 0.0f;

            var items = excludeList != null ? Items.Where(item => !excludeList.Contains(item.Object)).ToList() : Items;

            // Update normalized weights
            UpdateDropTable(items);

            foreach (Item item in items)
            {
                if (item.NormalizedWeight <= 0.0f) continue;
                double nextWeight = currentWeight + item.NormalizedWeight;
                if (roll >= currentWeight && roll <= nextWeight)
                {
                    return item.Object is DropTable drop ? (Randomizer ?? StrongRandom.Default).RandomItem(drop) : item.Object;
                }
                currentWeight += item.NormalizedWeight;
            }
            return default(T);
        }

        /// <summary>
        /// Get a random item using the normalized drop rate, this function can only return a null object whenever no item is added to the drop table.
        /// </summary>
        /// <param name="roll">The rate/roll (value between 0-1)</param>
        /// <param name="excludeList">List of excluded items</param>
        /// <returns>Returns the item, this can only return null if there are no items into the droptable!</returns>
        public T GetNormalizedItemAs(double roll, IEnumerable<T> excludeList = null)
        {
            double currentWeight = 0.0f;

            var items = excludeList != null ? Items.Where(item => item.Object is T obj && !excludeList.Contains(obj)).ToList() : Items;

            // Update normalized weights
            UpdateDropTable(items);

            foreach (Item item in items)
            {
                if (item.NormalizedWeight <= 0.0f) continue;
                double nextWeight = currentWeight + item.NormalizedWeight;
                if (roll >= currentWeight && roll <= nextWeight)
                {
                    return item.Object is DropTable drop ? (T) (Randomizer ?? StrongRandom.Default).RandomItem(drop) : (T) item.Object;
                }
                currentWeight += item.NormalizedWeight;
            }
            return default(T);
        }

        /// <summary>
        /// Convert the drop table to a list
        /// </summary>
        /// <returns></returns>
        public List<T> ToListAs()
        {
            return Items.Select(item => item.Object).ToList();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new DropTableEnumT<T>(Items);
        }

        public IEnumerator GetEnumerator()
        {
            return new DropTableEnumT<T>(Items);
        }
    }
    
    [UnityEngine.Scripting.Preserve, Serializable]
    public class DropTableEnumT<T> : IEnumerator<T>
    {
        private readonly List<DropTable<T>.Item> _items;
        private int _position = -1;

        public DropTableEnumT(List<DropTable<T>.Item> list)
        {
            _items = list;
        }

        public bool MoveNext()
        {
            _position++;
            return (_position < _items.Count);
        }

        public void Reset()
        {
            _position = -1;
        }

        object IEnumerator.Current => Current;

        public T Current
        {
            get
            {
                try
                {
                    return (T) _items[_position].Object;
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public void Dispose()
        {
        }
    }
}