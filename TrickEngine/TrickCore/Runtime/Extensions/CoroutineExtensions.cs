using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BeauRoutine;
using UnityEngine;

namespace TrickCore
{
    public static class CoroutineExtensions
    {
        private static List<Routine> _list = new List<Routine>();

        public static IEnumerator StartCoroutineAll(this MonoBehaviour monoBehaviour, IEnumerable<IEnumerator> enumerators, Action<List<Routine>> coroutinesCallback = null)
        {
            if (monoBehaviour == null) yield break;
            if (enumerators == null) yield break;
            var enumeratorList = enumerators.ToList();
            if (enumeratorList.Count == 0)
            {
                coroutinesCallback?.Invoke(_list);
                yield break;
            }
            var combined = Routine.Combine(enumeratorList);
            coroutinesCallback?.Invoke(_list);
            yield return combined;
        }

        public static IEnumerator StartMultiCoroutine(this MonoBehaviour monoBehaviour, int num,
            Func<int, IEnumerator> obj)
        {
            return StartMultiCoroutineConcurrent(monoBehaviour, num, num, obj);
        }

        public static IEnumerator StartMultiCoroutineConcurrent(this MonoBehaviour monoBehaviour, int num, int concurrent,
            Func<int, IEnumerator> obj)
        {
            if (monoBehaviour == null) yield break;
            if (obj == null) yield break;

            IEnumerator Stepper(int startIndex)
            {
                List<IEnumerator> enumerators = new List<IEnumerator>();
                int nextTarget = Math.Min(startIndex + concurrent, num);
                for (int i = startIndex; i < nextTarget; i++)
                {
                    if (i >= num) break;
                    var routine = obj(i);
                    if (routine != null) enumerators.Add(routine);
                }

                if (enumerators.Count > 0) yield return Routine.Combine(enumerators);
            }

            int start = 0;
            while (start < num)
            {
                yield return Stepper(start);
                start += concurrent;
            }
        }
    }
}