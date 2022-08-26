using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrickCore
{
    public class TrickObjectPool<T>
    {
        private readonly Stack<T> _stack = new Stack<T>();
        private readonly Func<T> _onCreate;
        private readonly Action<T> _onDestroy;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly bool _collectionCheck = true;
    
        /// <summary>
        /// Number of inactive objects in the pool.
        /// </summary>
        public int PoolSize => _stack.Count;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="actionOnCreate">Action when the object gets created</param>
        /// <param name="actionOnDestroy">Action when the object gets destroyed</param>
        /// <param name="actionOnGet">Action on get (claim).</param>
        /// <param name="actionOnRelease">Action on release.</param>
        /// <param name="collectionCheck">True if collection integrity should be checked.</param>
        public TrickObjectPool(Func<T> actionOnCreate, Action<T> actionOnDestroy, Action<T> actionOnGet, Action<T> actionOnRelease, bool collectionCheck = true)
        {
            _onGet = actionOnGet;
            _onCreate = actionOnCreate;
            _onDestroy = actionOnDestroy;
            _onRelease = actionOnRelease;
            _collectionCheck = collectionCheck;
        }
    

        public T2 GetAs<T2>() where T2 : class
        {
            return Get() as T2;
        }
    
        /// <summary>
        /// Get an object from the pool.
        /// </summary>
        /// <returns>A new object from the pool.</returns>
        public T Get()
        {
            T element;
            if (_stack.Count == 0)
            {
                element = _onCreate();
                Release(element);
                return Get();
            }
            else
            {
                element = _stack.Pop();
            }

            _onGet?.Invoke(element);
            return element;
        }

        /// <summary>
        /// Pooled object.
        /// </summary>
        public struct TrickPooledObject : IDisposable
        {
            readonly T _toReturn;
            readonly TrickObjectPool<T> _pool;

            internal TrickPooledObject(T value, TrickObjectPool<T> pool)
            {
                _toReturn = value;
                _pool = pool;
            }

            /// <summary>
            /// Disposable pattern implementation.
            /// </summary>
            void IDisposable.Dispose() => _pool.Release(_toReturn);
        }

        /// <summary>
        /// Get the new PooledObject.
        /// </summary>
        /// <param name="v">Output new typed object.</param>
        /// <returns>New PooledObject</returns>
        public TrickPooledObject Get(out T v) => new TrickPooledObject(v = Get(), this);

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

        public void SetSize(int size)
        {
            if (_stack.Count == size) return;
        
            if (size > _stack.Count)
            {
                // We need to create
                var toCreate = size - _stack.Count;
                for (int i = 0; i < toCreate; i++)
                {
                    var element = _onCreate.Invoke();
                    Release(element);
                }
            }
            else
            {
                var toRemove = _stack.Count - size;
                for (int i = 0; i < toRemove; i++)
                {
                    _onDestroy?.Invoke(Get());
                }
            }
        }
    }
}