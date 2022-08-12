using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrickCore;
using UnityEngine;
using UnityEngine.Events;

public class TrickObjectPool<T>
{
    readonly Stack<T> m_Stack = new Stack<T>();
    readonly Func<T> m_ActionOnCreate;
    readonly UnityAction<T> m_ActionOnDestroy;
    readonly UnityAction<T> m_ActionOnGet;
    readonly UnityAction<T> m_ActionOnRelease;
    readonly bool m_CollectionCheck = true;
    
    /// <summary>
    /// Number of inactive objects in the pool.
    /// </summary>
    public int PoolSize { get { return m_Stack.Count; } }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="actionOnGet">Action on get.</param>
    /// <param name="actionOnRelease">Action on release.</param>
    /// <param name="collectionCheck">True if collection integrity should be checked.</param>
    public TrickObjectPool(Func<T> actionOnCreate, UnityAction<T> actionOnDestroy, UnityAction<T> actionOnGet, UnityAction<T> actionOnRelease, bool collectionCheck = true)
    {
        m_ActionOnGet = actionOnGet;
        m_ActionOnCreate = actionOnCreate;
        m_ActionOnDestroy = actionOnDestroy;
        m_ActionOnRelease = actionOnRelease;
        m_CollectionCheck = collectionCheck;
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
        if (m_Stack.Count == 0)
        {
            element = m_ActionOnCreate();
            Release(element);
            return Get();
        }
        else
        {
            element = m_Stack.Pop();
        }
        if (m_ActionOnGet != null)
            m_ActionOnGet(element);
        return element;
    }

    /// <summary>
    /// Pooled object.
    /// </summary>
    public struct TrickPooledObject : IDisposable
    {
        readonly T m_ToReturn;
        readonly TrickObjectPool<T> m_Pool;

        internal TrickPooledObject(T value, TrickObjectPool<T> pool)
        {
            m_ToReturn = value;
            m_Pool = pool;
        }

        /// <summary>
        /// Disposable pattern implementation.
        /// </summary>
        void IDisposable.Dispose() => m_Pool.Release(m_ToReturn);
    }

    /// <summary>
    /// Get et new PooledObject.
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
        if (m_CollectionCheck && m_Stack.Count > 0)
        {
            if (m_Stack.Contains(element))
                Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
        }
#endif
        if (m_ActionOnRelease != null)
            m_ActionOnRelease(element);
        m_Stack.Push(element);
    }

    public void SetSize(int size)
    {
        if (m_Stack.Count == size) return;
        
        if (size > m_Stack.Count)
        {
            // We need to create
            var toCreate = size - m_Stack.Count;
            Debug.Log("To create: " + toCreate);
            for (int i = 0; i < toCreate; i++)
            {
                var element = m_ActionOnCreate.Invoke();
                Release(element);
            }
        }
        else
        {
            var toRemove = m_Stack.Count - size;
            Debug.Log("To remove: " + toRemove);
            for (int i = 0; i < toRemove; i++)
            {
                m_ActionOnDestroy?.Invoke(Get());
            }
        }
    }
}