using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace TrickCore
{
    /// <summary>
    /// The UnityDropTable base class
    /// </summary>
    [Preserve, JsonObject, Serializable]
    public abstract class BaseUnityDropTable : IDropTable
    {
        /// <summary>
        /// If true, the drop table might have been modified.
        /// </summary>
        public bool IsDirty { get; set; }
        
        /// <summary>
        /// Get the header of the element of the entries [PCT%] {ElementName}
        /// </summary>
        /// <param name="instance">The Entry object</param>
        /// <returns>The string to display</returns>
        public abstract string EditorGetElementHeader(object instance);
        
        /// <summary>
        /// Order the droptable
        /// </summary>
        /// <param name="ascending">True if ascending, false for descending</param>
        public abstract void EditorOrderByNormalizedWeight(bool ascending);
        
        /// <summary>
        /// Get a random object from the DropTable using a randomizer. If no randomizer is supplied, TrickIRandomizer.DefaultPcg32 (System.Random) will be used.
        /// </summary>
        /// <param name="randomizer">The randomizer</param>
        /// <returns>Returns the random object from the DropTable</returns>
        public abstract object RandomItem(IRandomizer randomizer = null);
        
        /// <summary>
        /// Get random objects from the DropTable using a randomizer. If no randomizer is supplied, TrickIRandomizer.DefaultPcg32 (System.Random) will be used.
        /// </summary>
        /// <param name="randomizer">The randomizer</param>
        /// <param name="count">Count items</param>
        /// <param name="allowDuplicate">True if duplicate entries, if no entry is found null will be supplied</param>
        /// <returns>Returns the list of the randomized objects from the DropTable</returns>
        public abstract object RandomItems(IRandomizer randomizer, int count, bool allowDuplicate);
        
        /// <summary>
        /// Get random objects from the DropTable using a randomizer. If no randomizer is supplied, TrickIRandomizer.DefaultPcg32 (System.Random) will be used.
        /// </summary>
        /// <param name="randomizer">The randomizer</param>
        /// <param name="minItems">Num min items</param>
        /// <param name="maxItems">Num max items</param>
        /// <param name="allowDuplicate">True if duplicate entries, if no entry is found null will be supplied</param>
        /// <returns>Returns the list of the randomized objects from the DropTable</returns>
        public abstract object RandomItems(IRandomizer randomizer, int minItems, int maxItems, bool allowDuplicate);

        public abstract void Clear();
        public abstract void AddObject(object item, float weight);
        public abstract bool RemoveObject(object item);
        
        public abstract List<object> GetItems();
        public abstract List<(object, float)> GetItemsWithWeights();
    }
}