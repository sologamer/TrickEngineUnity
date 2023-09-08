/**
 * MonoSingleton is an abstract class to easily create a singleton. A singleton is a single instance of an object. This also support unity OnDestroy which destroys the actual singleton
 * The singleton is actually an Unity gameobject, when the gameobject is destroyed the singleton will get destroyed aswell, this gives you more control which singletons are still active.
 * Made by Tuan Le
 */

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TrickCore;

namespace TrickCore
{
#if ODIN_INSPECTOR && !ODIN_INSPECTOR_EDITOR_ONLY
    using Sirenix.OdinInspector;
#endif

    public abstract class MonoSingleton<T>
#if !NO_UNITY
#if ODIN_INSPECTOR && !ODIN_INSPECTOR_EDITOR_ONLY
    : SerializedMonoBehaviour
#else
        : UnityEngine.MonoBehaviour
#endif
#else
        : Singleton<T>
#endif
        where T : MonoSingleton<T>
#if NO_UNITY        
        , new()
#endif
    {
#if !NO_UNITY
        private static T _instance;
        private static bool _didFindObjectOfType;

        /// <summary>
        ///     Get the instance of the singleton
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;
                // new GameObject("New " + typeof (T).Name, typeof (T)).GetComponent<T>()
                if (MonoSingletonSettings.TryFindObjectOfType && !_didFindObjectOfType)
                {
                    _didFindObjectOfType = true;
                    return _instance = FindObjectOfType<T>();
                }
                else
                    return null;
            }
        }
#endif

        #region Singleton virtuals

        /// <summary>
        ///     This function is called when the instance is used the first time.
        ///     Put all your initializations here, as you would do it in Awake
        /// </summary>
        protected
#if NO_UNITY
            override 
#else
            virtual
#endif
            void Initialize()
        {
#if NO_UNITY
            Awake();
#endif
        }

        protected
#if NO_UNITY
            override 
#else
            virtual
#endif
            void Start()
        {

        }

        protected
#if NO_UNITY
            new
#endif
            virtual void Destroy()
        {
        }

        protected virtual void ApplicationQuit()
        {
        }

        // If no other monobehaviour request the instance in an awake function
        // executing before this one, no need to search the object.
        protected virtual void Awake()
        {
#if !NO_UNITY
            if (_instance != null && _instance != this)
            {
                UnityEngine.Debug.LogError($"Failed to create a new instance of {typeof(T)}, because the singleton already exists.");
                Destroy(gameObject);
                return;
            }


            UnityEngine.Object[] instances = FindObjectsOfType(typeof(T));

            if (instances.Length == 1)
            {
                _instance = (T) instances[0];
                _instance.Initialize();
            }
            else
            {
                UnityEngine.Debug.LogError($"There are multiple MonoSingletons active of the type {typeof(T)}. The newly created instance (this) will be destroyed.");
                Destroy(gameObject);
            }
#endif
        }

#if !NO_UNITY
        // Make sure the instance isn't referenced anymore when the user quit, just in case.
        protected void OnApplicationQuit()
        {
            if (_instance != this) return;

            _instance = null;
            ApplicationQuit();
        }

        // Make sure to destroy the instance when the gameobject is destroyed
        protected void OnDestroy()
        {
            if (_instance != this) return;

            _instance = null;
            Destroy();
        }
#else // Destroy the singleton
        public void DestroySingleton()
        {
            Destroy();
        }
#endif

        #endregion
    }

#if NO_UNITY
    public class Singleton<T>
    {
        protected virtual void Initialize()
        {
            
        }

        protected virtual void Start()
        {
            
        }
    }
#endif

    public static class MonoSingletonSettings
    {
        public static bool TryFindObjectOfType = true;
    }
}