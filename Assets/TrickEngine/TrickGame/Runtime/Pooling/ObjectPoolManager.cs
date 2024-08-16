using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace TrickCore
{
    public class ObjectPoolManager : MonoSingleton<ObjectPoolManager>
    {
        [SerializeField] public AssetReference DefaultSpriteAssetReference;
        [SerializeField] public HideFlags ActiveInstancesHideFlags = HideFlags.DontSave;
        [SerializeField] public HideFlags PoolHideFlags = HideFlags.HideAndDontSave;
        private readonly Dictionary<string, ObjectPoolData> _objectPools = new();

        public Transform PoolTransform { get; set; }
        public Transform ActiveInstancesParent { get; set; }

        protected override void Initialize()
        {
            base.Initialize();

            PoolTransform = transform;
            if (ActiveInstancesParent == null)
            {
                ActiveInstancesParent = new GameObject("Active Instances").transform;
                ActiveInstancesParent.SetParent(transform);
                ActiveInstancesParent.gameObject.hideFlags = ActiveInstancesHideFlags;
            }
        }

        public ObjectPoolData GetPoolDataSprite(AssetReferenceSprite assetReference, int initialCapacity = 1)
        {
            return GetPoolDataObject(assetReference, true, initialCapacity);
        }
        public ObjectPoolData GetPoolDataEntity(AssetReferenceGameObject assetReference, int initialCapacity = 1)
        {
            return GetPoolDataObject(assetReference, false, initialCapacity);
        }
        public ObjectPoolData GetPoolDataAsset(AssetReference assetReference, int initialCapacity = 1)
        {
            return GetPoolDataObject(assetReference, false, initialCapacity);
        }

        private ObjectPoolData GetPoolDataObject(AssetReference assetReference, bool isSprite = false, int initialCapacity = 1)
        {
            if (!assetReference.HasAddress())
            {
                // We default to the default sprite
                if (isSprite)
                {
                    assetReference = DefaultSpriteAssetReference;
                }
                else
                {
                    Debug.LogError($"Asset reference {assetReference} is not valid!");
                    return null;
                }
            }

            var poolId = assetReference.AssetGUID;

            if (_objectPools.TryGetValue(poolId, out var poolData))
            {
                return poolData;
            }

            poolData = new ObjectPoolData(poolId, initialCapacity, assetReference.GetType());

            if (assetReference is AssetReferenceGameObject or AssetReferenceT<GameObject>)
            {
                if (assetReference.OperationHandle.IsValid() && assetReference.OperationHandle.Status != AsyncOperationStatus.None) assetReference = new AssetReferenceGameObject(assetReference.AssetGUID);
                AsyncOperationHandle<GameObject> op = assetReference.LoadAssetAsync<GameObject>();
                op.Completed += CompletedGameObject;
                _objectPools.Add(poolId, poolData);
                return _objectPools[poolId];
                
                void CompletedGameObject(AsyncOperationHandle<GameObject> handle)
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        // Resolve the pool prefab
                        // Add the PoolObjectInstance component to the prefab, but makes sure that it's not saved to the prefab
                        var prefab = handle.Result;
                        var instance = prefab.GetComponent<IPoolObject>();
                        if (instance == null)
                        {
                            var defaultInstance = prefab.AddComponent<PoolObject>();
                            defaultInstance.hideFlags = HideFlags.HideAndDontSave;
                            instance = defaultInstance;
                        }

                        poolData.ResolvePoolPrefab(instance as PoolObject, assetReference);
                    }
                    else
                    {
#if UNITY_EDITOR
                        var asset = assetReference.editorAsset as GameObject;
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

                            poolData.ResolvePoolPrefab(instance as PoolObject, assetReference);
                        }
                        else
                        {
                            Debug.LogError($"Failed to load asset {assetReference}!");
                        }
#else
                        Debug.LogError($"Failed to load asset {assetReference}!");
#endif
                        
                    }
                
                    op.Completed -= CompletedGameObject;
                }
            }

            if (assetReference is AssetReferenceT<Material>)
            {
                return TryLoad<Material>();
            }
            if (assetReference is AssetReferenceT<Sprite>)
            {
                return TryLoad<Sprite>();
            }

            return TryLoad<Object>();

            ObjectPoolData TryLoad<T>()
            {
                if (assetReference.OperationHandle.IsValid() && assetReference.OperationHandle.Status != AsyncOperationStatus.None) assetReference = new AssetReference(assetReference.AssetGUID);
                AsyncOperationHandle<T> op = assetReference.LoadAssetAsync<T>();
                op.Completed += CompletedObject;
                _objectPools.Add(poolId, poolData);
                return _objectPools[poolId];
                
                void CompletedObject<T2>(AsyncOperationHandle<T2> handle)
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        // Resolve the pool prefab
                        // Add the PoolObjectInstance component to the prefab, but makes sure that it's not saved to the prefab
                        poolData.ResolvePoolPrefabAsset(handle.Result as Object, assetReference);
                    }
                    else
                    {
#if UNITY_EDITOR
                        var asset = assetReference.editorAsset;
                        if (asset != null)
                        {
                            poolData.ResolvePoolPrefabAsset(asset, assetReference);
                        }
                        else
                        {
                            Debug.LogError($"Failed to load asset {assetReference}!");
                        }
#else
                        Debug.LogError($"Failed to load asset {assetReference}!");
#endif
                    }
                
                    op.Completed -= CompletedObject;
                }
            }
        }

        public ObjectPoolData GetPoolDataByPoolId(string poolId, IGameContext context, int initialCapacity = 1, Type poolType = null)
        {
            if (_objectPools.TryGetValue(poolId, out var poolData))
                return poolData;

            poolData = new ObjectPoolData(poolId, initialCapacity, poolType);
            _objectPools.Add(poolId, poolData);
            return _objectPools[poolId];
        }

        public void SendInstanceToPool(IPoolObject poolObjectInstance)
        {
            if (poolObjectInstance == null || poolObjectInstance.PoolId == null)
            {
                Debug.LogError($"Pool object instance {poolObjectInstance} is not valid!");
                return;
            }
            
            if (!_objectPools.TryGetValue(poolObjectInstance.PoolId, out var poolData))
            {
                Debug.LogError($"Pool with id {poolObjectInstance.PoolId} does not exist!");
                return;
            }
            
            if (poolObjectInstance.IsInPool)
                return;

            poolData.SendToPool(poolObjectInstance);
        }

        public Transform GetPoolParent(string poolId)
        {
            if (!_objectPools.TryGetValue(poolId, out var poolData))
            {
                Debug.LogError($"Pool with id {poolId} does not exist!");
                return null;
            }

            return poolData.PoolParent;
        }

        public void RemoveAllInstancesWithGameContext(IGameContext gameContext)
        {
            foreach (ObjectPoolData poolData in _objectPools.Values)
            {
                poolData.RemoveAllInstancesWithGameContext(gameContext);
            }
        }
    }
}