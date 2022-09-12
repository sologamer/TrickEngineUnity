using System;
using System.Collections;
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
    }
}