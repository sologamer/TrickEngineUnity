#if UNITY_ADDRESSABLES
using System;
using System.Collections;
using System.Collections.Generic;
using TrickCore;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace TrickCore
{
    public sealed class ObjectPoolManager : MonoSingleton<ObjectPoolManager>
    {
        public Action<ITrickPool> PoolObjectCustomCreateAction { get; set; } = null;
        public Dictionary<TrickAssetGroupId, Dictionary<string, TrickObjectPoolAsync<ITrickPool>>> AssetReferencePoolDict { get; } = new Dictionary<TrickAssetGroupId, Dictionary<string, TrickObjectPoolAsync<ITrickPool>>>();

        protected override void Initialize()
        {
            AssetReferencePoolDict.Add(TrickAssetGroupId.ManualReleaseAssetGroupId, new Dictionary<string, TrickObjectPoolAsync<ITrickPool>>());
        }

        private void DoDestroy(ITrickPool arg0)
        {
            if (arg0 is MonoBehaviour beh && beh != null)
            {
                if (!AddressablesManager.Instance.ReleaseAsset(TrickAssetGroupId.ManualReleaseAssetGroupId, beh.gameObject))
                    Destroy(beh.gameObject);
            }
        }

        private static void Release(ITrickPool arg0)
        {
            arg0.IsClaimed = false;
            arg0.Release();
        }

        private static void Get(ITrickPool arg0)
        {
            arg0.IsClaimed = true;
            arg0.Claim();
        }

        public void ReleasePooledAssetReferences(TrickAssetGroupId group)
        {
            foreach (KeyValuePair<string,TrickObjectPoolAsync<ITrickPool>> pair in AssetReferencePoolDict[group])
            {
                pair.Value.SetSize(0);
            }
        }

        public IEnumerator GetAssetReferenceCoroutine<T2>(TrickAssetGroupId assetGroupId, AssetReferenceGameObject assetReference, Action<T2> callback) where T2 : class
        {
            if (!assetReference.IsValidReference())
            {
                callback?.Invoke(null);
                yield break;
            }
            yield return GetAssetReferencePool(assetGroupId, assetReference).GetCoroutineAs(callback);
        }

        public void GetAssetReferenceAsync<T2>(TrickAssetGroupId assetGroupId, AssetReferenceGameObject assetReference, Action<T2> callback) where T2 : class
        {
            if (!assetReference.IsValidReference())
            {
                callback?.Invoke(null);
            }
            GetAssetReferencePool(assetGroupId, assetReference).GetAsync(callback);
        }

        public TrickObjectPoolAsync<ITrickPool> GetAssetReferencePool(TrickAssetGroupId assetGroupId, AssetReferenceGameObject assetReference)
        {
            if (!assetReference.IsValidReference()) return default;
            if (!AssetReferencePoolDict[assetGroupId].TryGetValue(assetReference.AssetGUID, out var pool))
            {
                var tempGroup = assetGroupId;
                var tempAr = assetReference;
                IEnumerator Create(Action<ITrickPool> onCreateCallback)
                {
                    yield return AddressablesManager.Instance.InstantiateAssetCoroutine(assetGroupId, tempAr, o =>
                    {
                        if (o == null) return;
                        var go = o.GetComponent<ITrickPool>();
                        if (go == null)
                        {
                            var monoTrickPool = o.gameObject.AddComponent<DynamicMonoTrickPool>();
                            monoTrickPool.AssetReferenceGameObject = tempAr;
                            monoTrickPool.AssetGroupType = tempGroup;
                            PoolObjectCustomCreateAction?.Invoke(monoTrickPool);
                            onCreateCallback?.Invoke(monoTrickPool);
                        }
                        else
                        {
                            go.AssetReferenceGameObject = tempAr;
                            go.AssetGroupType = tempGroup;
                            PoolObjectCustomCreateAction?.Invoke(go);
                            onCreateCallback?.Invoke(go);
                        }
                    });
                }
            
                AssetReferencePoolDict[assetGroupId].Add(assetReference.AssetGUID, pool = new TrickObjectPoolAsync<ITrickPool>(Create, DoDestroy, Get, Release));
            }
            return pool;
        }

        public void TryReleaseObject(ITrickPool obj)
        {
            if (obj.IsClaimed) GetAssetReferencePool(obj.AssetGroupType, obj.AssetReferenceGameObject).Release(obj);
        }
    }

    public struct TrickAssetGroupId : IEquatable<TrickAssetGroupId>
    {
        public static readonly TrickAssetGroupId ManualReleaseAssetGroupId = new(int.MaxValue);

        public int Id { get; }

        public TrickAssetGroupId(int id)
        {
            Id = id;
        }

        public bool Equals(TrickAssetGroupId other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is TrickAssetGroupId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public static bool operator ==(TrickAssetGroupId left, TrickAssetGroupId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TrickAssetGroupId left, TrickAssetGroupId right)
        {
            return !left.Equals(right);
        }
    }
}
#endif