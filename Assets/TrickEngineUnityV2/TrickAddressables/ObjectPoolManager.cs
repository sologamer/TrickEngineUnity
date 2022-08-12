#if UNITY_ADDRESSABLES
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using TrickCore;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ObjectPoolManager : MonoSingleton<ObjectPoolManager>
{
    public Action<ITrickPool> PoolObjectCustomCreateAction { get; set; } = null;
    
    public Dictionary<AddressableGroupType, Dictionary<string, TrickObjectPoolAsync<ITrickPool>>>
        AssetReferencePoolDict { get; } =
        new Dictionary<AddressableGroupType, Dictionary<string, TrickObjectPoolAsync<ITrickPool>>>()
        {
            {AddressableGroupType.Lobby, new Dictionary<string, TrickObjectPoolAsync<ITrickPool>>()},
            {AddressableGroupType.Game, new Dictionary<string, TrickObjectPoolAsync<ITrickPool>>()},
            {AddressableGroupType.ReleaseOnDestroy, new Dictionary<string, TrickObjectPoolAsync<ITrickPool>>()},
        };


    #if UNITY_EDITOR
    [ShowInInspector]
    [ListDrawerSettings(Expanded = true)]
    public List<TrickPair<AssetReferenceGameObject, int>> PoolDebug(AddressableGroupType group)
    {
        var ret = AssetReferencePoolDict[group].Select(pair =>
            new TrickPair<AssetReferenceGameObject, int>(new AssetReferenceGameObject(pair.Key), pair.Value.PoolSize)).OrderByDescending(pair => pair.Value).ToList();

        string debugData = $"[{group}] Pool Groups:\n";
        debugData += string.Join("\n", ret.Select(pair => $"{pair.Key.editorAsset.name} x{pair.Value}"));

        var assetsObjects = AddressablesManager.Instance.GetAssetsObjects(group);
        debugData += $"\n[{group}] Asset Objects:\n";
        debugData += string.Join("\n", assetsObjects.Select(o => $"{(o != null ? o.name : "null")}").GroupBy(s => s.Replace("(Clone)", string.Empty)).Select(g => new
        {
            Name = g.Key,
            Count = g.Count()
        }).OrderByDescending(arg => arg.Count).Select(arg => $"{arg.Name} x{arg.Count}"));
        
        var assetsGameObjects = AddressablesManager.Instance.GetAssetsGameObjects(group);
        debugData += $"\n[{group}] Asset GameObjects:\n";
        debugData += string.Join("\n", assetsGameObjects.Select(o => $"{(o != null ? o.name : "null")}").GroupBy(s => s.Replace("(Clone)", string.Empty)).Select(g => new
        {
            Name = g.Key,
            Count = g.Count()
        }).OrderByDescending(arg => arg.Count).Select(arg => $"{arg.Name} x{arg.Count}"));
        
        Debug.Log(debugData);
        
        return ret;
    }
    #endif

    protected override void Initialize()
    {
    }

    private void DoDestroy(ITrickPool arg0)
    {
        if (arg0 is MonoBehaviour beh && beh != null)
        {
            if (!AddressablesManager.Instance.ReleaseAsset(AddressableGroupType.Game, beh.gameObject))
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

    public void ReleasePooledAssetReferences(AddressableGroupType group)
    {
        foreach (KeyValuePair<string,TrickObjectPoolAsync<ITrickPool>> pair in AssetReferencePoolDict[group])
        {
            pair.Value.SetSize(0);
        }
    }

    public IEnumerator GetAssetReferenceCoroutine<T2>(AddressableGroupType group, AssetReferenceGameObject assetReference, Action<T2> callback) where T2 : class
    {
        if (!assetReference.IsValidReference())
        {
            callback?.Invoke(null);
            yield break;
        }
        yield return GetAssetReferencePool(group, assetReference).GetCoroutineAs(callback);
    }

    public void GetAssetReferenceAsync<T2>(AddressableGroupType group, AssetReferenceGameObject assetReference, Action<T2> callback) where T2 : class
    {
        if (!assetReference.IsValidReference())
        {
            callback?.Invoke(null);
        }
        GetAssetReferencePool(group, assetReference).GetAsync(callback);
    }

    public TrickObjectPoolAsync<ITrickPool> GetAssetReferencePool(AddressableGroupType group, AssetReferenceGameObject assetReference)
    {
        if (!assetReference.IsValidReference()) return default;
        if (!AssetReferencePoolDict[group].TryGetValue(assetReference.AssetGUID, out var pool))
        {
            var tempGroup = group;
            var tempAr = assetReference;
            IEnumerator Create(Action<ITrickPool> onCreateCallback)
            {
                yield return AddressablesManager.Instance.InstantiateAssetCoroutine(group, tempAr, o =>
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
            
            AssetReferencePoolDict[group].Add(assetReference.AssetGUID, pool = new TrickObjectPoolAsync<ITrickPool>(Create, DoDestroy, Get, Release));
        }
        return pool;
    }

    public void TryReleaseObject(ITrickPool obj)
    {
        if (obj.IsClaimed) GetAssetReferencePool(obj.AssetGroupType, obj.AssetReferenceGameObject).Release(obj);
    }
}
#endif