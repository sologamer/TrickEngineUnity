#if UNITY_ADDRESSABLES
#define EDITOR_QUICK_LOAD
#define EDITOR_QUICK_INSTANTIATE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BeauRoutine;
using TrickCore;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using WaitUntil = UnityEngine.WaitUntil;

namespace TrickCore
{
    /// <summary>
    /// TODO: Refactor the AddressablesManager, support usage of Tasks
    /// </summary>
    public class AddressablesManager : MonoSingleton<AddressablesManager>
    {
        private readonly Dictionary<TrickAssetGroupId, HashSet<GameObject>> _assets =
            new()
            {
                {TrickAssetGroupId.ManualReleaseAssetGroupId, new HashSet<GameObject>()},
            };
    
        private readonly Dictionary<TrickAssetGroupId, HashSet<Object>> _assetObjects =
            new()
            {
                {TrickAssetGroupId.ManualReleaseAssetGroupId, new HashSet<Object>()},
            };
    
        private static Dictionary<string, AssetReferenceGameObject> CachedArGoes { get; } = new();
    
        private readonly List<Object> _dontDestroyAssets = new();
        private string _addressablesVersion;
        private readonly List<Action<bool>> _localInvokeQueue = new();
        private readonly List<Action<bool>> _remoteInvokeQueue = new();

        public Action<object> ConcurrentDownloadStartFunc { get; set; } = null;
        public Action<object, DownloadStatus> ConcurrentDownloadUpdateFunc { get; set; } = null;
        public Action<object> ConcurrentDownloadCompleteFunc { get; set; } = null;
        public Action<List<IResourceLocation>, Dictionary<string, long>> InitializeAddressableDownloadFunc { get; set; } = null;
        public Action<List<IResourceLocation>, Dictionary<string, long>> InitializeAddressableDownloadFuncFast { get; set; } = null;
        public Action EndAddressableDownloadFunc { get; set; } = null;

        private const int CONCURRENT_STEPS = 100;
    
        public bool LocalInitialized { get; set; }
        public bool RemoteInitialized { get; set; }

        public int NumInstantiatingAssets { get; set; }
        public int NumLoadingAssets { get; set; }

#if UNITY_EDITOR

        public List<GameObject> GetAssetsGameObjects(TrickAssetGroupId group)
        {
            return _assets[group].ToList();
        }
    
        public List<Object> GetAssetsObjects(TrickAssetGroupId group)
        {
            return _assetObjects[group].ToList();
        }
    
#endif

#if UNITY_ADDRESSABLES && ENABLE_ADDRESSABLESMANAGER

    protected override void Initialize()
    {
        base.Initialize();
        Routine.Start(FastInitializeAddressables());
    }

    private IEnumerator FastInitializeAddressables()
    {
        AsyncResultData loadAddressables = new AsyncResultData();
        yield return LoadAddressables(false, loadAddressables);
        if (!loadAddressables.GetValueOrDefault())
        {
            Debug.Log("[FastInitializeAddressables] Addressables failed to initialize!");
            yield break;
        }
        Debug.Log("[FastInitializeAddressables] Addressables initialized!");
    }
#endif

        public bool ReleaseAsset(TrickAssetGroupId group, Object obj)
        {
            if (_dontDestroyAssets.Contains(obj))
            {
                Debug.LogError($"[ReleaseAsset] Addressable asset {obj} is in the _dontDestroyAssets list");
                return false;
            }

            if (obj is GameObject go)
            {
                if (_assets[group].Remove(go))
                {
                    // Disable auto-release, since we release it here
                    if (group == TrickAssetGroupId.ManualReleaseAssetGroupId)
                    {
                        var helper = go.GetComponent<AddressableAssetHelper>();
                        if (helper != null) helper.enabled = false;
                    }
                
                    Addressables.ReleaseInstance(go);
                    if (go != null) Destroy(go);
                    return true;
                }
            }
            else
            {
                if (_assetObjects[group].Remove(obj))
                {
                    Addressables.Release(obj);
                    return true;
                }
            }
       
        
            return false;
        }
    
        public void ReleaseAllInstances(TrickAssetGroupId group)
        {
            _assets[group].Except(_dontDestroyAssets.OfType<GameObject>()).Where(o => o != null).ToList()
                .ForEach(o => Addressables.ReleaseInstance(o));
            _assetObjects[group].Except(_dontDestroyAssets).Where(o => o != null).ToList().ForEach(Addressables.Release);
        
            _assetObjects[group].Clear();
            _assets[group].Clear();
        }

        public void DontDestroyAsset(TrickAssetGroupId group, Object o)
        {
            if (o == null) return;
            _dontDestroyAssets.Add(o);
            if (o is GameObject go)
                _assets[group].Remove(go);
            else
                _assetObjects[group].Remove(o);
        }

        public void RemoveDontDestroyAsset(TrickAssetGroupId group, Object o)
        {
            if (o == null) return;
            if (!_dontDestroyAssets.Remove(o)) return;
            if (o is GameObject go)
                _assets[@group].Add(go);
            else
                _assetObjects[@group].Add(o);
        }

        public void InstantiateAssetAsync(TrickAssetGroupId group, AssetReference ar, Vector3 position, Quaternion quaternion,
            Transform parent, Action<GameObject> callback)
        {
            if (!ar.IsValidReference())
            {
                Debug.LogError("Not a valid reference");
                callback?.Invoke(null);
                return;
            }
        
            if (!(ar is AssetReferenceGameObject arGo))
            {
                if (!CachedArGoes.TryGetValue(ar.AssetGUID, out arGo))
                    CachedArGoes.AddOrReplace(ar.AssetGUID, arGo = new AssetReferenceGameObject(ar.AssetGUID));
            }

            void OnCompleted(AsyncOperationHandle<GameObject> handle)
            {
                HandleGameObjectResult(group, ar, handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null, callback);
            }

#if UNITY_EDITOR && EDITOR_QUICK_INSTANTIATE
            if (ar.editorAsset is GameObject go)
            {
                HandleGameObjectResult(group, ar, Instantiate(go, position, quaternion, parent), callback);
            }
            else
            {
                var op = ar.InstantiateAsync(position, quaternion, parent);
                if (op.Status == AsyncOperationStatus.Succeeded) OnCompleted(op);
                else op.Completed += OnCompleted;
            }
#else
        var op = ar.InstantiateAsync(position, quaternion, parent);
        if (op.Status == AsyncOperationStatus.Succeeded) OnCompleted(op);
        else op.Completed += OnCompleted;
#endif
        }

        public void InstantiateAssetAsync(TrickAssetGroupId group, AssetReference ar, Transform parent, Action<GameObject> callback)
        {
            if (!ar.IsValidReference())
            {
                Debug.LogError("Not a valid reference");
                callback?.Invoke(null);
                return;
            }

            NumInstantiatingAssets++;

            void OnCompleted(AsyncOperationHandle<GameObject> handle)
            {
                HandleGameObjectResult(group, ar, handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null, callback);
            }

#if UNITY_EDITOR && EDITOR_QUICK_INSTANTIATE
            if (ar.editorAsset is GameObject go)
            {
                HandleGameObjectResult(group, ar, Instantiate(go, parent), callback);
            }
            else
            {
                var op = ar.InstantiateAsync(parent);
                if (op.Status == AsyncOperationStatus.Succeeded) OnCompleted(op);
                else op.Completed += OnCompleted;
            }
#else
        var op = ar.InstantiateAsync(parent);
        if (op.Status == AsyncOperationStatus.Succeeded) OnCompleted(op);
        else op.Completed += OnCompleted;
#endif
        }
    
        private void HandleGameObjectResult(TrickAssetGroupId group, AssetReference ar, GameObject handleResult, Action<GameObject> callback)
        {
            NumInstantiatingAssets--;
            if (handleResult != null)
            {
                _assets[group].Add(handleResult);
                
                if (handleResult.TryGetComponent<ITrickPool>(out var poolObject))
                {
                    poolObject.AssetGroupType = group;
                        
                    if (!(ar is AssetReferenceGameObject arGo))
                    {
                        if (!CachedArGoes.TryGetValue(ar.AssetGUID, out arGo))
                            CachedArGoes.AddOrReplace(ar.AssetGUID, arGo = new AssetReferenceGameObject(ar.AssetGUID));
                    }
                        
                    poolObject.AssetReferenceGameObject = arGo;

                    poolObject.OnInstantiated();
                }

                if (@group == TrickAssetGroupId.ManualReleaseAssetGroupId)
                {
                    if (!handleResult.TryGetComponent<AddressableAssetHelper>(out var helper))
                        helper = handleResult.AddComponent<AddressableAssetHelper>();
                    helper.IsUsingManager = true;
                    helper.AddData(handleResult);
                }
            }
            
            callback?.Invoke(handleResult);
        }

        public IEnumerator InstantiateAssetCoroutine(TrickAssetGroupId group, AssetReferenceGameObject value, Action<GameObject> callback)
        {
            yield return Routine.Inline(InstantiateAssetCoroutine(group, value, null, callback));
        }
    
        public IEnumerator InstantiateAssetCoroutine(TrickAssetGroupId group, AssetReferenceGameObject value, Transform parent, Action<GameObject> callback)
        {
            if (!value.IsValidReference())
            {
                Debug.LogError($"Not a valid reference - {value?.AssetGUID}");
                callback?.Invoke(null);
                yield break;
            }

            bool loaded = false;
            GameObject go = null;
            InstantiateAssetAsync(group, value, parent, o =>
            {
                go = o;
                loaded = true;
            });

            bool InCondition() => loaded;

            if (!InCondition()) yield return new WaitUntil(InCondition);
            callback?.Invoke(go);
        }

        public void LoadAssetAsync<T>(TrickAssetGroupId group, AssetReference value, Action<T> callback)
            where T : Object
        {
            if (!value.IsValidReference())
            {
                Debug.LogError("Not a valid reference");
                callback?.Invoke(null);
                return;
            }

            bool loaded = false;
            if (value.IsValid())
            {
                bool mustRegister = false;
                var op = value.OperationHandle;
                if (!op.IsValid() || op.Result == null)
                {
                    var newOp = Addressables.LoadAssetAsync<T>(value.RuntimeKey);
                    mustRegister = true;
                    NumLoadingAssets++;
                    void OnNewOpOnCompleted(AsyncOperationHandle<T> handle)
                    {
                        NumLoadingAssets--;
                        HandleOperation(handle);
                    }

                    if (newOp.Status == AsyncOperationStatus.Succeeded) OnNewOpOnCompleted(newOp);
                    else newOp.Completed += OnNewOpOnCompleted;
                }
                else
                {
                    HandleOperation(op);
                }
            
                void HandleOperation(AsyncOperationHandle asyncOperationHandle)
                {
                    if (asyncOperationHandle.Status == AsyncOperationStatus.Failed)
                    {
                        if (asyncOperationHandle.OperationException != null)
                        {
                            Debug.LogError(asyncOperationHandle.OperationException.ToString());
                        }
                    }

                    if (!asyncOperationHandle.IsValid()) return;
                    if (asyncOperationHandle.Result != null && typeof(T) == typeof(GameObject) &&
                        asyncOperationHandle.Result is GameObject go)
                    {
                        if (go.TryGetComponent<ITrickPool>(out var poolObject))
                        {
                            poolObject.AssetGroupType = group;
                            
                            if (value is not AssetReferenceGameObject arGo)
                            {
                                if (!CachedArGoes.TryGetValue(value.AssetGUID, out arGo))
                                    CachedArGoes.AddOrReplace(value.AssetGUID, arGo = new AssetReferenceGameObject(value.AssetGUID));
                            }

                            poolObject.AssetReferenceGameObject = arGo;
                        }
                    }

                    if (mustRegister) _assetObjects[group].Add(asyncOperationHandle.Result as T);
                    callback?.Invoke(asyncOperationHandle.Result as T);
                }
            }
            else
            {
                NumLoadingAssets++;

                void OnCompleted(AsyncOperationHandle<T> handle)
                {
                    if (handle.Status == AsyncOperationStatus.Failed)
                    {
                        if (handle.OperationException != null)
                        {
                            Debug.LogError(handle.OperationException.ToString());
                        }
                    }
                    HandleLoadResult(@group, value,
                        handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : default, ref loaded, callback);
                }

#if UNITY_EDITOR && EDITOR_QUICK_LOAD
                if (value.editorAsset is T tObject)
                {
                    HandleLoadResult(@group, value, tObject, ref loaded, callback);
                }
                else
                {
                    var op = value.LoadAssetAsync<T>();
                    if (op.Status == AsyncOperationStatus.Succeeded) OnCompleted(op);
                    else op.Completed += OnCompleted;
                }
#else
            var op = value.LoadAssetAsync<T>();
            if (op.Status == AsyncOperationStatus.Succeeded) OnCompleted(op);
            else op.Completed += OnCompleted;
#endif

            }
        }

        public IEnumerator LoadAssetCoroutine<T>(TrickAssetGroupId group, AssetReference value, Action<T> callback) where T : Object
        {
            if (!value.IsValidReference())
            {
                Debug.LogError("Not a valid reference");
                callback?.Invoke(null);
                yield break;
            }
        
            T asset = default;

            bool loaded = false;
            LoadAssetAsync<T>(group, value, o =>
            {
                asset = o;
                loaded = true;
            });
            bool InCondition() => loaded;
            if (!InCondition()) yield return new WaitUntil(InCondition);
            callback?.Invoke(asset);
        }
    
        private void HandleLoadResult<T>(TrickAssetGroupId group, AssetReference value, T handleResult, ref bool loaded, Action<T> callback) where T : Object
        {
            NumLoadingAssets--;

            if (handleResult != null && typeof(T) == typeof(GameObject) && handleResult is GameObject go)
            {
                var poolObject = go.GetComponent<ITrickPool>();
                if (poolObject != null)
                {
                    poolObject.AssetGroupType = @group;

                    if (value is not AssetReferenceGameObject arGo)
                    {
                        if (!CachedArGoes.TryGetValue(value.AssetGUID, out arGo)) CachedArGoes.AddOrReplace(value.AssetGUID, arGo = new AssetReferenceGameObject(value.AssetGUID));
                    }

                    poolObject.AssetReferenceGameObject = arGo;
                }
            }

            if (handleResult != null) _assetObjects[@group].Add(handleResult);
        
            callback?.Invoke(handleResult);
            loaded = true;
        }

        public IEnumerator InstantiateAssetCoroutine(TrickAssetGroupId group, AssetReferenceGameObject value, Vector3 position,
            Quaternion quaternion,
            Transform parent, Action<GameObject> callback)
        {
            if (!value.IsValidReference())
            {
                Debug.LogError("Not a valid reference");
                callback?.Invoke(null);
                yield break;
            }
        
            bool loaded = false;
            GameObject go = null;
            InstantiateAssetAsync(group, value, position, quaternion, parent, o =>
            {
                go = o;
                loaded = true;
            });

            bool InCondition() => loaded;

            if (!InCondition()) yield return new WaitUntil(InCondition);
            callback?.Invoke(go);
        }
    
        public IEnumerator InitializeAddressables(AsyncResultData result)
        {
            result.Result = null;
        
            bool? addressableInitializeResult = null;
            void Completed(AsyncOperationHandle<IResourceLocator> handle)
            {
                Debug.Log("Addressables Initialize Status " + handle.Status);
                result.Result = handle.Status == AsyncOperationStatus.Succeeded;
                addressableInitializeResult = handle.Status == AsyncOperationStatus.Succeeded;
            }

            Debug.Log("Trying to initialize Addressables");
            try
            {
                Addressables.InitializeAsync().Completed += Completed;
            }
            catch (Exception)
            {
                // Redo it one more time
                Debug.Log("[Failed] Retrying to initialize Addressables");
                Addressables.InitializeAsync().Completed += Completed;
            }
            Debug.Log("Waiting for addressables initialize to complete");

            bool InCondition() => addressableInitializeResult != null && result.Result != null;

            if (!InCondition()) yield return new WaitUntil(InCondition);
            Debug.Log("Addressables Initialize Result: " + result.Result.GetValueOrDefault());
        }

        public IEnumerator GetDownloadSize(AsyncResultData result, object key)
        {
            result.Result = null;
        
            void Completed(AsyncOperationHandle<long> handle)
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    result.Data = new KeyValuePair<object, long>(key, handle.Result);
                    result.Result = true;
                }
                else
                {
                    result.Data = null;
                    result.Result = false;
                }
                Addressables.Release(handle);
            }

            AsyncOperationHandle<long> op;
            if (key is string s)
            {
                op = Addressables.GetDownloadSizeAsync(s);
            }
            else if (key is IEnumerable keys)
            {
                op = Addressables.GetDownloadSizeAsync(keys);
            }
            else
            {
                op = Addressables.GetDownloadSizeAsync(key);
            }
            op.Completed += Completed;

            bool InCondition() => result.Result != null;
            if (!InCondition()) yield return new WaitUntil(InCondition);
        }

        public IEnumerator DownloadDependenciesAsync(AsyncResultData result, object key, Action<object> downloadStart, Action<object, DownloadStatus> downloadUpdate, Action<object> downloadComplete)
        {
            result.Result = null;
        
            void Completed(AsyncOperationHandle handle)
            {
                result.Result = handle.Status == AsyncOperationStatus.Succeeded;
                Addressables.Release(handle);
            }

            downloadStart?.Invoke(key);
            AsyncOperationHandle downloadOp;

            if (key is IList<IResourceLocation> locations)
                downloadOp = Addressables.DownloadDependenciesAsync(locations, true);
            else
                downloadOp = Addressables.DownloadDependenciesAsync(key, true);

            var routine = Routine.Start(PercentageWatcher(key, downloadOp, downloadUpdate));
            downloadOp.Completed += Completed;
            bool InCondition() => result.Result != null;
            if (!InCondition()) yield return new WaitUntil(InCondition);
            downloadComplete?.Invoke(key);
            routine.Stop();
        }

        private IEnumerator PercentageWatcher(object key, AsyncOperationHandle op, Action<object, DownloadStatus> downloadUpdate)
        {
            while (!op.IsDone)
            {
                downloadUpdate?.Invoke(key, op.GetDownloadStatus());
                yield return null;
            }
        }

        public IEnumerator CheckUpdateCatalog(AsyncResultData result)
        {
            void Completed(AsyncOperationHandle<List<string>> operationHandle)
            {
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log($"Catalogs to update: {string.Join(",", operationHandle.Result)}");

                    List<object> officialKeys = Addressables.ResourceLocators.SelectMany(locator => locator.Keys)
                        .ToList();

                    if (operationHandle.Result.Count > 0)
                    {
                        void UpdateCatalogsCompleted(AsyncOperationHandle<List<IResourceLocator>> asyncOperationHandle)
                        {
                            if (asyncOperationHandle.Status == AsyncOperationStatus.Succeeded)
                            {
                                Debug.Log("Updated catalogs (" + asyncOperationHandle.Result.Count + ")");
                                result.Result = true;
                                List<object> keys = asyncOperationHandle.Result.SelectMany(locator => locator.Keys).ToList();
                                result.Data = keys;
                            
                                Debug.Log($"Downloaded Locators: " + keys.Count);
                                Debug.Log($"Total Resource locators count: " + officialKeys.Count);
                            }
                            else
                            {
                                result.Result = false;
                            }
                        }

                        Addressables.UpdateCatalogs(operationHandle.Result).Completed += UpdateCatalogsCompleted;
                    }
                    else
                    {
                        result.Result = true;   
                        result.Data = officialKeys;
                    
                        Debug.Log($"[Catalog-UpToDate] Total Resource locators count: " + officialKeys.Count);
                    }
                }
                else
                {
                    result.Result = false;
                }
            }

            Addressables.CheckForCatalogUpdates().Completed += Completed;
            bool InCondition() => result.Result != null;
            if (!InCondition()) yield return new WaitUntil(InCondition);
        }

#if UNITY_ADDRESSABLES && ENABLE_ADDRESSABLESMANAGER
    public IEnumerator LoadAddressables(bool withRemote, AsyncResultData result, Action startDownloadCallback = null)
    {
        if (!withRemote)
        {
            using (new TrickStepTimeLog($"[LoadAddressables] Initialize (withRemote: {withRemote})"))
            {
                AsyncResultData initializeAddressables = new AsyncResultData();
                yield return InitializeAddressables(initializeAddressables);
                if (!initializeAddressables.GetValueOrDefault())
                {
                    result.Result = false;
                    yield break;
                }
            }
        }

        if (withRemote)
        {
            using (new TrickStepTimeLog($"[LoadAddressables] Release instances"))
            {
                yield return null;
                ReleaseAllInstances(AddressableGroupType.Lobby);
                ReleaseAllInstances(AddressableGroupType.Game);
            }
            
            // Check for a catalog update
            using (new TrickStepTimeLog("[Local] LoadAddressables"))
            {
                AsyncResultData checkCatalogUpdates = new AsyncResultData();
                yield return CheckUpdateCatalog(checkCatalogUpdates);
                if (!checkCatalogUpdates.GetValueOrDefault())
                {
                    result.Result = false;
                }
            }

            using (new TrickStepTimeLog("[Remote] LoadAddressables"))
                yield return RemoteLoadAddressablesFast(result, startDownloadCallback);
        }
        
        if (result.Result is false)
            yield break;

        result.Result = true;
        
        // Invoke Adddressables complete
        if (result.GetValueOrDefault())
        {
            // Addressables completed successfully, run Initialized!

            if (withRemote)
            {
                if (!RemoteInitialized)
                {
                    _remoteInvokeQueue.ForEach(action => action?.Invoke(true));
                    _remoteInvokeQueue.Clear();

                    RemoteInitialized = true;
                }
            }
            else
            {
                if (!LocalInitialized)
                {
                    _localInvokeQueue.ForEach(action => action?.Invoke(false));
                    _localInvokeQueue.Clear();

                    LocalInitialized = true;
                }
            }
        }
    }

    private IEnumerator RemoteLoadAddressables(AsyncResultData result)
    {
        var locations = Application.isEditor
            ? new List<object>()
            : Addressables.ResourceLocators.FirstOrDefault()?.Keys.ToList() ?? new List<object>();

        var resourceLocationResult = new AsyncResultData();
        yield return GetAllResourceLocations(resourceLocationResult, locations);
        
        if (!(resourceLocationResult.Data is List<IResourceLocation> resourceLocations))
        {
            resourceLocations = new List<IResourceLocation>();
        }
        else
        {
            //Debug.Log(string.Join("\n", resourceLocations.Select(location => $"InternalId {location.InternalId}, PrimaryKey {location.PrimaryKey}, ProviderId {location.ProviderId}, ResourceType {location.ResourceType}, DependencyHashCode {location.DependencyHashCode}")));
        }
        
        long localSize = 0;
        var depsHashes = new List<int>();
        var keysToFetch = new List<IResourceLocation>();
        var keysToDownload = new List<IResourceLocation>();
        // foreach (var key in assetsToDownload)
        
        IEnumerator ConcurrentGetDownloadSize(int cIndex)
        {
            var key = resourceLocations[cIndex];
            AsyncResultData getDownloadSizeSucceeded = new AsyncResultData();
            yield return GetDownloadSize(getDownloadSizeSucceeded, key);
            if (!getDownloadSizeSucceeded.GetValueOrDefault())
            {
                result.Result = false;
                yield break;
            }

            if (getDownloadSizeSucceeded.Data is KeyValuePair<object, long> pair)
            {
                if (pair.Value > 0)
                {
                    if (!depsHashes.Contains(key.DependencyHashCode))
                    {
                        foreach (IResourceLocation dependency in key.Dependencies)
                        {
                            if (!keysToFetch.Contains(dependency))
                            {
                                keysToFetch.Add(dependency);
                            }
                        }
                            
                        depsHashes.Add(key.DependencyHashCode);
                    }

                    if (!keysToDownload.Contains(key)) keysToDownload.Add(key);
                        
                    localSize += pair.Value;
                }
            }
        }
        yield return this.StartMultiCoroutineConcurrent(resourceLocations.Count, GameManager.Instance.RoutineAllocationCapacity / 2, ConcurrentGetDownloadSize);
        
        List<string> downloadList = keysToFetch.Where(location => location.InternalId.Contains("https://")).Select(location => location.PrimaryKey).ToList();
        
        Dictionary<string, long> remoteKeySizes = new Dictionary<string, long>();
        AsyncResultData addressableSize = new AsyncResultData();
        yield return new RESTPost<string>($"v2/game/addressable_size", new KeyValuePair<string, string>[]
        {
            new KeyValuePair<string, string>("data", TrickCore.JsonExtensions.SerializeToJson(downloadList, false, true)), 
            new KeyValuePair<string, string>("platform", TrickEngineManager.GetPlatform()), 
        }, s =>
        {
            Debug.Log("addressable_size: " + s);
            if (s == null)
            {
                addressableSize.Result = false;
                return;
            }
            try
            {
                List<long> sizes = TrickCore.JsonExtensions.DeserializeJson<List<long>>(s);
                if (downloadList.Count == sizes.Count)
                {
                    remoteKeySizes = downloadList.Select((s1, i) => new KeyValuePair<string, long>(s1, sizes[i]))
                        .ToDictionary(pair => pair.Key, pair => pair.Value);
                
                    addressableSize.Result = true;
                }
                else
                {
                    addressableSize.Result = false;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.Log("Failed to fetch size");
                addressableSize.Result = false;
            }
        }, new RequestFailHandler(() =>
        {
            Debug.Log("Failed to fetch size");
            addressableSize.Result = false;
        }, true));
        
        bool InCondition() => addressableSize.Result != null;
        if (!InCondition()) yield return new WaitUntil(InCondition);
        
        if (!addressableSize.Result.GetValueOrDefault())
        {
            result.Result = false;
            yield break;
        }

        long remoteSize = remoteKeySizes.Values.Sum();

        Debug.Log("Local Download size is: " + localSize);
        // Debug.Log("Bundles to download:\n" + string.Join(",", downloadList));
        Debug.Log("Remote Download size is: " + remoteSize);
        // Debug.Log("Remote file sizes per bundle:\n" + string.Join("\n", remoteKeySizes.Select(pair => $"{pair.Key} - {pair.Value}")));

        InitializeAddressableDownloadFunc?.Invoke(keysToDownload, remoteKeySizes);

        if (remoteSize != 0)
        //if (localSize != 0)
        {
            bool? downloadAddressables = null;
            ModalPopupMenu.ShowYesNoModal("Game Asset Update",
                $"A total of {remoteSize.SizeSuffix(1)} will be downloaded.\nA Wi-Fi connection is recommended.", "Yes", "No",
                () => { downloadAddressables = true; }, () => { downloadAddressables = false; });
            yield return new UnityEngine.WaitUntil(() => downloadAddressables != null);
            if (!downloadAddressables.GetValueOrDefault())
            {
                result.Result = false;
                yield break;
            }
        }

        //Debug.Log($"{keysToDownload.Count} objects needed to download...");
        //Debug.Log(string.Join("\n", keysToDownload.Select(key => $"{key.PrimaryKey}")));
        
        /*IEnumerator ConcurrentDownloadDependencies(int cIndex)
        {
            var downloadKey = keysToDownload[cIndex];
            AsyncResultData downloadSucceeded = new AsyncResultData();
            //if (assetsToDownload.TryGetValue(key.Key, out var downloadKey))
            {
                //Debug.Log($"Downloading: {downloadKey} ({key.Key})");
                //Debug.Log($"Downloading: {downloadKey.PrimaryKey}...");
                yield return DownloadDependenciesAsync(downloadSucceeded, downloadKey.PrimaryKey,
                    //(o) => { UIManager.Instance.GetMenu<DGHMainMenu>().StartAddressableDownload(o); },
                    //(o, f) => { UIManager.Instance.GetMenu<DGHMainMenu>().UpdateAddressableDownload(o, f); },
                    ConcurrentDownloadStartFunc,
                    ConcurrentDownloadUpdateFunc,
                    ConcurrentDownloadCompleteFunc);
                if (!downloadSucceeded.GetValueOrDefault())
                {
                    result.Result = false;
                    yield break;
                }
            }
        }

        yield return this.StartMultiCoroutineConcurrent(keysToDownload.Count, CONCURRENT_STEPS, ConcurrentDownloadDependencies);*/
        
        AsyncResultData downloadSucceeded = new AsyncResultData();
        //if (assetsToDownload.TryGetValue(key.Key, out var downloadKey))
        {
            //Debug.Log($"Downloading: {downloadKey} ({key.Key})");
            //Debug.Log($"Downloading: {downloadKey.PrimaryKey}...");
            yield return DownloadDependenciesAsync(downloadSucceeded, resourceLocations,
                //(o) => { UIManager.Instance.GetMenu<DGHMainMenu>().StartAddressableDownload(o); },
                //(o, f) => { UIManager.Instance.GetMenu<DGHMainMenu>().UpdateAddressableDownload(o, f); },
                ConcurrentDownloadStartFunc,
                ConcurrentDownloadUpdateFunc,
                ConcurrentDownloadCompleteFunc);
            if (!downloadSucceeded.GetValueOrDefault())
            {
                result.Result = false;
                yield break;
            }
        }

        EndAddressableDownloadFunc?.Invoke();
    }
    private IEnumerator RemoteLoadAddressablesFast(AsyncResultData result, Action startDownloadCallback = null)
    {
        /*var locations = Application.isEditor
            ? new List<object>()
            : Addressables.ResourceLocators.FirstOrDefault()?.Keys.ToList() ?? new List<object>();*/

        var locations = Addressables.ResourceLocators.FirstOrDefault()?.Keys.ToList() ?? new List<object>();
        
        var resourceLocationResult = new AsyncResultData();

        using (new TrickStepTimeLog($"[RemoteLoadAddressablesFast] GetAllResourceLocations"))
            yield return GetAllResourceLocations(resourceLocationResult, locations);
        
        if (!(resourceLocationResult.Data is List<IResourceLocation> resourceLocations))
        {
            resourceLocations = new List<IResourceLocation>();
        }
        else
        {
            //Debug.Log(string.Join("\n", resourceLocations.Select(location => $"InternalId {location.InternalId}, PrimaryKey {location.PrimaryKey}, ProviderId {location.ProviderId}, ResourceType {location.ResourceType}, DependencyHashCode {location.DependencyHashCode}")));
        }

        //var resourceLocations = Labels;
        
        long localSize = 0;
        var keysToDownload = new List<IResourceLocation>();
        Dictionary<string, long> remoteKeySizes = new Dictionary<string, long>();
        
        AsyncResultData allResourceSize = new AsyncResultData();
        yield return GetDownloadSize(allResourceSize, resourceLocations);
        if (!allResourceSize.GetValueOrDefault())
        {
            result.Result = false;
            yield break;
        }
        if (allResourceSize.Data is KeyValuePair<object, long> pair)
        {
            if (pair.Value > 0)
            {
                localSize += pair.Value;
                remoteKeySizes.AddOrReplace("all", pair.Value);
            }
        }
        
        IEnumerator ConcurrentGetDownloadSize(int cIndex)
        {
            var key = resourceLocations[cIndex];
            AsyncResultData getDownloadSizeSucceeded = new AsyncResultData();
            yield return GetDownloadSize(getDownloadSizeSucceeded, key);
            if (!getDownloadSizeSucceeded.GetValueOrDefault())
            {
                result.Result = false;
                yield break;
            }

            if (getDownloadSizeSucceeded.Data is KeyValuePair<object, long> pair)
            {
                if (pair.Value > 0)
                {
                    if (!keysToDownload.Contains(key)) keysToDownload.Add(key);
                }
            }
        }
        
        using (new TrickStepTimeLog($"[RemoteLoadAddressablesFast] GetDownloadSize (num={resourceLocations.Count})"))
            yield return this.StartMultiCoroutineConcurrent(resourceLocations.Count, GameManager.Instance.RoutineAllocationCapacity / 2, ConcurrentGetDownloadSize);
        
        long remoteSize = remoteKeySizes.Values.Sum();

        Debug.Log("Local Download size is: " + localSize);
        // Debug.Log("Bundles to download:\n" + string.Join(",", downloadList));
        Debug.Log("Remote Download size is: " + remoteSize);
        // Debug.Log("Remote file sizes per bundle:\n" + string.Join("\n", remoteKeySizes.Select(pair => $"{pair.Key} - {pair.Value}")));

        InitializeAddressableDownloadFuncFast?.Invoke(keysToDownload, remoteKeySizes);

        if (remoteSize != 0)
        {
            bool? downloadAddressables = null;
            ModalPopupMenu.ShowYesNoModal("Game Update",
                $"A total of {remoteSize.SizeSuffix(1)} will be downloaded.\nA Wi-Fi connection is recommended.", "Yes", "No",
                () => { downloadAddressables = true; }, () => { downloadAddressables = false; });
            yield return new UnityEngine.WaitUntil(() => downloadAddressables != null);
            if (!downloadAddressables.GetValueOrDefault())
            {
                result.Result = false;
                yield break;
            }

            startDownloadCallback?.Invoke();
        
            AsyncResultData downloadSucceeded = new AsyncResultData();
            {
                //Debug.Log($"Downloading: {downloadKey} ({key.Key})");
                //Debug.Log($"Downloading: {downloadKey.PrimaryKey}...");
                yield return DownloadDependenciesAsync(downloadSucceeded, keysToDownload,
                    ConcurrentDownloadStartFunc,
                    ConcurrentDownloadUpdateFunc,
                    ConcurrentDownloadCompleteFunc);
                if (!downloadSucceeded.GetValueOrDefault())
                {
                    result.Result = false;
                    yield break;
                }
            }
        }
        

        EndAddressableDownloadFunc?.Invoke();
    }
#endif

        public IEnumerator GetAllResourceLocations(AsyncResultData result, IEnumerable<object> keys)
        {
            result.Result = null;
        
            var op = Addressables.LoadResourceLocationsAsync(keys, Addressables.MergeMode.Union, typeof(Object));
            op.Completed +=
                handle =>
                {
                    result.Data = handle.Result;
                    result.Result = true;
                    Addressables.Release(handle);
                };
        
            bool InCondition() => result.Result != null;
            if (!InCondition()) yield return new WaitUntil(InCondition);
        }

        public void OnInitialized(Action<bool> action)
        {
            if (LocalInitialized) action?.Invoke(false);
            else _localInvokeQueue.Add(action);
        
            if (RemoteInitialized) action?.Invoke(false);
            else _remoteInvokeQueue.Add(action);
        }

        public IEnumerator LoadSceneAssetCoroutine(AssetReference assetReference, LoadSceneMode loadSceneMode, Action<SceneInstance?> onSceneLoad)
        {
            if (!assetReference.IsValidReference())
            {
                onSceneLoad?.Invoke(default);
                yield break;
            }
        
            bool loaded = false;
            SceneInstance? ret = default;
            LoadSceneAssetAsync(assetReference, loadSceneMode, o =>
            {
                ret = o;
                loaded = true;
            });

            bool InCondition() => loaded;
            if (!InCondition()) yield return new WaitUntil(InCondition);
        
            if (ret != null)
            {
                bool InCondition2() => ret.Value.Scene.isLoaded;
                if (!InCondition2()) yield return new WaitUntil(InCondition2);
            }
            onSceneLoad?.Invoke(ret);
        }

        public void LoadSceneAssetAsync(AssetReference assetReference, LoadSceneMode loadSceneMode, Action<SceneInstance?> onSceneLoad)
        {
            var op = Addressables.LoadSceneAsync(assetReference, loadSceneMode);
            op.Completed +=
                handle =>
                {
                    onSceneLoad?.Invoke(handle.Result);
                };
        }

        public IEnumerator UnloadSceneAssetCoroutine(SceneInstance instance, Action<bool> unloadCallback)
        {
            bool loaded = false;
            bool ret = default;
            UnloadSceneAssetAsync(instance, b =>
            {
                ret = b;
                loaded = true;
            });
            bool InCondition() => loaded;
            if (!InCondition()) yield return new WaitUntil(InCondition);
            unloadCallback?.Invoke(ret);
        }

        public void UnloadSceneAssetAsync(SceneInstance instance, Action<bool> unloadCallback)
        {
            Addressables.UnloadSceneAsync(instance).Completed += handle =>
            {
                unloadCallback?.Invoke(handle.Status == AsyncOperationStatus.Succeeded);
            };
        }

        public static void RuntimeReleaseAsset(TrickAssetGroupId group, Object obj)
        {
            Instance.ReleaseAsset(group, obj);
        }

        public static IEnumerator RuntimeInstantiateAssetCoroutine(TrickAssetGroupId group,
            AssetReferenceGameObject value, Action<GameObject> callback)
        {
            yield return RuntimeInstantiateAssetCoroutine(group, value, null, callback);
        }
        public static IEnumerator RuntimeInstantiateAssetCoroutine(TrickAssetGroupId group, AssetReferenceGameObject value, Transform parent, Action<GameObject> callback)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                yield return Routine.Inline(Instance.InstantiateAssetCoroutine(group, value, parent, callback));
            }
            else
            {
                if (value.IsValidReference())
                {
                    var editorAsset = value.editorAsset;
                    if (editorAsset != null)
                        callback?.Invoke(Instantiate(editorAsset));
                    else
                        callback?.Invoke(default);
                }
                else
                {
                    callback?.Invoke(default);
                }
            }
#else
        yield return Routine.Inline(Instance.InstantiateAssetCoroutine(group, value, parent, callback));
#endif
        }
        public static IEnumerator RuntimeLoadAssetCoroutine<T>(TrickAssetGroupId group, AssetReference value,
            Action<T> callback) where T : Object
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                yield return Routine.Inline(Instance.LoadAssetCoroutine(group, value, callback));
            }
            else
            {
                if (value.IsValidReference())
                {
                    callback?.Invoke(value.editorAsset as T);
                }
                else
                {
                    callback?.Invoke(default);
                }
            }
#else
        yield return Routine.Inline(Instance.LoadAssetCoroutine(group, value, callback));
#endif
        }
    }
}
#endif