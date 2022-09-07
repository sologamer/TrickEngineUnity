using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

public class TrickEngineUPM : EditorWindow
{
    public static TrickEngineUPM Instance { get; set; }

    private Dictionary<string, Request> ActiveRequests = new Dictionary<string, Request>();

    private Dictionary<string, TrickEnginePackage> _packageData =
        new Dictionary<string, TrickEnginePackage>();

    private List<TrickEnginePackage> AddList = new List<TrickEnginePackage>();
    private List<TrickEnginePackage> RemoveList = new List<TrickEnginePackage>();

    private ListRequest _packages;
    private Vector2 _scrollPosition;
    private object _recommended;

    /*[MenuItem("Window/Add Package Example")]
    static void Add()
    {
        // Add a package to the project
        Request = Client.Add("com.unity.textmeshpro");
        EditorApplication.update += Progress;
    }*/
    [MenuItem("TrickEngine/Module Window")]
    static void InitWindow()
    {
        TrickEngineUPM window = GetWindow<TrickEngineUPM>();
        window.Show();
        window.InitializeWindow();
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        if (Instance != null)
            Instance.FetchPackages();
    }

    private void OnEnable()
    {
        Instance = this;
    }

    private void OnDisable()
    {
        Instance = null;
    }

    private void OnGUI()
    {
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
        foreach (var pair in _packageData)
        {
            var packageData = pair.Value;
            GUILayout.BeginHorizontal("box");
            GUILayout.Label($"{packageData.displayName} - {packageData.name}");
            GUILayout.FlexibleSpace();

            GUI.enabled = _packages.Status == StatusCode.Success && !RemoveList.Contains(packageData) && !AddList.Contains(packageData);

            bool hasPackage = _packages.Status == StatusCode.Success &&
                              _packages.Result.Any(info => info.name == packageData.name);

            if (ActiveRequests.Count > 0)
            {
                if (GUILayout.Button(".......", GUILayout.Width(120)))
                {
                    
                }
            }
            else
            {
                if (hasPackage)
                {
                    if (GUILayout.Button("Remove", GUILayout.Width(120)))
                    {
                        RemoveList.Add(packageData);
                        Debug.Log($"Remove: {pair.Value}");
                    }
                }
                else
                {
                    if (GUILayout.Button("Add", GUILayout.Width(120)))
                    {
                        AddList.Add(packageData);
                        Debug.Log($"Add: {pair.Value}");
                    }
                }
            }
            
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }
        
        GUILayout.BeginHorizontal("box");
        

        if (_recommended is JArray array && array.Values<string>().ToList() is {} list)
        {
            var recommended = _packageData.Where(pair => list.Contains(pair.Key)).Where(pair =>
            {
                bool hasPackage = _packages.Status == StatusCode.Success &&
                                  _packages.Result.Any(info => info.name == pair.Value.name);
                return !hasPackage && !AddList.Contains(pair.Value);
            }).Select(pair => pair.Value).ToList();

            GUI.enabled = recommended.Count > 0;
            
            if (GUILayout.Button("Add Recommended", GUILayout.Width(120)))
            {
                AddList.AddRange(recommended);
            }
            
            GUI.enabled = true;
        }
        
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Add All", GUILayout.Width(120)))
        {
            AddList.AddRange(_packageData.Where(pair =>
            {
                bool hasPackage = _packages.Status == StatusCode.Success &&
                                  _packages.Result.Any(info => info.name == pair.Value.name);
                return !hasPackage && !AddList.Contains(pair.Value);
            }).Select(pair => pair.Value));
        }
        
        
        if (GUILayout.Button("Remove All", GUILayout.Width(120)))
        {
            RemoveList.AddRange(_packageData.Where(pair =>
            {
                bool hasPackage = _packages.Status == StatusCode.Success &&
                                  _packages.Result.Any(info => info.name == pair.Value.name);
                return hasPackage && !RemoveList.Contains(pair.Value);
            }).Select(pair => pair.Value));
        }
        
        GUILayout.EndHorizontal();
        
        GUILayout.Space(20);
        
        GUILayout.BeginVertical("box");

        for (var index = 0; index < AddList.Count; index++)
        {
            var package = AddList[index];
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("x", GUILayout.Width(30)))
            {
                AddList.RemoveAt(index);
                index--;
            }
            GUILayout.Label($"(add) {package.name}");
            GUILayout.EndHorizontal();
        }

        for (var index = 0; index < RemoveList.Count; index++)
        {
            var package = RemoveList[index];
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("x", GUILayout.Width(30)))
            {
                RemoveList.RemoveAt(index);
                index--;
            }
            GUILayout.Label($"(remove) {package.name}");
            GUILayout.EndHorizontal();
        }

        if ((AddList.Count > 0 || RemoveList.Count > 0) && GUILayout.Button("Execute"))
        {
#if UNITY_2021_1_OR_NEWER
            ActiveRequests["add_or_remove"] = Client.AddAndRemove(
                AddList.Select(package => package.downloadPath).ToArray(),
                RemoveList.Select(package => package.name).ToArray());
#else
            var upmKey = $"{Application.dataPath}UPM";
            
            // Remove first
            var removeArr = RemoveList.Select(package => package.downloadPath).ToArray();
            if (removeArr.Length > 0) EditorPrefs.SetString($"{upmKey}Remove", JsonConvert.SerializeObject(removeArr));
            
            // Add after
            var addArr = AddList.Select(package => package.downloadPath).ToArray();
            if (addArr.Length > 0) EditorPrefs.SetString($"{upmKey}Add", JsonConvert.SerializeObject(addArr));

            ProcessPackageKey();
#endif
            
            if (ActiveRequests.Count == 1)
                EditorApplication.update += Progress;
        }
        
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
    }

    private void ProcessPackageKey()
    {
        var upmKey = $"{Application.dataPath}UPM";
        
        if (EditorPrefs.HasKey($"{upmKey}Remove"))
        {
            var removeList = JsonConvert.DeserializeObject<List<string>>(EditorPrefs.GetString($"{upmKey}Remove"));
            if (removeList != null && removeList.Count > 0)
            {
                var key = removeList[0];
                removeList.RemoveAt(0);
                ActiveRequests["add_or_remove"] = Client.Remove(key);
                if (removeList.Count == 0) EditorPrefs.DeleteKey($"{upmKey}Remove");
                
                if (ActiveRequests.Count == 1)
                    EditorApplication.update += Progress;
                
                return;
            }
        }
        
        if (EditorPrefs.HasKey($"{upmKey}Add"))
        {
            var addList = JsonConvert.DeserializeObject<List<string>>(EditorPrefs.GetString($"{upmKey}Add"));
            if (addList != null && addList.Count > 0)
            {
                var key = addList[0];
                addList.RemoveAt(0);
                ActiveRequests["add_or_remove"] = Client.Add(key);
                if (addList.Count == 0) EditorPrefs.DeleteKey($"{upmKey}Add");
                
                if (ActiveRequests.Count == 1)
                    EditorApplication.update += Progress;
            }
        }
        
    }

    private void InitializeWindow()
    {
        FetchPackages();
    }

    private void FetchPackages()
    {
        _packages = Client.List();
        string packageListUrl = "https://raw.githubusercontent.com/sologamer/TrickEngineUnity/main/packages.json";
        // https://github.com/sologamer/TrickEngineUnity?path=TrickEngine/TrickCore/package.json
        // https://raw.githubusercontent.com/sologamer/TrickEngineUnity/main/TrickEngine/TrickCore/package.json
        var packageList = UnityWebRequest.Get(packageListUrl);
        var send = packageList.SendWebRequest();
        send.completed += _ =>
        {
            SetPackageList(JsonConvert.DeserializeObject<Dictionary<string, object>>(packageList.downloadHandler.text));
            
            ProcessPackageKey();
        };
    }

    
    private void SetPackageList(Dictionary<string, object> dict)
    {
        if (dict.TryGetValue("recommended", out var recommended))
        {
            _recommended = recommended;
            dict.Remove("recommended");
        }
        dict.OrderBy(pair => pair.Key).ToList().ForEach(s =>
        {
            var captured = s;
            var packageJsonPath = ToRawUrl($"{captured.Value}/package.json");
            var packgeJsonRequest = UnityWebRequest.Get(packageJsonPath);
            var send = packgeJsonRequest.SendWebRequest();
            send.completed += _ =>
            {
                var package = JsonConvert.DeserializeObject<TrickEnginePackage>(packgeJsonRequest
                    .downloadHandler.text);
                if (package != null)
                {
                    package.downloadPath = captured.Value.ToString();
                    _packageData[s.Key] = package;
                }
            };
        });
    }

    private string ToRawUrl(string githubUrl)
    {
        return githubUrl
            .Replace("github.com", "raw.githubusercontent.com")
            .Replace(".git?path=", "/main/")
            ;
    }


    private static void Progress()
    {
        if (Instance == null)
        {
            EditorApplication.update -= Progress;
            return;
        }
        if (Instance.ActiveRequests.Count > 0)
        {
            if (Instance.ActiveRequests.All(request => request.Value.Status != StatusCode.InProgress))
            {
                Instance.FetchPackages();
                Instance.ActiveRequests.Clear();
                Instance.ProcessPackageKey();
            }
            if (Instance.ActiveRequests.Count == 0)
                EditorApplication.update -= Progress;
        }
    }
}

[JsonObject]
public class TrickEngineRepository {
    public string type { get; set; }
    public string url { get; set; }

}

[JsonObject]
public class TrickEngineAuthor {
    public string name { get; set; }
    public string email { get; set; }
    public string url { get; set; }
}

[JsonObject]
public class TrickEnginePackage {
    public string downloadPath { get; set; }
    public string name { get; set; }
    public string version { get; set; }
    public string displayName { get; set; }
    public string description { get; set; }
    public string unity { get; set; }
    public string unityRelease { get; set; }
    public string documentationUrl { get; set; }
    public string changelogUrl { get; set; }
    public string licensesUrl { get; set; }
    public TrickEngineRepository repository { get; set; } 
    public IList<string> keywords { get; set; }
    public TrickEngineAuthor author { get; set; }
}