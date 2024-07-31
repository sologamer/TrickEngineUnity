using System;
using System.Collections.Generic;
using TrickCore;
using UnityEngine;
using UnityEngine.AddressableAssets;

public static class AddressableExtensions
{
    public static bool AddressEquals(this AssetReference assetReference, AssetReference other)
    {
        return assetReference != null && other != null && assetReference.AssetGUID == other.AssetGUID;
    }

    public static void GetPoolAsset<T>(this AssetReferenceT<T> assetReference, Action<T> callback) where T : UnityEngine.Object
    {
        if (!assetReference.HasAddress()) return;
        ObjectPoolManager.RuntimeInstance.GetPoolDataAsset(assetReference, 1)
            .OnResolve(data => callback?.Invoke(data.GetAssetAs<T>()));
    }

    public static void GetRandomPoolAsset<T>(this List<AssetReferenceT<T>> list, IRandomizer randomizer, Action<T> callback) where T : UnityEngine.Object
    {
        if (list == null || list.Count == 0) return;
        list.Random(randomizer).GetPoolAsset(callback);
    }

    public static void GetRandomPoolEntity<T>(this List<AssetReferenceGameObject> list, IRandomizer randomizer, IGameContext context,
        Action<T> callback, Transform parent = null, Vector3? position = null, Quaternion? rotation = null) where T : Component
    {
        if (list == null || list.Count == 0) return;
        list.Random(randomizer).GetPoolEntity(context, callback, parent, position, rotation);
    }

    public static void GetPoolEntity<T>(this AssetReferenceGameObject assetReference, IGameContext context,
        Action<T> callback, Transform parent = null, Vector3? position = null, Quaternion? rotation = null)
        where T : Component
    {
        if (!assetReference.HasAddress()) return;
        ObjectPoolManager.RuntimeInstance.GetPoolDataEntity(assetReference, 1).OnResolve(data =>
        {
            if (position == null && rotation == null)
                callback?.Invoke(data.GetContextInstanceAs<T>(context, parent));
            else
                callback?.Invoke(data.GetContextInstanceAs<T>(context, parent, position.GetValueOrDefault(),
                    rotation.GetValueOrDefault()));
        });
    }

    public static bool HasAddress(this AssetReference ar)
    {
        return ar != null && !string.IsNullOrEmpty(ar.AssetGUID);
    }

    public static Vector3 GetTransformPosition(this IPoolObject obj)
    {
        return ((MonoBehaviour)obj).transform.position;
    }

    public static Transform GetTransform(this IPoolObject obj)
    {
        return ((MonoBehaviour)obj).transform;
    }

    public static Transform GetBoneTransform(this IPoolObject obj, HumanBodyBones bone)
    {
        return ((MonoBehaviour)obj).GetComponent<Animator>().GetBoneTransform(bone);
    }
}