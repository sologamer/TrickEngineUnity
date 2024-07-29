using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using UnityEngine;

namespace TrickCore
{
    /// <summary>
    /// The TrickEngineManager automatically initializes the TrickEngine.Init, updates TrickEngine.Update, and TrickEngine.Exit upon exit
    /// </summary>
    public sealed class TrickEngineManager : MonoSingleton<TrickEngineManager>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitializeOnLoad()
        {
            var instance = new GameObject("TrickEngineManager", typeof(TrickEngineManager)).GetComponent<TrickEngineManager>();
            instance.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(instance.gameObject);
        }

        protected override void Initialize()
        {
            base.Initialize();
            
            Logger.LogTarget = new TrickUnityLogger();
            try { TrickEngine.Init(); } catch (Exception) { /*ignored*/ }

#if ENABLE_ZLIB
            ByteArrayExtensions.ZLibEncodeFunc = lzip.compressBuffer;
            ByteArrayExtensions.ZLibDecodeFunc = lzip.decompressBuffer;
#endif
            
            Routine.Start(EndOfFrameUpdateEnumerator());
        }

        private IEnumerator EndOfFrameUpdateEnumerator()
        {
            while (true)
            {
                yield return Routine.WaitForEndOfFrame();
                TrickEngine.ExecuteDispatchContainer(DispatchContainerType.WaitForEndOfFrame);
            }
            // ReSharper disable once IteratorNeverReturns
        }

        public void Update()
        {
            TrickEngine.Update();
            TrickEngine.ExecuteDispatchContainer(DispatchContainerType.WaitForNewFrame);
        }

        protected override void ApplicationQuit()
        {
            base.ApplicationQuit();
            
            TrickEngine.Exit();
        }

        public static IEnumerator RuntimeStartCoroutineAll(List<IEnumerator> enumerators)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                yield return Instance.StartCoroutineAll(enumerators);
            }
            else
            {
                foreach (IEnumerator enumerator in enumerators)
                {
                    yield return Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(enumerator);
                }
            }
#else
            yield return Instance.StartCoroutineAll(enumerators);
#endif
        }

        public static void RuntimeStartCoroutineAllVoid(List<IEnumerator> enumerators, Action loadCallback = null)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                var routine = Routine.Start(Instance.StartCoroutineAll(enumerators));
                if (loadCallback != null) routine.OnComplete(loadCallback);
            }
            else
            {
                int expected = enumerators.Count;
                int current = 0;

                IEnumerator CountWrap(int i)
                {
                    yield return enumerators[i];
                    current++;
                    if (current == expected)
                    {
                        loadCallback?.Invoke();
                    }
                }

                for (var i = 0; i < enumerators.Count; i++)
                {
                    Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(CountWrap(i));
                }
            }
#else
            var routine = Routine.Start(Instance.StartCoroutineAll(enumerators));
            if (loadCallback != null) routine.OnComplete(loadCallback);
#endif
        }


        public static IEnumerator RuntimeStartCoroutine(IEnumerator enumerator)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                yield return Routine.Start(enumerator);
            }
            else
            {
                yield return Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(enumerator);
            }
#else
            yield return Routine.Start(enumerator);
#endif
        }

        public static void RuntimeStartCoroutineVoid(IEnumerator enumerator)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Routine.Start(enumerator);
            }
            else
            {
                Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutineOwnerless(enumerator);
            }
#else
            Routine.Start(enumerator);
#endif
        }
    }
}