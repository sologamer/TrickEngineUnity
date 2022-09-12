#if UNITY_ADDRESSABLES
using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using TrickCore;
using UnityEngine;

namespace TrickCore
{
    public sealed class TrickObjectPoolAsync<T>
    {
        private readonly Dictionary<int, T> _queue = new Dictionary<int, T>();
        private readonly Stack<T> _stack = new Stack<T>();
        private readonly Func<Action<ITrickPool>, IEnumerator> _onCreateAsync;
        private readonly Action<T> _onDestroy;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly bool _collectionCheck = true;
        private bool _isAsync;

        /// <summary>
        /// Number of inactive objects in the pool.
        /// </summary>
        public int PoolSize => _stack.Count;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="onCreateAsync">Action when the object gets created</param>
        /// <param name="onDestroy">Action when the object gets destroyed</param>
        /// <param name="onGet">Action on get (claim).</param>
        /// <param name="onRelease">Action on release.</param>
        /// <param name="collectionCheck">True if collection integrity should be checked.</param>
        public TrickObjectPoolAsync(Func<Action<ITrickPool>, IEnumerator> onCreateAsync,
            Action<T> onDestroy, Action<T> onGet, Action<T> onRelease,
            bool collectionCheck = true)
        {
            _onGet = onGet;
            _onCreateAsync = onCreateAsync;
            _onDestroy = onDestroy;
            _onRelease = onRelease;
            _collectionCheck = collectionCheck;
        }

        /// <summary>
        /// Get an object from the pool.
        /// </summary>
        /// <param name="getItemCallback"></param>
        /// <returns>A new object from the pool.</returns>
        public void GetAsync<T2>(Action<T2> getItemCallback) where T2 : class
        {
            T element = default;
        
            // We have to create one
            if (_stack.Count == 0)
            {
                Routine.Start(_onCreateAsync.Invoke(pool =>
                {
                    Release((T) pool);
                    element = _stack.Pop();
                    _onGet?.Invoke(element);

                    if (element is DynamicMonoTrickPool dynTrickPool)
                    {
                        if (dynTrickPool is T2 dynT2)
                        {
                            getItemCallback?.Invoke(dynT2);
                        }
                        else
                        {
                            var find = dynTrickPool.GetComponent<T2>();
                            if (find == null)
                            {
                                find = dynTrickPool.gameObject.AddComponent(typeof(T2)) as T2;
                                if (find is ITrickPool x)
                                {
                                    x.AssetGroupType = dynTrickPool.AssetGroupType;
                                    x.AssetReferenceGameObject = dynTrickPool.AssetReferenceGameObject;
                                    x.IsClaimed = dynTrickPool.IsClaimed;
                                    dynTrickPool.RedirectTarget = x;
                                }
                            }


                            getItemCallback?.Invoke(find);
                        }
                    }
                    else
                    {
                        getItemCallback?.Invoke(element as T2);
                    }
                }));
                return;
            }

            element = _stack.Pop();
            _onGet?.Invoke(element);
            element = FixElement<T2>(ref element);
            getItemCallback?.Invoke(element as T2);
        }

        private static T FixElement<T2>(ref T element) where T2 : class
        {
            if (element is T2) return element;
            // get it as mono
            var mono = element as MonoBehaviour;
            // now we add the T2 on the mono
            if (mono != null)
            {
                var go = mono.gameObject;
                var newElementComp = go.AddComponent(typeof(T2)) as T2;
                // remove the old mono
                UnityEngine.Object.Destroy(mono);
                // set new element
                element = (T) (object)newElementComp;
                
                // if redirector exists fix it
                var dynTrickPool = go.GetComponent<DynamicMonoTrickPool>();
                if (dynTrickPool != null && newElementComp is ITrickPool x)
                {
                    x.AssetGroupType = dynTrickPool.AssetGroupType;
                    x.AssetReferenceGameObject = dynTrickPool.AssetReferenceGameObject;
                    x.IsClaimed = dynTrickPool.IsClaimed;
                    dynTrickPool.RedirectTarget = x;
                }
            }
            return element;
        }

        /// <summary>
        /// Get an object from the pool.
        /// </summary>
        /// <returns>A new object from the pool.</returns>
        public IEnumerator GetCoroutineAs<T2>(Action<T2> callback) where T2 : class
        {
            T element = default;
        
            // We have to create one
            if (_stack.Count == 0)
            {
                yield return _onCreateAsync.Invoke(pool =>
                {
                    Release((T) pool);
                    element = _stack.Pop();
                    _onGet?.Invoke(element);
                    if (element is DynamicMonoTrickPool dynTrickPool)
                    {
                        if (dynTrickPool is T2 dynT2)
                        {
                            callback?.Invoke(dynT2);
                        }
                        else
                        {
                            var find = dynTrickPool.GetComponent<T2>();
                            if (find == null)
                            {
                                find = dynTrickPool.gameObject.AddComponent(typeof(T2)) as T2;
                                if (find is ITrickPool x)
                                {
                                    x.AssetGroupType = dynTrickPool.AssetGroupType;
                                    x.AssetReferenceGameObject = dynTrickPool.AssetReferenceGameObject;
                                    x.IsClaimed = dynTrickPool.IsClaimed;
                                    dynTrickPool.RedirectTarget = x;
                                }
                            }

                        
                            callback?.Invoke(find);
                        }
                    }
                    else
                    {
                        callback?.Invoke(element as T2);
                    }
                });

                yield break;
            }

            element = _stack.Pop();
            _onGet?.Invoke(element);
            element = FixElement<T2>(ref element);
            callback?.Invoke(element as T2);
        }

        /// <summary>
        /// Release an object to the pool.
        /// </summary>
        /// <param name="element">Object to release.</param>
        public void Release(T element)
        {
#if UNITY_EDITOR // keep heavy checks in editor
            if (_collectionCheck && _stack.Count > 0)
            {
                if (_stack.Contains(element))
                    Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
            }
#endif
            _onRelease?.Invoke(element);
            _stack.Push(element);
        }

        public void AddSize(int addSize, Action<T> customCreateAction = null)
        {
            SetSize(PoolSize + addSize, customCreateAction);
        }
        public void SetSize(int size, Action<T> customCreateAction = null)
        {
            if (_stack.Count == size) return;
        
            if (size > _stack.Count)
            {
                // We need to create
                var toCreate = size - _stack.Count;
                IEnumerator CreateAndRelease(int index)
                {
                    yield return _onCreateAsync.Invoke(pool =>
                    {
                        customCreateAction?.Invoke((T) pool);
                        Release((T) pool);                            
                    });
                }

                ObjectPoolManager.Instance.StartMultiCoroutine(toCreate, CreateAndRelease);
            }
            else
            {
                var toRemove = _stack.Count - size;
                while (toRemove > 0)
                {
                    if (_stack.Count == 0) break;
                    var pop = _stack.Pop();
                    _onDestroy?.Invoke(pop);
                    toRemove--;
                }
            }
        }
    }
}
#endif