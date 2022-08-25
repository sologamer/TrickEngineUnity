using System;
using FirebaseWebGL.Scripts.Objects;
using TrickCore;
using UnityEngine;

public static class TrickFirebaseStorage
{
    public static void DownloadFile(string location,
        Action<(string content, FirebaseError error)> callbackOrFallback)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            FirebaseManager.Instance.Register(nameof(DownloadFile), callbackOrFallback, false, location);
            FirebaseWebGL.Scripts.FirebaseBridge.FirebaseStorage.DownloadFile(location,
                nameof(FirebaseManager), $"{nameof(DownloadFile)}Callback", $"{nameof(DownloadFile)}Fallback");
        }
        else
        {
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            Firebase.Storage.FirebaseStorage.DefaultInstance.GetReference($"{location}")
                .GetBytesAsync(long.MaxValue)
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    var bytes = task.Result;
                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((Convert.ToBase64String(bytes), null)));
                });
#endif
        }
    }
    
    public static void UploadFile(string location, string dataBase64,
        Action<(string content, FirebaseError error)> callbackOrFallback)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            FirebaseManager.Instance.Register(nameof(UploadFile), callbackOrFallback, false, location);
            FirebaseWebGL.Scripts.FirebaseBridge.FirebaseStorage.UploadFile(location, dataBase64,
                nameof(FirebaseManager), $"{nameof(UploadFile)}Callback", $"{nameof(UploadFile)}Fallback");
        }
        else
        {
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            Firebase.Storage.FirebaseStorage.DefaultInstance.GetReference($"{location}")
                .PutBytesAsync(Convert.FromBase64String(dataBase64))
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    var bytes = task.Result;
                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((bytes.Path, null)));
                });
#endif
        }
    }
    
    public static void DeleteFile(string location, Action<(string content, FirebaseError error)> callbackOrFallback)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            FirebaseManager.Instance.Register(nameof(DeleteFile), callbackOrFallback, false, location);
            FirebaseWebGL.Scripts.FirebaseBridge.FirebaseStorage.DeleteFile(location, nameof(FirebaseManager), $"{nameof(DeleteFile)}Callback", $"{nameof(DeleteFile)}Fallback");
        }
        else
        {
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            Firebase.Storage.FirebaseStorage.DefaultInstance.GetReference($"{location}")
                .DeleteAsync()
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke(("{}", null)));
                });
#endif
        }
    }
}