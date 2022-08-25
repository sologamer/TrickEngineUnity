using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FirebaseWebGL.Scripts.Objects;
using TrickCore;
using UnityEngine;

public static class TrickFirebaseRemoteConfig
{
    public static void FetchAndActivate(Action<(string content, FirebaseError error)> callbackOrFallback)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            FirebaseManager.Instance.Register(nameof(FetchAndActivate), callbackOrFallback, false);
            FirebaseWebGL.Scripts.FirebaseBridge.FirebaseRemoteConfig.FetchAndActivate(nameof(FirebaseManager), $"{nameof(FetchAndActivate)}Callback", $"{nameof(FetchAndActivate)}Fallback");
        }
        else
        {
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync()
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    var result = task.Result;
                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((result.ToString().ToLower(), null)));
                });
#endif
        }
    }
    
    
    public static void GetAll(Action<(string content, FirebaseError error)> callbackOrFallback)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            FirebaseManager.Instance.Register(nameof(FetchAndActivate), callbackOrFallback, false);
            FirebaseWebGL.Scripts.FirebaseBridge.FirebaseRemoteConfig.GetAll(nameof(FirebaseManager), $"{nameof(FetchAndActivate)}Callback", $"{nameof(FetchAndActivate)}Fallback");
        }
        else
        {
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE

            TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((
                Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.AllValues
                    .ToDictionary(pair => pair.Key, pair => pair.Value.StringValue)
                    .SerializeToJson(false, true, FirebaseManager.FirebaseContractResolver), null)));
#endif
        }
    }
}