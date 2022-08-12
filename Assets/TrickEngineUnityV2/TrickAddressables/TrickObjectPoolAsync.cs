#if UNITY_ADDRESSABLES
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using BeauRoutine;
using TrickCore;
using UnityEngine;
using UnityEngine.Events;

public class TrickObjectPoolAsync<T>
{
    private readonly Dictionary<int, T> m_Queue = new Dictionary<int, T>();
    private readonly Stack<T> m_Stack = new Stack<T>();
    private readonly Func<Action<ITrickPool>, IEnumerator> m_ActionOnCreateAsync;
    private readonly UnityAction<T> m_ActionOnDestroy;
    private readonly UnityAction<T> m_ActionOnGet;
    private readonly UnityAction<T> m_ActionOnRelease;
    private readonly bool m_CollectionCheck = true;
    private int m_QueueId = 0;
    
    private bool m_IsAsync;

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
    public TrickObjectPoolAsync(Func<Action<ITrickPool>, IEnumerator> actionOnCreateAsync, UnityAction<T> actionOnDestroy, UnityAction<T> actionOnGet, UnityAction<T> actionOnRelease, bool collectionCheck = true)
    {
        m_ActionOnGet = actionOnGet;
        m_ActionOnCreateAsync = actionOnCreateAsync;
        m_ActionOnDestroy = actionOnDestroy;
        m_ActionOnRelease = actionOnRelease;
        m_CollectionCheck = collectionCheck;
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
        if (m_Stack.Count == 0)
        {
            Routine.Start(m_ActionOnCreateAsync.Invoke(pool =>
            {
                Release((T) pool);
                element = m_Stack.Pop();
                m_ActionOnGet?.Invoke(element);

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

        element = m_Stack.Pop();
        m_ActionOnGet?.Invoke(element);
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
        if (m_Stack.Count == 0)
        {
            yield return m_ActionOnCreateAsync.Invoke(pool =>
            {
                Release((T) pool);
                element = m_Stack.Pop();
                m_ActionOnGet?.Invoke(element);
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

        element = m_Stack.Pop();
        m_ActionOnGet?.Invoke(element);
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
        if (m_CollectionCheck && m_Stack.Count > 0)
        {
            if (m_Stack.Contains(element))
                Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
        }
#endif
        m_ActionOnRelease?.Invoke(element);
        m_Stack.Push(element);
    }

    public void AddSize(int addSize, Action<T> customCreateAction = null)
    {
        SetSize(PoolSize + addSize, customCreateAction);
    }
    public void SetSize(int size, Action<T> customCreateAction = null)
    {
        if (m_Stack.Count == size) return;
        
        if (size > m_Stack.Count)
        {
            // We need to create
            var toCreate = size - m_Stack.Count;
            IEnumerator CreateAndRelease(int index)
            {
                yield return m_ActionOnCreateAsync.Invoke(pool =>
                {
                    customCreateAction?.Invoke((T) pool);
                    Release((T) pool);                            
                });
            }

            ObjectPoolManager.Instance.StartMultiCoroutine(toCreate, CreateAndRelease);
        }
        else
        {
            var toRemove = m_Stack.Count - size;
            while (toRemove > 0)
            {
                if (m_Stack.Count == 0) break;
                var pop = m_Stack.Pop();
                m_ActionOnDestroy?.Invoke(pop);
                toRemove--;
            }
        }
    }
}
#endif