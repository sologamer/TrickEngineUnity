using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace TrickCore
{
    public static class ListExtensions
    {
        public static IEnumerable<T> TrickTakeLast<T>(this IEnumerable<T> source, int N)
        {
            var enumerable = source as T[] ?? source.ToArray();
            return enumerable.Skip(Math.Max(0, enumerable.Count() - N));
        }
        
        public static void DestroyObjects<T>(this IEnumerable<T> source, bool clear = true) where T : MonoBehaviour
        {
            if (source == null) return;
            foreach (T behaviour in source)
            {
                Object.Destroy(behaviour.gameObject);
            }
            if (clear && source is IList list) list.Clear();
        }
        
        /// <summary>
        /// Offset a list by a number, but loops it (circular buffer)
        /// </summary>
        /// <param name="enumerable"></param>
        /// <param name="offset"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<T> OffsetList<T>(this IEnumerable<T> enumerable, int offset)
        {
            var tempList = enumerable.ToList();
            List<T> offsetList = new List<T>();
            for (int i = offset; i < tempList.Count; i++)
            {
                offsetList.Add(tempList[i]);
            }
            for (int j = 0; j < offset; j++)
            {
                offsetList.Add(tempList[j]);
            }
            return offsetList;
        }	

        /// <summary>
        /// A function to smartly instantiate an object or get from a current list, this saves unnecessary instantiations (GC's)
        /// if the current list is too large, the num exceeding the source list will be deleted
        /// </summary>
        /// <param name="source"></param>
        /// <param name="currentList"></param>
        /// <param name="onGetCallback"></param>
        /// <param name="instantiateFunc"></param>
        /// <param name="callGetCallbackOnInstantiate"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        public static void PoolGetOrInstantiate<T, T2>(this IEnumerable<T> source, ref List<T2> currentList, Action<T2, T, int> onGetCallback, Func<T, int, T2> instantiateFunc, bool callGetCallbackOnInstantiate = true, bool destroyIfExceedingSourceList = true) where T2 : MonoBehaviour
        {
            if (source == null) return;
            currentList ??= new List<T2>();
            var referenceList = source is List<T> list ? list : source.ToList();
            int numEntries = referenceList.Count;

            currentList.RemoveAll(behaviour => behaviour == null);
            
            for (int i = 0; i < numEntries; i++)
            {
                if (i >= 0 && currentList.Count > i)
                {
                    onGetCallback?.Invoke(currentList[i], referenceList[i], i);
                    if (!destroyIfExceedingSourceList)
                    {
                        currentList[i].gameObject.SetActive(true);
                    }
                }
                else
                {
                    var ret = instantiateFunc?.Invoke(referenceList[i], i);
                    if (callGetCallbackOnInstantiate) onGetCallback?.Invoke(ret, referenceList[i], i);
                    currentList.Add(ret);
                }
            }

            if (currentList.Count > numEntries)
            {
                int numToRemove = currentList.Count - numEntries;
                if (destroyIfExceedingSourceList)
                {
                    while (numToRemove > 0)
                    {
                        var lastItem = currentList.Count-1 >= 0 && currentList.Count-1 < currentList.Count ? currentList[currentList.Count-1] : null;
                        if (lastItem != null) Object.Destroy(lastItem.gameObject);
                        currentList.RemoveAt(currentList.Count - 1);
                        numToRemove--;
                    }
                }
                else
                {
                    for (int i = currentList.Count - numEntries; i < currentList.Count; i++)
                    {
                        currentList[i].gameObject.SetActive(false);
                    }
                }
            }
        }
        
        public static void PoolGetOrInstantiate<T2>(this int numEntries, ref List<T2> currentList, Action<T2, int> onGetCallback, Func<int, T2> instantiateFunc, bool callGetCallbackOnInstantiate = true, bool destroyIfExceedingSourceList = true) where T2 : MonoBehaviour
        {
            if (currentList == null) currentList = new List<T2>();
            currentList.RemoveAll(behaviour => behaviour == null);
            
            for (int i = 0; i < numEntries; i++)
            {
                if (i >= 0 && currentList.Count > i)
                {
                    onGetCallback?.Invoke(currentList[i], i);
                    if (!destroyIfExceedingSourceList)
                    {
                        currentList[i].gameObject.SetActive(true);
                    }
                }
                else
                {
                    var ret = instantiateFunc?.Invoke(i);
                    if (callGetCallbackOnInstantiate) onGetCallback?.Invoke(ret, i);
                    currentList.Add(ret);
                }
            }

            if (currentList.Count > numEntries)
            {
                int numToRemove = currentList.Count - numEntries;
                if (destroyIfExceedingSourceList)
                {
                    while (numToRemove > 0)
                    {
                        var lastItem = currentList.Count-1 >= 0 && currentList.Count-1 < currentList.Count ? currentList[currentList.Count-1] : null;
                        if (lastItem != null) Object.Destroy(lastItem.gameObject);
                        currentList.RemoveAt(currentList.Count - 1);
                        numToRemove--;
                    }
                }
                else
                {
                    for (int i = currentList.Count - numEntries; i < currentList.Count; i++)
                    {
                        currentList[i].gameObject.SetActive(false);
                    }
                }
            }
        }
        
        public static int GetSequenceHashCode<T>(this IList<T> sequence)
        {
            const int seed = 487;
            const int modifier = 31;
            if (sequence == null) return 0;
            unchecked
            {
                return sequence.Aggregate(seed, (current, item) =>
                    (current*modifier) + item.GetHashCode());
            }            
        }
        
        public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize) 
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
        
        public static T Random<T>(this List<T> list, Random randomGenerator, int startIndex = 0)
        {
            int random = randomGenerator.Next(startIndex, list.Count);
            return list.Count > random ? list[random] : default(T);
        }
        public static T Random<T>(this T[] array, Random randomGenerator, int startIndex = 0)
        {
            int random = randomGenerator.Next(startIndex, array.Length);
            return array.Length > random ? array[random] : default(T);
        }

        public static T Random<T>(this IEnumerable<T> enumerable, Random randomGenerator, int startIndex = 0)
        {
            var array = enumerable.ToArray();
            int random = randomGenerator.Next(startIndex, array.Length);
            return array.Length > random ? array[random] : default(T);
        }

        public static T Random<T>(this Array array, Random randomGenerator, int startIndex = 0)
        {
            int random = randomGenerator.Next(startIndex, array.Length);
            return (T)(array.Length > random ? array.GetValue(random) : default(T));
        }

        public static T Random<T>(this List<T> list, IRandomizer randomGenerator, int startIndex = 0)
        {
            int random = randomGenerator.Next(startIndex, list.Count);
            return list.Count > random ? list[random] : default(T);
        }
        public static T Random<T>(this T[] array, IRandomizer randomGenerator, int startIndex = 0)
        {
            int random = randomGenerator.Next(startIndex, array.Length);
            return array.Length > random ? array[random] : default(T);
        }

        public static T Random<T>(this IEnumerable<T> enumerable, IRandomizer randomGenerator, int startIndex = 0)
        {
            var array = enumerable.ToArray();
            int random = randomGenerator.Next(startIndex, array.Length);
            return array.Length > random ? array[random] : default(T);
        }

        public static T Random<T>(this Array array, IRandomizer randomGenerator, int startIndex = 0)
        {
            int random = randomGenerator.Next(startIndex, array.Length);
            return (T)(array.Length > random ? array.GetValue(random) : default(T));
        }

        
        public static T RandomAndPop<T>(this List<T> list, Random randomGenerator, int startIndex = 0)
        {
            int random = randomGenerator.Next(startIndex, list.Count);
            if (list.Count <= random) return default(T);
            var ret = list[random];
            list.RemoveAt(random);
            return ret;

        }
        public static T RandomAndPop<T>(this List<T> list, IRandomizer randomGenerator, int startIndex = 0)
        {
            int random = randomGenerator.Next(startIndex, list.Count);
            if (list.Count <= random) return default(T);
            var ret = list[random];
            list.RemoveAt(random);
            return ret;
        }
        
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> enumerable, IRandomizer rnd)
        {
            var list = enumerable.ToArray();

            for (int i = 1; i < list.Length; i++)
            {
                int pos = rnd.Next(0, i + 1);
                var x = list[i];
                list[i] = list[pos];
                list[pos] = x;
            }

            return list;
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> enumerable, Random rnd)
        {
            var list = enumerable.ToArray();

            for (int i = 1; i < list.Length; i++)
            {
                int pos = rnd.Next(i + 1);
                var x = list[i];
                list[i] = list[pos];
                list[pos] = x;
            }

            return list;
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> enumerable)
        {
            return Shuffle(enumerable, new Random());
        }

        public static IEnumerable<T> Shuffle<T>(this List<T> enumerable, int randomSeed)
        {
            return Shuffle(enumerable, new Random(randomSeed));
        }

        public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
        {
            return !enumerable.Any();
        }

        public static IEnumerable<T> ListPadding<T>(this IEnumerable<T> enumerable, int minLength)
        {
            if (enumerable == null) enumerable = new List<T>();
            var list = enumerable.ToList();
            if (list.Count < minLength) list.AddRange(Enumerable.Repeat(default(T), minLength - list.Count));
            return list;
        }
    }
}