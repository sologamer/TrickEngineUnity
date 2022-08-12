#if UNITY_ADDRESSABLES
#define EDITOR_QUICK_LOAD
#define EDITOR_QUICK_INSTANTIATE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BeauRoutine;
using Sirenix.OdinInspector;
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
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class AddressablesManager : MonoSingleton<AddressablesManager>
{
    [ShowInInspector, NonSerialized] private readonly Dictionary<AddressableGroupType, HashSet<GameObject>> _assets =
        new Dictionary<AddressableGroupType, HashSet<GameObject>>()
        {
            {AddressableGroupType.Game, new HashSet<GameObject>()},
            {AddressableGroupType.Lobby, new HashSet<GameObject>()},
            {AddressableGroupType.ReleaseOnDestroy, new HashSet<GameObject>()}
        };
    
    [ShowInInspector, NonSerialized] private readonly Dictionary<AddressableGroupType, HashSet<UnityEngine.Object>> _assetObjects =
        new Dictionary<AddressableGroupType, HashSet<UnityEngine.Object>>()
        {
            {AddressableGroupType.Game, new HashSet<UnityEngine.Object>()},
            {AddressableGroupType.Lobby, new HashSet<UnityEngine.Object>()},
            {AddressableGroupType.ReleaseOnDestroy, new HashSet<UnityEngine.Object>()},
        };
    
    [ShowInInspector, NonSerialized]
    private readonly List<UnityEngine.Object> _dontDestroyAssets = new List<UnityEngine.Object>();
    public List<string> Labels = new List<string>();
    private string _addressablesVersion;
    private readonly List<Action<bool>> _localInvokeQueue = new List<Action<bool>>();
    private readonly List<Action<bool>> _remoteInvokeQueue = new List<Action<bool>>();

    [NonSerialized] public Action<object> ConcurrentDownloadStartFunc = null;
    [NonSerialized] public Action<object, DownloadStatus> ConcurrentDownloadUpdateFunc = null;
    [NonSerialized] public Action<object> ConcurrentDownloadCompleteFunc = null;
    [NonSerialized] public Action<List<IResourceLocation>, Dictionary<string, long>> InitializeAddressableDownloadFunc = null;
    [NonSerialized] public Action<List<IResourceLocation>, Dictionary<string, long>> InitializeAddressableDownloadFuncFast = null;
    [NonSerialized] public Action EndAddressableDownloadFunc = null;
    private const int CONCURRENT_STEPS = 100;
    
    public bool LocalInitialized { get; set; }
    public bool RemoteInitialized { get; set; }

    public int NumInstantiatingAssets { get; set; }
    public int NumLoadingAssets { get; set; }

#if UNITY_EDITOR

    public List<GameObject> GetAssetsGameObjects(AddressableGroupType group)
    {
        return _assets[group].ToList();
    }
    
    public List<Object> GetAssetsObjects(AddressableGroupType group)
    {
        return _assetObjects[group].ToList();
    }
    
    [Button]
    public void UpdateAllLabels()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        HashSet<string> usedLabels = new HashSet<string>();
        foreach (var label in from @group in settings.groups from entry in @group.entries from label in entry.labels.Where(label => !usedLabels.Contains(label)) select label)
            usedLabels.Add(label);
        Labels = usedLabels.ToList();
    }
    #endif

    protected override void Initialize()
    {
        base.Initialize();
        
        //BetterStreamingAssets.Initialize();
        
        if (!PlayerPrefs.HasKey("FirstTimeInit_V3"))
        {
            PlayerPrefs.SetInt("FirstTimeInit_V3", 1);
            PlayerPrefs.Save();
            
            // Make sure we clear the cache, if it's a new game
            Caching.ClearCache();
            
            // Reset this key
            PlayerPrefs.DeleteKey(Addressables.kAddressablesRuntimeDataPath);
        }

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

    public bool ReleaseAsset(AddressableGroupType group, UnityEngine.Object obj)
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
                if (group == AddressableGroupType.ReleaseOnDestroy)
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
    
    [Button]
    public void ReleaseAllInstances(AddressableGroupType group)
    {
        _assets[group].Except(_dontDestroyAssets.OfType<GameObject>()).Where(o => o != null).ToList()
            .ForEach(o => Addressables.ReleaseInstance(o));
        _assetObjects[group].Except(_dontDestroyAssets).Where(o => o != null).ToList().ForEach(Addressables.Release);
        
        _assetObjects[group].Clear();
        _assets[group].Clear();
    }

    public void DontDestroyAsset(AddressableGroupType group, UnityEngine.Object o)
    {
        if (o == null) return;
        _dontDestroyAssets.Add(o);
        if (o is GameObject go)
            _assets[group].Remove(go);
        else
            _assetObjects[group].Remove(o);
    }

    public void RemoveDontDestroyAsset(AddressableGroupType group, UnityEngine.Object o)
    {
        if (o == null) return;
        if (!_dontDestroyAssets.Remove(o)) return;
        if (o is GameObject go)
            _assets[@group].Add(go);
        else
            _assetObjects[@group].Add(o);
    }

    public void InstantiateAssetAsync(AddressableGroupType group, AssetReference ar, Vector3 position, Quaternion quaternion,
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

    public static Dictionary<string, AssetReferenceGameObject> CachedArGoes { get; } = new Dictionary<string, AssetReferenceGameObject>();

    public void InstantiateAssetAsync(AddressableGroupType group, AssetReference ar, Transform parent, Action<GameObject> callback)
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
    
    private void HandleGameObjectResult(AddressableGroupType @group, AssetReference ar, GameObject handleResult, Action<GameObject> callback)
    {
        NumInstantiatingAssets--;
        if (handleResult != null)
        {
            _assets[@group].Add(handleResult);
                
            if (handleResult.TryGetComponent<ITrickPool>(out var poolObject))
            {
                poolObject.AssetGroupType = @group;
                        
                if (!(ar is AssetReferenceGameObject arGo))
                {
                    if (!CachedArGoes.TryGetValue(ar.AssetGUID, out arGo))
                        CachedArGoes.AddOrReplace(ar.AssetGUID, arGo = new AssetReferenceGameObject(ar.AssetGUID));
                }
                        
                poolObject.AssetReferenceGameObject = arGo;

                poolObject.OnInstantiated();
            }

            if (@group == AddressableGroupType.ReleaseOnDestroy)
            {
                if (!handleResult.TryGetComponent<AddressableAssetHelper>(out var helper))
                    helper = handleResult.AddComponent<AddressableAssetHelper>();
                helper.IsUsingManager = true;
                helper.AddData(handleResult);
            }
        }
            
        callback?.Invoke(handleResult);
    }

    public IEnumerator InstantiateAssetCoroutine(AddressableGroupType group, AssetReferenceGameObject value, Action<GameObject> callback)
    {
        yield return Routine.Inline(InstantiateAssetCoroutine(group, value, null, callback));
    }
    
    public IEnumerator InstantiateAssetCoroutine(AddressableGroupType group, AssetReferenceGameObject value, Transform parent, Action<GameObject> callback)
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
            // if (go != null)
            // {
            //     go.transform.localPosition = Vector3.zero;
            //     go.transform.localRotation = Quaternion.identity;
            // }

            loaded = true;
        });

        bool InCondition() => loaded;

        if (!InCondition()) yield return new WaitUntil(InCondition);
        callback?.Invoke(go);
    }

    public void LoadAssetAsync<T>(AddressableGroupType group, AssetReference value, Action<T> callback)
        where T : UnityEngine.Object
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
                
                if (asyncOperationHandle.IsValid())
                {
                    if (asyncOperationHandle.Result != null && typeof(T) == typeof(GameObject) &&
                        asyncOperationHandle.Result is GameObject go)
                    {
                        if (go.TryGetComponent<ITrickPool>(out var poolObject))
                        {
                            poolObject.AssetGroupType = group;
                            
                            if (!(value is AssetReferenceGameObject arGo))
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

    public IEnumerator LoadAssetCoroutine<T>(AddressableGroupType group, AssetReference value, Action<T> callback) where T : UnityEngine.Object
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
    
    private void HandleLoadResult<T>(AddressableGroupType @group, AssetReference value, T handleResult, ref bool loaded, Action<T> callback) where T : Object
    {
        NumLoadingAssets--;

        if (handleResult != null && typeof(T) == typeof(GameObject) && handleResult is GameObject go)
        {
            var poolObject = go.GetComponent<ITrickPool>();
            if (poolObject != null)
            {
                poolObject.AssetGroupType = @group;

                if (!(value is AssetReferenceGameObject arGo))
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

    public IEnumerator InstantiateAssetCoroutine(AddressableGroupType group, AssetReferenceGameObject value, Vector3 position,
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

    /*private IEnumerator ReadFromStreamingAssets(string filePath, Action<string> callback)
    {
        string fullPath = $"{Application.streamingAssetsPath}/{filePath}";
        Debug.Log("ReadFromStreamingAssets: " + fullPath);
        if (Application.platform == RuntimePlatform.Android)
        {
            using (UnityWebRequest uwr = UnityWebRequest.Get(fullPath))
            {
                yield return uwr.SendWebRequest();
                if (uwr.isNetworkError || uwr.isHttpError)
                {
                    Debug.LogError("www error:" + uwr.error + " " + filePath);
                }
                else
                {
                    callback?.Invoke(uwr.downloadHandler.text);
                }
            }
        }
        else
        {
            Debug.Log("Reading file: " + filePath);
            string content = null;
            if (BetterStreamingAssets.FileExists(filePath))
            {
                Debug.Log("BSA Read");
                content = BetterStreamingAssets.ReadAllText(filePath);
            }

            if (content == null)
            {
                Debug.Log("FileIO Read (full)");
                if (File.Exists(fullPath))
                {
                    content = File.ReadAllText(fullPath);
                }
            }

            if (content == null)
            {
                Debug.Log("FileIO Read (filePath)");
                if (File.Exists(filePath))
                {
                    content = File.ReadAllText(filePath);
                }
            }
            
            callback?.Invoke(content);
        }
    }*/
    
    public IEnumerator InitializeAddressables(AsyncResultData result)
    {
        result.Result = null;
        
        // var path = Addressables.ResolveInternalId(PlayerPrefs.GetString(Addressables.kAddressablesRuntimeDataPath,
        //     $"{Addressables.RuntimePath}/settings.json"));
        //
        // var addressablesSettingsData = TrickCore.JsonExtensions.DeserializeJson<AddressablesSettingsData>(File.ReadAllText(path));
        // var catalogLocation = addressablesSettingsData?.CatalogLocations.FirstOrDefault(data => data.Keys.Any(s => s == "AddressablesMainContentCatalogRemoteHash"));
        // if (catalogLocation != null)
        // {
        //     string hashLocation = catalogLocation.InternalId;
        //     string pathWithoutRoot = hashLocation.Replace(RESTHelper.Settings.BaseUrl, string.Empty);
        //     yield return new RESTGet<string>(pathWithoutRoot, null, s =>
        //     {
        //         Debug.Log($"Result: {s}");
        //         if (string.IsNullOrEmpty(s) || s.Length != 32)
        //         {
        //             result.Result = false;
        //         }
        //     }, new RequestFailHandler(() =>
        //         {
        //             result.Result = false;
        //         }, true));
        //
        //     if (result.Result != null && !result.Result.GetValueOrDefault())
        //         yield break;
        //     
        //     void Completed(AsyncOperationHandle<IResourceLocator> handle)
        //     {
        //         Debug.Log("Addressables Initialize Status " + handle.Status);
        //         result.Result = handle.Status == AsyncOperationStatus.Succeeded;
        //     }
        //
        //     var initOp = Addressables.InitializeAsync();
        //     initOp.Completed += Completed;
        // }
        // else
        // {
        //     result.Result = false;
        // }

        // // Try the game version
        // AsyncResultData catalog = new AsyncResultData();
        // yield return new RESTGet<string>($"addressables/{GameManager.Instance.GetPlatform()}/catalog_{GameManager.ToAndroidVersion(Application.version).ToString()}.hash", null, s =>
        // {
        //     Debug.Log($"Result: {s} (version={GameManager.ToAndroidVersion(Application.version).ToString()})");
        //     if (string.IsNullOrEmpty(s) || s.Length != 32)
        //     {
        //         catalog.Result = false;
        //     }
        //     else
        //     {
        //         catalog.Result = true;
        //         catalog.Data = s;
        //     }
        // }, new RequestFailHandler(() =>
        //     {
        //         result.Result = false;
        //     }, true));
        //
        // // Fallback to version 1
        // if (catalog.Result != null && !catalog.Result.GetValueOrDefault())
        // {
        //     catalog = new AsyncResultData();
        //     yield return new RESTGet<string>($"addressables/{GameManager.Instance.GetPlatform()}/catalog_1.hash", null,
        //         s =>
        //         {
        //             Debug.Log($"Result: {s} (version=1)");
        //             if (string.IsNullOrEmpty(s) || s.Length != 32)
        //             {
        //                 catalog.Result = false;
        //             }
        //             else
        //             {
        //                 catalog.Result = true;
        //                 catalog.Data = s;
        //             }
        //         }, new RequestFailHandler(() => { catalog.Result = false; }, true));
        //
        //     if (catalog.Result != null && !catalog.Result.GetValueOrDefault())
        //     {
        //         // Failed
        //         result.Result = false;
        //         yield break;
        //     }
        //     else
        //     {
        //         yield return UpdateSettings("1", result.Data is string hash ? hash : null);
        //     }
        // }
        // else
        // {
        //     yield return UpdateSettings(GameManager.ToAndroidVersion(Application.version).ToString(), result.Data is string hash ? hash : null);
        // }


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

    public IEnumerator LoadAddressables(bool withRemote, AsyncResultData result, Action startDownloadCallback = null)
    {
        if (!withRemote)
        {
            using (new StepTimeLog($"[LoadAddressables] Initialize (withRemote: {withRemote})"))
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
            using (new StepTimeLog($"[LoadAddressables] Release instances"))
            {
                yield return null;
                ReleaseAllInstances(AddressableGroupType.Lobby);
                ReleaseAllInstances(AddressableGroupType.Game);
            }
            
            // Check for a catalog update
            using (new StepTimeLog("[Local] LoadAddressables"))
            {
                AsyncResultData checkCatalogUpdates = new AsyncResultData();
                yield return CheckUpdateCatalog(checkCatalogUpdates);
                if (!checkCatalogUpdates.GetValueOrDefault())
                {
                    result.Result = false;
                }
            }

            using (new StepTimeLog("[Remote] LoadAddressables"))
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

        using (new StepTimeLog($"[RemoteLoadAddressablesFast] GetAllResourceLocations"))
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
        
        using (new StepTimeLog($"[RemoteLoadAddressablesFast] GetDownloadSize (num={resourceLocations.Count})"))
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

    public class AssetToDownloadData
    {
        public IResourceLocation RootLocation;
        
        // Dependencies
        public List<IResourceLocation> Deps = new List<IResourceLocation>();
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

    public static void RuntimeReleaseAsset(AddressableGroupType @group, Object obj)
    {
#if UNITY_EDITOR && EDITOR_QUICK_LOAD && false
        if (Application.isPlaying)
        {
            Instance.ReleaseAsset(group, obj);
        }
        else
        {
            DestroyImmediate(obj, false);
        }
#else
        Instance.ReleaseAsset(group, obj);
#endif
    }

    public static IEnumerator RuntimeInstantiateAssetCoroutine(AddressableGroupType group,
        AssetReferenceGameObject value, Action<GameObject> callback)
    {
        yield return RuntimeInstantiateAssetCoroutine(group, value, null, callback);
    }
    public static IEnumerator RuntimeInstantiateAssetCoroutine(AddressableGroupType group, AssetReferenceGameObject value, Transform parent, Action<GameObject> callback)
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
    public static IEnumerator RuntimeLoadAssetCoroutine<T>(AddressableGroupType group, AssetReference value,
        Action<T> callback) where T : UnityEngine.Object
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
#endif