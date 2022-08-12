#if UNITY_ADDRESSABLES
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BeauRoutine;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace TrickCore
{
    public static class AddressablesExtensions
    {
        public static bool IsValidReference(this AssetReference ar)
        {
            return ar != null && !string.IsNullOrEmpty(ar.AssetGUID);
        }
        
        public static IEnumerator TrickLoadAssetCoroutine<T>(this AssetReferenceT<T> ar, Action<T> callback) where T : Object
        {
            yield return TrickLoadAssetCoroutine(ar as AssetReference, callback);
        }
        
        public static IEnumerator TrickLoadAssetCoroutine<T>(this AssetReference ar, Action<T> callback) where T : Object
        {
            if (ar.IsValidReference())
            {
                yield return AddressablesManager.Instance.LoadAssetCoroutine<T>(AddressableGroupType.Game, ar,
                    asset => { callback?.Invoke(asset); });
            }
            else
            {
                callback?.Invoke(default);
            }
        }
        
        public static void TrickLoadAssetAsync<T>(this AssetReferenceT<T> ar, Action<T> callback) where T : Object
        {
            TrickLoadAssetAsync(ar as AssetReference, callback);
        }
        
        public static void TrickLoadAssetAsync<T>(this AssetReference ar, Action<T> callback) where T : Object
        {
            if (ar.IsValidReference())
            {
                AddressablesManager.Instance.LoadAssetAsync<T>(AddressableGroupType.Game, ar,
                    asset => { callback?.Invoke(asset); });
            }
            else
            {
                callback?.Invoke(default);
            }
        }

        public static IEnumerator TrickLoadAssetsCoroutine<T>(this IEnumerable<AssetReference> enumerable, Action<List<T>> callback) where T : Object
        {
            if (enumerable == null)
            {
                callback?.Invoke(new List<T>());
                yield break;
            }

            var list = enumerable.ToList();
            
            if (list.Count == 0)
            {
                callback?.Invoke(new List<T>());
                yield break;
            }

            List<T> result = new List<T>(list.Count);
            List<IEnumerator> enumerators = new List<IEnumerator>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                int i1 = i;
                var assetReference = list[i1];
                result.Add(default);
                enumerators.Add(AddressablesManager.Instance.LoadAssetCoroutine<T>(AddressableGroupType.Game, assetReference, asset =>
                {
                    result[i1] = asset;
                }));
            }
            yield return AddressablesManager.Instance.StartCoroutineAll(enumerators);
            callback?.Invoke(result);
        }
        
        public static IEnumerator TrickLoadAssetsCoroutine<T>(this IEnumerable<AssetReferenceT<T>> enumerable, Action<List<T>> callback) where T : Object
        {
            if (enumerable == null)
            {
                callback?.Invoke(new List<T>());
                yield break;
            }
            yield return TrickLoadAssetsCoroutine(enumerable.Cast<AssetReference>(), callback);
        }
    
        public static void SetSprite(this AssetReferenceSprite ar, Image instance, bool setActive = true)
        {
            if (instance == null) return;
            if (setActive) instance.gameObject.SetActive(false);

            var meta = instance.GetComponent<AddressableSpriteInfo>();
            if (meta != null)
            {
                if (ar.IsValidReference() && ar.AssetGUID == meta.LastAssetGUID)
                {
                    instance.sprite = meta.LoadedSprite;
                    if (setActive) instance.gameObject.SetActive(instance.sprite != null);
                    return;
                }
            }
            
            if (ar.IsValidReference())
            {
                if (ar.IsValid())
                {
                    /*IEnumerator LoadAsset()
                    {
                        bool mustRegister = false;
                        var op = ar.OperationHandle;
                        if (op.IsValid())
                        {
                            yield return op;

                            if (!op.IsValid() || op.Result == null)
                            {
                                var newOp = Addressables.LoadAssetAsync<Sprite>(ar.RuntimeKey);
                                yield return newOp;
                                op = newOp;
                            
                                mustRegister = true;
                            }
                        }
                        else
                        {
                            var newOp = Addressables.LoadAssetAsync<Sprite>(ar.RuntimeKey);
                            yield return newOp;
                            op = newOp;
                            
                            mustRegister = true;
                        }

                        if (op.IsValid() && op.Result is Sprite resultData)
                        {
                            if (instance == null)
                            {
                                if (resultData != null) Addressables.Release(resultData);
                                yield break;
                            }

                            if (mustRegister && resultData != null)
                            {
                                var helper = instance.GetComponent<AddressableAssetHelper>();
                                if (helper == null) helper = instance.gameObject.AddComponent<AddressableAssetHelper>();
                                helper.IsUsingManager = false;
                                helper.AddData(resultData);
                            }
                        
                            instance.sprite = resultData;
                            if (setActive) instance.gameObject.SetActive(instance.sprite != null);
                        }
                        else
                        {
                            if (instance == null) yield break;
                            instance.sprite = null;
                            if (setActive) instance.gameObject.SetActive(instance.sprite != null);
                        }
                    }
                    
                    Routine.Start(LoadAsset());*/
                    
                    bool mustRegister = false;
                    var op = ar.OperationHandle;
                    
                    void WaitForJobExecute2(AsyncOperationHandle handle)
                    {
                        op = handle;
                        mustRegister = true;
                        
                        if (op.IsValid() && op.Result is Sprite resultData)
                        {
                            if (instance == null)
                            {
                                if (resultData != null) Addressables.Release(resultData);
                                return;
                            }

                            if (mustRegister && resultData != null)
                            {
                                var helper = instance.GetComponent<AddressableAssetHelper>();
                                if (helper == null) helper = instance.gameObject.AddComponent<AddressableAssetHelper>();
                                helper.IsUsingManager = false;
                                helper.AddData(resultData);
                            }
                    
                            instance.sprite = resultData;
                            if (setActive) instance.gameObject.SetActive(instance.sprite != null);
                            
                            var spriteMeta = instance.GetComponent<AddressableSpriteInfo>();
                            if (spriteMeta == null) spriteMeta = instance.gameObject.AddComponent<AddressableSpriteInfo>();
                            spriteMeta.LastAssetGUID = ar.AssetGUID;
                            spriteMeta.LoadedSprite = resultData;
                        }
                        else
                        {
                            if (instance == null)
                                return;
                            instance.sprite = null;
                            if (setActive) instance.gameObject.SetActive(instance.sprite != null);
                        }
                    }
                    
                    if (op.IsValid())
                    {
                        void WaitForJobExecute(AsyncOperationHandle handle)
                        {
                            if (!op.IsValid() || op.Result == null)
                            {
                                var newOp = Addressables.LoadAssetAsync<Sprite>(ar.RuntimeKey);
                                newOp.CompletedTypeless += WaitForJobExecute2;
                            }
                            else if (op.Result is Sprite resultData)
                            {
                                if (instance == null) return;
                                instance.sprite = resultData;
                                if (setActive) instance.gameObject.SetActive(true);
                                
                                var spriteMeta = instance.GetComponent<AddressableSpriteInfo>();
                                if (spriteMeta == null) spriteMeta = instance.gameObject.AddComponent<AddressableSpriteInfo>();
                                spriteMeta.LastAssetGUID = ar.AssetGUID;
                                spriteMeta.LoadedSprite = resultData;
                            }
                        }
                        
                        if (!op.IsDone)
                        {
                            // wait until completion and execute
                            op.Completed += WaitForJobExecute;
                        }
                        else
                        {
                            // Already done, execute
                            WaitForJobExecute(op);
                        }
                    }
                    else
                    {
                        var newOp = Addressables.LoadAssetAsync<Sprite>(ar.RuntimeKey);
                        newOp.CompletedTypeless += WaitForJobExecute2;
                    }
                }
                else
                {
                    ar.LoadAssetAsync().Completed += handle =>
                    {
                        var resultData = handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;

                        if (instance == null)
                        {
                            if (resultData != null) Addressables.Release(resultData);
                            return;
                        }

                        if (resultData != null)
                        {
                            var helper = instance.GetComponent<AddressableAssetHelper>();
                            if (helper == null) helper = instance.gameObject.AddComponent<AddressableAssetHelper>();
                            helper.IsUsingManager = false;
                            helper.AddData(resultData);
                        }

                        instance.sprite = resultData;
                        if (setActive) instance.gameObject.SetActive(instance.sprite != null);
                        
                        var spriteMeta = instance.GetComponent<AddressableSpriteInfo>();
                        if (spriteMeta == null) spriteMeta = instance.gameObject.AddComponent<AddressableSpriteInfo>();
                        spriteMeta.LastAssetGUID = ar.AssetGUID;
                        spriteMeta.LoadedSprite = resultData;
                    };
                }
            }
            else
            {
                if (instance == null) return;
                instance.sprite = null;
                if (setActive) instance.gameObject.SetActive(instance.sprite != null);
            }
        }
    
        public static void SetAction(this AssetReferenceSprite ar, MonoBehaviour behaviour, Action<Sprite> action)
        {
            var meta = behaviour.GetComponent<AddressableSpriteInfo>();
            if (meta != null)
            {
                if (ar.IsValidReference() && ar.AssetGUID == meta.LastAssetGUID)
                {
                    action?.Invoke(meta.LoadedSprite);
                    return;
                }
            }

            if (ar.IsValidReference())
            {
                if (ar.IsValid())
                {
                    IEnumerator LoadAsset()
                    {
                        var mustRegister = false;
                        var op = ar.OperationHandle;
                        if (op.IsValid())
                        {
                            yield return op;
                            
                            if (!op.IsValid() || op.Result == null)
                            {
                                var newOp = Addressables.LoadAssetAsync<Sprite>(ar.RuntimeKey);
                                yield return newOp;
                                op = newOp;
                                
                                mustRegister = true;
                            }
                        }
                        else
                        {
                            var newOp = Addressables.LoadAssetAsync<Sprite>(ar.RuntimeKey);
                            yield return newOp;
                            op = newOp;
                                
                            mustRegister = true;
                        }

                        if (op.IsValid() && op.Result is Sprite resultData)
                        {
                            if (behaviour != null)
                            {
                                if (mustRegister)
                                {
                                    var helper = behaviour.GetComponent<AddressableAssetHelper>();
                                    if (helper == null) helper = behaviour.gameObject.AddComponent<AddressableAssetHelper>();
                                    helper.IsUsingManager = false;
                                    helper.AddData(resultData);
                                }

                                var spriteMeta = behaviour.GetComponent<AddressableSpriteInfo>();
                                if (spriteMeta == null) spriteMeta = behaviour.gameObject.AddComponent<AddressableSpriteInfo>();
                                spriteMeta.LastAssetGUID = ar.AssetGUID;
                                spriteMeta.LoadedSprite = resultData;
                            }
                            
                            action?.Invoke(resultData);
                        }
                        else
                        {
                            action?.Invoke(null);
                        }
                    }
                    
                    Routine.Start(LoadAsset());
                }
                else
                {
                    ar.LoadAssetAsync().Completed += handle =>
                    {
                        var resultData = handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;

                        if (behaviour != null)
                        {
                            var helper = behaviour.GetComponent<AddressableAssetHelper>();
                            if (helper == null) helper = behaviour.gameObject.AddComponent<AddressableAssetHelper>();
                            helper.IsUsingManager = false;
                            helper.AddData(resultData);
                            
                            var spriteMeta = behaviour.GetComponent<AddressableSpriteInfo>();
                            if (spriteMeta == null) spriteMeta = behaviour.gameObject.AddComponent<AddressableSpriteInfo>();
                            spriteMeta.LastAssetGUID = ar.AssetGUID;
                            spriteMeta.LoadedSprite = resultData;
                        }

                        action?.Invoke(resultData);
                    };
                }
            }
            else
            {
                action?.Invoke(null);
            }
        }
    }
}
#endif