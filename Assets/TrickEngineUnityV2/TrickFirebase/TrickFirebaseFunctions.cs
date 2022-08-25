using System;
using System.Collections;
using System.Collections.Generic;
using FirebaseWebGL.Scripts.Objects;
using TrickCore;
using UnityEngine;

public static class TrickFirebaseFunctions
{
    public static string Region = "europe-west3";
    public static void CallCloudFunctionArgsJava(string functionName, Dictionary<string,object> parameters,
        Action<(string content, FirebaseError error)> callbackOrFallback)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            FirebaseManager.Instance.Register(nameof(CallCloudFunctionArgsJava), callbackOrFallback, false, functionName);
            FirebaseWebGL.Scripts.FirebaseBridge.FirebaseFunctions.CallCloudFunctionArgsJava(Region, functionName, parameters.SerializeToJson(false, true),
                nameof(FirebaseManager), $"{nameof(CallCloudFunctionArgsJava)}Callback",
                $"{nameof(CallCloudFunctionArgsJava)}Fallback");
        }
        else
        {
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            var callable = Firebase.Functions.FirebaseFunctions.GetInstance(Firebase.FirebaseApp.DefaultInstance, Region).GetHttpsCallable($"{functionName}");
            (parameters == null || parameters.Count == 0 ? callable.CallAsync() : callable.CallAsync(parameters))
                .ContinueWith(task =>
                {
                    
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    var result = task.Result;
                    if (result.Data is IDictionary)
                    {
                        TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((result.Data.SerializeToJson(false, true), null)));
                    }
                    else
                    {
                        TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((result.Data.ToString(), null)));
                    }
                });
#endif
        }
    }
    
    public static void CallCloudFunctionJava(string functionName, Action<(string content, FirebaseError error)> callbackOrFallback)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            FirebaseManager.Instance.Register(nameof(CallCloudFunctionJava), callbackOrFallback, false, functionName);
            FirebaseWebGL.Scripts.FirebaseBridge.FirebaseFunctions.CallCloudFunctionJava(Region, functionName,
                nameof(FirebaseManager), $"{nameof(CallCloudFunctionJava)}Callback",
                $"{nameof(CallCloudFunctionJava)}Fallback");
        }
        else
        {
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            var callable = Firebase.Functions.FirebaseFunctions.GetInstance(Firebase.FirebaseApp.DefaultInstance, Region).GetHttpsCallable($"{functionName}");
            callable.CallAsync()
                .ContinueWith(task =>
                {
                    
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    var result = task.Result;
                    if (result.Data is IDictionary)
                    {
                        TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((result.Data.SerializeToJson(false, true), null)));
                    }
                    else
                    {
                        TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((result.Data.ToString(), null)));
                    }
                });
#endif
        }
    }
}