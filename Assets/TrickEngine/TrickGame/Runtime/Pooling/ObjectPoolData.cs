using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BeauRoutine;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace TrickCore
{
    public class ObjectPoolData
    {
        private PoolObject _prefab;
        private Object _prefabAsset;
        private readonly string _poolId;
        private readonly int _initialCapacity;
        private int _count;
        private AssetReference _assetReference;
        private bool _isResolved;
        private readonly Type _poolType;
        
        private List<IPoolObject> Instances { get; } = new();
        public Transform PoolParent { get; set; }
        public int ReferenceCount { get; set; }

        public string PoolId => _poolId;

        public PoolObject GetPrefab() => _prefab;
        public Object GetPrefabAsset() => _prefabAsset;
        
        public T GetContextInstanceAs<T>(IGameContext context, Transform parent) where T : Component => GetContextInstance(context, parent).GetComponent<T>();
        public T GetInstanceAs<T>(Transform parent) where T : Component => GetContextInstance(null, parent).GetComponent<T>();
        public T GetContextInstanceAs<T>(IGameContext context, Transform parent, Vector3 position, Quaternion rotation) where T : Component => GetContextInstance(context, parent, position, rotation) as T;
        public T GetInstanceAs<T>(Transform parent, Vector3 position, Quaternion rotation) where T : Component => GetInstance(parent, position, rotation) as T;
        public T GetAssetAs<T>() where T : Object => typeof(T) == typeof(GameObject) && _prefab != null ? _prefab.gameObject as T : _prefabAsset as T;
        
        private PoolObject GetContextInstance(IGameContext context, Transform parent)
        {
            var instances = context == null ? Instances.Where(instance => instance.IsInPool && instance.Context == null) : Instances.Where(instance => instance.IsInPool && instance.Context == context);
            foreach (var instance in instances)
            {
                instance.OnSpawn(parent);
                ReferenceCount++;
                return instance as PoolObject;
            }

            var existingInstance = AddPoolInstance(context);
            existingInstance?.OnSpawn(parent);
            ReferenceCount++;
            return existingInstance as PoolObject;
        }

        public PoolObject GetContextInstance(IGameContext context, Transform parent, Vector3 position, Quaternion rotation)
        {
            var instances = context == null ? Instances.Where(instance => instance.IsInPool && instance.Context == null) : Instances.Where(instance => instance.IsInPool && instance.Context == context);
            foreach (var instance in instances)
            {
                if (instance is not PoolObject poolObject) continue;
                var tr = poolObject.transform;
                tr.position = position;
                tr.rotation = rotation;
                instance.OnSpawn(parent);
                ReferenceCount++;
                return poolObject;
            }

            var existingInstance = AddPoolInstance(context) as PoolObject;
            if (existingInstance == null) return null;
            
            {
                var tr = existingInstance.transform;
                tr.position = position;
                tr.rotation = rotation;
                existingInstance.OnSpawn(parent);
                ReferenceCount++;
                return existingInstance;
            }
        }

        public PoolObject GetInstance(Transform parent, Vector3 position, Quaternion rotation) => GetContextInstance(null, parent, position, rotation);

        public ObjectPoolData(string poolId, int initialCapacity, Type poolType)
        {
            _poolId = poolId;
            _prefab = null;
            _prefabAsset = null;
            _initialCapacity = initialCapacity;
            _poolType = poolType;

            PoolParent = new GameObject($"null-{poolId}").transform;
            PoolParent.SetParent(ObjectPoolManager.RuntimeInstance.PoolTransform != null ? ObjectPoolManager.RuntimeInstance.transform : null);
            PoolParent.gameObject.hideFlags = ObjectPoolManager.RuntimeInstance.PoolHideFlags;
        }


        public void ResolvePoolPrefabAsset(Object handleResult, AssetReference assetReference)
        {
            if (_prefabAsset != null) return;
            
            _prefabAsset = handleResult;
            _assetReference = assetReference;
            _isResolved = true;
            PoolParent.name = $"{_prefabAsset.name}-{_poolId}";

            if (handleResult is ScriptableObject)
                for (var i = 0; i < _initialCapacity; i++) AddPoolInstance(null);
        }
        
        public void ResolvePoolPrefab(PoolObject result, AssetReference assetReference)
        {
            if (_prefab != null) return;

            _prefab = result;
            _assetReference = assetReference;
            _isResolved = true;
            PoolParent.name = $"{_prefab.name}-{_poolId}";
        }

        public void SendToPool(IPoolObject instance)
        {
            if (!Instances.Contains(instance))
            {
                Debug.LogError($"Pool with id {instance.PoolId} does not contain instance {instance.GetObjectName()}!");
                return;
            }
            
            ReferenceCount--;
            instance.OnDespawn();
        }

        public void CleanupPool()
        {
            foreach (var instance in Instances)
            {
                Object.Destroy(instance.GetGameObject());
            }

            ReferenceCount = 0;
            Instances.Clear();
        }

        public IPoolObject AddPoolInstance(IGameContext context)
        {
            return _poolType == typeof(AssetReferenceGameObject) ? AddPoolInstanceGameObject(context) : AddPoolInstanceAsset();
        }

        private IPoolObject AddPoolInstanceAsset()
        {
            if (_prefabAsset == null)
            {
                Debug.LogError($"Instance for pool {_poolId} is not resolved!");
                return null;
            }

            var copy = Object.Instantiate(_prefabAsset) as IPoolObject;
            Instances.Add(copy);
            return copy;
        }

        private IPoolObject AddPoolInstanceGameObject(IGameContext context)
        {
            if (_prefab == null)
            {
                Debug.LogError($"Instance for pool {_poolId} is not resolved!");
                return null;
            }
            
            if (context == null)
            {
                Debug.LogError($"Context is null for pool {_poolId}!");
                return null;
            }

            var instance = Object.Instantiate(_prefab);
            instance.name = $"{_prefab.name} #{++_count} ({context.GetContextName()})";
            instance.InitializePoolObject(_poolId, _count, context);
            instance.OnDespawn();
            Instances.Add(instance);
            return instance;
        }

        public void RemovePoolInstance(IPoolObject poolObjectInstance)
        {
            ReferenceCount--;
            Instances.Remove(poolObjectInstance);
        }

        public IEnumerator WaitForPoolReady(Action<ObjectPoolData> onReady = null)
        {
            if (_poolType == typeof(AssetReferenceGameObject))
            {
                while (_prefab == null)
                {
                    _prefab = null;
                
                    // If we change the prefab, we need to wait for the new prefab to be resolved
                    if (_isResolved)
                    {
                        _isResolved = false;
                    
                        // Resolve the pool prefab and set it as the new prefab
                        var handle = new AssetReferenceGameObject(_assetReference.AssetGUID).LoadAssetAsync();
                        yield return handle;
                        if (handle.Status == AsyncOperationStatus.Succeeded)
                        {
                            var prefab = handle.Result;
                            var instance = prefab.GetComponent<IPoolObject>();
                            if (instance == null)
                            {
                                var defaultInstance = prefab.AddComponent<PoolObject>();
                                defaultInstance.hideFlags = HideFlags.HideAndDontSave;
                                instance = defaultInstance;
                            }

                            ResolvePoolPrefab(instance as PoolObject, _assetReference);
                        }
                        else
                        {
#if UNITY_EDITOR
                            var asset = new AssetReferenceGameObject(_assetReference.AssetGUID).editorAsset;
                            if (asset != null)
                            {
                                var prefab = asset;
                                var instance = prefab.GetComponent<IPoolObject>();
                                if (instance == null)
                                {
                                    var defaultInstance = prefab.AddComponent<PoolObject>();
                                    defaultInstance.hideFlags = HideFlags.HideAndDontSave;
                                    instance = defaultInstance;
                                }

                                ResolvePoolPrefab(instance as PoolObject, _assetReference);
                            }
                            else
                            {
                                Debug.LogError($"Failed to load asset {_assetReference}!");
                            }
#else
                            Debug.LogError($"Failed to load asset {_assetReference}!");
#endif
                        }
                    }
                
                    yield return null;
                }
            }
            else if (_poolType == typeof(AssetReferenceT<Material>))
            {
                while (_prefabAsset == null)
                {
                    _prefabAsset = null;
                    yield return ResolveAsset<Material>();
                }
            }
            else if (_poolType == typeof(AssetReferenceSprite))
            {
                while (_prefabAsset == null)
                {
                    _prefabAsset = null;
                    yield return ResolveAsset<Sprite>();
                }
            }
            else
            {
                while (_prefabAsset == null)
                {
                    _prefabAsset = null;
                    yield return ResolveAsset<Object>();
                }
            }
            
            onReady?.Invoke(this);
            yield break;

            IEnumerator ResolveAsset<T>() where T : Object
            {
                // If we change the prefab, we need to wait for the new prefab to be resolved
                if (_isResolved)
                {
                    _isResolved = false;
                    
                    // Resolve the pool prefab and set it as the new prefab
                    var handle = new AssetReferenceT<T>(_assetReference.AssetGUID).LoadAssetAsync<T>();
                    yield return handle;
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        ResolvePoolPrefabAsset(handle.Result, _assetReference);
                    }
                    else
                    {
#if UNITY_EDITOR
                        // Inside the editor we will try to get the editor asset
                        var editorObject = new AssetReferenceT<T>(_assetReference.AssetGUID).editorAsset;
                        if (editorObject != null)
                        {
                            ResolvePoolPrefabAsset(editorObject, _assetReference);
                        }
                        else
                        {
                            Debug.LogError($"Failed to load asset {_assetReference}!");
                        }
#else
                        Debug.LogError($"Failed to load asset {_assetReference}!");
#endif
                    }
                }
                yield return null;
            }
        }

        public void OnResolve(Action<ObjectPoolData> onResolve)
        {
            if (_prefab != null)
            {
                onResolve?.Invoke(this);
                return;
            }

            Routine.Start(WaitForPoolReady()).OnComplete(() => onResolve?.Invoke(this));
        }

        public void EnsurePoolInstances(IGameContext context, int spawnCount, bool allowDelete = false)
        {
            var count = Instances.Count(o => o.Context == context);
            
            if (count > spawnCount && allowDelete)
            {
                for (var i = 0; i < count - spawnCount; i++)
                {
                    ReferenceCount--;
                    var instance = Instances[i];
                    instance.OnDespawn();
                    Object.Destroy(instance.GetGameObject());
                    Instances.Remove(instance);
                    
                    // If we remove an instance, we need to update the count
                    count--;
                }
            }
            
            if (count >= spawnCount) return;
            
            if (_prefab == null)
            {
                Debug.LogError($"Instance for pool {_poolId} is not resolved!");
                return;
            }

            for (var i = 0; i < spawnCount - count; i++) AddPoolInstance(context);                
        }

        public void RemoveAllInstancesWithGameContext(IGameContext gameContext)
        {
            for (var i = Instances.Count - 1; i >= 0; i--)
            {
                var instance = Instances[i];
                if (instance != null && instance.Context == gameContext)
                {
                    ReferenceCount--;
                    instance.OnDespawn();
                    Object.Destroy(instance.GetGameObject());
                    Instances.RemoveAt(i);
                }
            }
        }
    }
}