using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TrickCore
{
    public static class DictionaryExtensions
    {
        public static bool TryGetValueClosestHigher<T2>(this IDictionary<int, T2> dict, int key, out T2 value, bool fallbackToFirst = true)
        {
            int count = dict.Count;
            if (count == 0)
            {
                value = default;
                return false;
            }

            if (dict.TryGetValue(key, out value))
                return true;

            if (count == 1)
            {
                if (fallbackToFirst)
                {
                    value = dict.Values.First();
                    return true;
                }

                var pair = dict.First();
                if (pair.Key < key) return false;
                value = pair.Value;
                return true;
            }
			
            Tuple<int, T2> bestMatch = null;
            foreach (var pair in dict)
            {
                var diff = key - pair.Key;
                if (diff < 0) continue;
                if (bestMatch == null || diff < bestMatch.Item1)
                    bestMatch = Tuple.Create(diff, pair.Value);
            }

            if (fallbackToFirst)
            {
                if (bestMatch != null)
                {
                    value = bestMatch.Item2;
                }
                else
                {
                    if (TryGetValueOperationTuple(dict, DictionaryOperatorType.LessThan, true, key, out var valueTuple))
                        value = valueTuple.Value;
                }
                return true;
            }

            if (bestMatch == null) return false;
            if (key <= bestMatch.Item1) return false;
            value = bestMatch.Item2;
            return true;
        }

        public enum DictionaryOperatorType
        {
            GreaterThanOrEqual,
            GreaterThan,
            Equals,
            LessThanOrEqual,
            LessThan,
        }

        public static bool TryGetValueOperationTuple<T2>(this IDictionary<int, T2> dict,
            DictionaryOperatorType operatorType, bool descending, int key, out (int Key, T2 Value) value)
        {
            value = default;
            if (dict == null) return false;
            switch (operatorType)
            {
                case DictionaryOperatorType.GreaterThanOrEqual:
                {
                    List<KeyValuePair<int, T2>> filtered =
                        (descending
                            ? dict.Where(pair => pair.Key >= key).OrderByDescending(pair => pair.Key)
                            : dict.Where(pair => pair.Key >= key).OrderBy(pair => pair.Key)).ToList();
                    if (filtered.Count <= 0) return false;
                    value = (filtered[0].Key, filtered[0].Value);
                    return true;
                }
                case DictionaryOperatorType.GreaterThan:
                {
                    List<KeyValuePair<int, T2>> filtered =
                        (descending
                            ? dict.Where(pair => pair.Key > key).OrderByDescending(pair => pair.Key)
                            : dict.Where(pair => pair.Key > key).OrderBy(pair => pair.Key)).ToList();
                    if (filtered.Count <= 0) return false;
                    value = (filtered[0].Key, filtered[0].Value);
                    return true;
                }
                case DictionaryOperatorType.Equals:
                {
                    List<KeyValuePair<int, T2>> filtered =
                        (descending
                            ? dict.Where(pair => pair.Key == key).OrderByDescending(pair => pair.Key)
                            : dict.Where(pair => pair.Key == key).OrderBy(pair => pair.Key)).ToList();
                    if (filtered.Count <= 0) return false;
                    value = (filtered[0].Key, filtered[0].Value);
                    return true;
                }
                case DictionaryOperatorType.LessThanOrEqual:
                {
                    List<KeyValuePair<int, T2>> filtered =
                        (descending
                            ? dict.Where(pair => pair.Key <= key).OrderByDescending(pair => pair.Key)
                            : dict.Where(pair => pair.Key <= key).OrderBy(pair => pair.Key)).ToList();
                    if (filtered.Count <= 0) return false;
                    value = (filtered[0].Key, filtered[0].Value);
                    return true;
                }
                case DictionaryOperatorType.LessThan:
                {
                    List<KeyValuePair<int, T2>> filtered =
                        (descending
                            ? dict.Where(pair => pair.Key < key).OrderByDescending(pair => pair.Key)
                            : dict.Where(pair => pair.Key < key).OrderBy(pair => pair.Key)).ToList();
                    if (filtered.Count <= 0) return false;
                    value = (filtered[0].Key, filtered[0].Value);
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetValueOnlyLowerEqualTuple<T2>(this IDictionary<int, T2> dict, int key, out (int Key, T2 Value) value)
        {
            value = default;
            if (dict == null) return false;
            var filtered = dict.Where(pair => pair.Key <= key).OrderBy(pair => pair.Key).ToList();
            if (filtered.Count <= 0) return false;
            value = (filtered[0].Key, filtered[0].Value);
            return true;
        }
        
        public static bool TryGetValueClosest<T2>(this IDictionary<int, T2> dict, int key, out T2 value)
        {
            if (dict.TryGetValue(key, out value))
                return true;

            //value = dict.OrderBy(pair => Math.Abs(pair.Key - key)).FirstOrDefault().Value;
            
            Tuple<int, T2> bestMatch = null;
            foreach (var e in dict)
            {
                var dif = Math.Abs(e.Key - key);
                if (bestMatch == null || dif < bestMatch.Item1)
                    bestMatch = Tuple.Create(dif, e.Value);
            }
            if (bestMatch != null) value = bestMatch.Item2;
            return !Equals(value, default(T2));
        }

        public static void AddOrReplace<T,T2>(this IDictionary<T, T2> dict, T key, T2 value)
        {
            if (dict.ContainsKey(key)) dict[key] = value;
            else dict.Add(key, value);
        }

        public static void AddOrReplace<T,T2>(this ConcurrentDictionary<T, T2> dict, T key, T2 value)
        {
            dict.AddOrUpdate(key, value, (arg1, arg2) => value);
        }

    }
}