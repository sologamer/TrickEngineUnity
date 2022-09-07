using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TrickCore
{
    public static class IEnumerableExtensions
    {
        public static bool TryAddTwo<T, T2>(this ConcurrentDictionary<T, T2> dictionary, ConcurrentDictionary<T, T2> anotherDictionary, T key, T2 value)
        {
            return dictionary.TryAdd(key, value) || anotherDictionary.TryAdd(key, value);
        }

        public static bool TryRemoveTwo<T, T2>(this ConcurrentDictionary<T, T2> dictionary, ConcurrentDictionary<T, T2> anotherDictionary, T key, out T2 value)
        {
            return dictionary.TryRemove(key, out value) || anotherDictionary.TryRemove(key, out value);
        }

        public static bool TryAddTwo<T, T2>(this ConcurrentDictionary<T, T2> dictionary, ConcurrentQueue<KeyValuePair<T, T2>> queue, T key, T2 value)
        {
            if (!dictionary.TryAdd(key, value))
            {
                queue.Enqueue(new KeyValuePair<T, T2>(key, value));
                return false;
            }

            return true;
        }

        public static bool TryRemoveTwo<T, T2>(this ConcurrentDictionary<T, T2> dictionary, ConcurrentQueue<KeyValuePair<T, T2>> queue, T key, out T2 value)
        {
            if (!dictionary.TryRemove(key, out value))
            {
                queue.Enqueue(new KeyValuePair<T, T2>(key, value));
                return false;
            }

            return true;
        }

        public static bool TryRemoveDebug<T, T2>(this ConcurrentDictionary<T, T2> dictionary, T key, out T2 value, string message = null)
        {
            if (dictionary.TryRemove(key, out value)) return true;
            //Console.WriteLine(message ?? Environment.StackTrace);
            return false;
        }

        public static bool TryDequeueDebug<T>(this ConcurrentQueue<T> dictionary, out T value, string message = null)
        {
            if (dictionary.TryDequeue(out value)) return true;
            //Console.WriteLine(message ?? Environment.StackTrace);
            return false;
        }
    }
}