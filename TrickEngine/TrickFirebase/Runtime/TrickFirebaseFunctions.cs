using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrickCore
{
    public static class TrickFirebaseFunctions
    {
        public static string Region = "europe-west3";
        public static void CallCloudFunctionArgsJava(string functionName, Dictionary<string,object> parameters,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(CallCloudFunctionArgsJava), callbackOrFallback, false,
                functionName);
            FirebaseFunctions.CallCloudFunctionArgsJava(Region, functionName, parameters.SerializeToJson(false, true),
                nameof(FirebaseManager), $"{nameof(CallCloudFunctionArgsJava)}Callback",
                $"{nameof(CallCloudFunctionArgsJava)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            var callable =
 Firebase.Functions.FirebaseFunctions.GetInstance(Firebase.FirebaseApp.DefaultInstance, Region).GetHttpsCallable($"{functionName}");
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
    
        public static void CallCloudFunctionJava(string functionName, Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(CallCloudFunctionJava), callbackOrFallback, false, functionName);
            FirebaseFunctions.CallCloudFunctionJava(Region, functionName,
                nameof(FirebaseManager), $"{nameof(CallCloudFunctionJava)}Callback",
                $"{nameof(CallCloudFunctionJava)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            var callable =
 Firebase.Functions.FirebaseFunctions.GetInstance(Firebase.FirebaseApp.DefaultInstance, Region).GetHttpsCallable($"{functionName}");
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