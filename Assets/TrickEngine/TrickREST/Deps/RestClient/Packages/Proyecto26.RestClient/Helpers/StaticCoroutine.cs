using System;
using UnityEngine;
using System.Collections;
using Object = UnityEngine.Object;

namespace Proyecto26
{
    public static class StaticCoroutine
    {
        public static Func<IEnumerator, Coroutine> StartCoroutineFunc;
        
        private class CoroutineHolder : MonoBehaviour { }

        private static CoroutineHolder _runner;
        private static CoroutineHolder Runner
        {
            get
            {
                if (_runner == null)
                {
                    _runner = new GameObject("Static Coroutine RestClient").AddComponent<CoroutineHolder>();
                    Object.DontDestroyOnLoad(_runner);
                }
                return _runner;
            }
        }

        public static Coroutine StartCoroutine(IEnumerator coroutine)
        {
            return StartCoroutineFunc != null ? StartCoroutineFunc?.Invoke(coroutine) : Runner.StartCoroutine(coroutine);
        }
    }
}
