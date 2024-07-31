using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using UnityEngine;

namespace TrickCore
{
    public class PoolObject : MonoBehaviour, IPoolObject
    {
        public Transform CachedTransform { get; private set; }
        public string PoolId { get; set; }
        public int InstanceId { get; set; }
        public bool IsInPool { get; set; }
        public IGameContext Context { get; set; }
        public Vector3 OriginalScale { get; set; }
        public Vector3 OriginalEuler { get; set; }

        public Dictionary<string, object> CustomData { get; set; } = new();

        public virtual void InitializePoolObject(string poolId, int instanceId, IGameContext context)
        {
            PoolId = poolId;
            InstanceId = instanceId;
            Context = context;
            CachedTransform = transform;
            OriginalScale = CachedTransform.localScale;
            OriginalEuler = CachedTransform.localEulerAngles;
        }

        public string GetObjectName() => name;
        public GameObject GetGameObject() => gameObject;

        public virtual void OnSpawn(Transform parent)
        {
            IsInPool = false;
            gameObject.SetActive(true);
            CachedTransform.SetParent(parent switch
            {
                null => Context.ContextParentRoot != null ? Context.ContextParentRoot : ObjectPoolManager.RuntimeInstance.ActiveInstancesParent,
                _ => parent
            });
        }

        public virtual void OnDespawn()
        {
            if (this == null) return;
            IsInPool = true;
            gameObject.SetActive(false);
            
            // Also reset the transform
            CachedTransform.position = Vector3.zero;
            CachedTransform.rotation = Quaternion.identity;
            CachedTransform.SetParent(ObjectPoolManager.RuntimeInstance.GetPoolParent(PoolId));
        }
        
        public virtual void ReturnToPool(float delay = 0.0f)
        {
            if (ObjectPoolManager.RuntimeInstance == null) return;
            
            if (delay > 0.0f)
            {
                Routine.StartDelay(() => ObjectPoolManager.RuntimeInstance.SendInstanceToPool(this), delay);
            }
            else
            {
                ObjectPoolManager.RuntimeInstance.SendInstanceToPool(this);
            }
        }

        protected virtual void OnDestroy()
        {
            // Ensure we are removed from the pool when the object is actually destroyed
            if (ObjectPoolManager.RuntimeInstance != null && !string.IsNullOrEmpty(PoolId)) ObjectPoolManager.RuntimeInstance.GetPoolDataByPoolId(PoolId, Context).RemovePoolInstance(this);
        }

        public Transform GetTransform()
        {
            return CachedTransform;
        }

        public virtual IEnumerator InvokeEvent(string eventName)
        {
            yield break;
        }

        public void AutoDestroyParticle()
        {
            if (!TryGetComponent<TrickAutoDestroyParticleOnComplete>(out _))
            {
                var autoDestroy = gameObject.AddComponent<TrickAutoDestroyParticleOnComplete>();
                if (autoDestroy != null) autoDestroy.Initialize(this, Context);
            }
        }
    }
}