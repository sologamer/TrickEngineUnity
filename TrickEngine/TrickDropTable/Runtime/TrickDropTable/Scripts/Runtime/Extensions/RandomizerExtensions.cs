using System;
using System.Collections.Generic;

namespace TrickCore
{
    public static class RandomizerExtensions
    {
        public static bool Roll(this IRandomizer random, float amt)
        {
            // amt = 1.0, we rolled 0.7 == true
            // amt = 0.3, we rolled 0.7 == false
            return amt >= random.NextDouble();
        }
        
        /// <summary>
        /// Get a random item using the normalized chance of the item
        /// </summary>
        /// <typeparam name="T">The return object type</typeparam>
        /// <param name="random">Randomizer object</param>
        /// <param name="table">The droptable</param>
        /// <returns>The random item</returns>
        public static T RandomItem<T>(this IRandomizer random, DropTable<T> table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            return table.GetNormalizedItemAs(random.NextDouble());
        }

        /// <summary>
        /// Get a random items using the normalized chance of the item
        /// </summary>
        /// <typeparam name="T">The return object type</typeparam>
        /// <param name="random">Randomizer object</param>
        /// <param name="table">The droptable</param>
        /// <param name="count">Amount of items</param>
        /// <param name="allowDuplicate">If true; Allow duplicate entries inside the returned list</param>
        /// <returns>The random items</returns>
        public static List<T> RandomItems<T>(this IRandomizer random, DropTable<T> table, int count, bool allowDuplicate)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            List<T> items = new List<T>();
            for (int i = 0; i < count; i++)
            {
                items.Add(table.GetNormalizedItemAs(random.NextDouble(), allowDuplicate ? null : items));
            }
            return items;
        }
        /// <summary>
        /// Get a random items using the normalized chance of the item
        /// </summary>
        /// <typeparam name="T">The return object type</typeparam>
        /// <param name="random">Randomizer object</param>
        /// <param name="table">The droptable</param>
        /// <param name="minItems">Minimum amount of items</param>
        /// <param name="maxItems">Maximum amount of items</param>
        /// <param name="allowDuplicate">If true; Allow duplicate entries inside the returned list</param>
        /// <returns>The random items</returns>
        public static List<T> RandomItems<T>(this IRandomizer random, DropTable<T> table, int minItems, int maxItems, bool allowDuplicate)
        {
            return RandomItems(random, table, random.Next(minItems, maxItems), allowDuplicate);
        }

        #region "non-generic / object support"

        /// <summary>
        /// Get a random item using the normalized chance of the item
        /// </summary>
        /// <typeparam name="T">The return object type</typeparam>
        /// <param name="random">Randomizer object</param>
        /// <param name="table">The droptable</param>
        /// <returns>The random item</returns>
        public static object RandomItem(this IRandomizer random, DropTable table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            return table.GetNormalizedItem(random.NextDouble());
        }

        /// <summary>
        /// Get a random items using the normalized chance of the item
        /// </summary>
        /// <typeparam name="T">The return object type</typeparam>
        /// <param name="random">Randomizer object</param>
        /// <param name="table">The droptable</param>
        /// <param name="count">Amount of items</param>
        /// <param name="allowDuplicate">If true; Allow duplicate entries inside the returned list</param>
        /// <returns>The random items</returns>
        public static List<object> RandomItems(this IRandomizer random, DropTable table, int count, bool allowDuplicate)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            List<object> items = new List<object>();
            for (int i = 0; i < count; i++)
            {
                items.Add(table.GetNormalizedItem(random.NextDouble(), allowDuplicate ? null : items));
            }
            return items;
        }
        /// <summary>
        /// Get a random items using the normalized chance of the item
        /// </summary>
        /// <typeparam name="T">The return object type</typeparam>
        /// <param name="random">Randomizer object</param>
        /// <param name="table">The droptable</param>
        /// <param name="minItems">Minimum amount of items</param>
        /// <param name="maxItems">Maximum amount of items</param>
        /// <param name="allowDuplicate">If true; Allow duplicate entries inside the returned list</param>
        /// <returns>The random items</returns>
        public static List<object> RandomItems(this IRandomizer random, DropTable table, int minItems, int maxItems, bool allowDuplicate)
        {
            return RandomItems(random, table, random.Next(minItems, maxItems), allowDuplicate);
        }

        #endregion
    }
}