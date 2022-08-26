using System;
using UnityEngine;

namespace TrickCore
{
    public static class TrickFirebaseAuth
    {
        public static void CreateUserWithEmailAndPassword(string email, string password, Action<(string content, FirebaseError error)> callbackOrFallback)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                FirebaseManager.Instance.Register(nameof(CreateUserWithEmailAndPassword), callbackOrFallback, false, email+password);
                FirebaseAuth.CreateUserWithEmailAndPassword(email, password,
                    nameof(FirebaseManager), $"{nameof(CreateUserWithEmailAndPassword)}Callback", $"{nameof(CreateUserWithEmailAndPassword)}Fallback");
            }
            else
            {
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            Firebase.Auth.FirebaseAuth.DefaultInstance.CreateUserWithEmailAndPasswordAsync(email, password)
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    // Firebase user has been created.
                    Firebase.Auth.FirebaseUser newUser = task.Result;
                    Debug.LogFormat("Firebase user created successfully: {0} ({1})",
                        newUser.DisplayName, newUser.UserId);

                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((newUser.SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
                });
#endif
            }
        }
    
        public static void SignInWithEmailAndPassword(string email, string password,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                FirebaseManager.Instance.Register(nameof(SignInWithEmailAndPassword), callbackOrFallback, false, email+password);
                FirebaseAuth.SignInWithEmailAndPassword(email, password,
                    nameof(FirebaseManager), $"{nameof(SignInWithEmailAndPassword)}Callback", $"{nameof(SignInWithEmailAndPassword)}Fallback");            
            }
            else
            {
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            Firebase.Auth.FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(
                task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    // Firebase user has been created.
                    Firebase.Auth.FirebaseUser newUser = task.Result;
                    Debug.LogFormat("User signed in successfully: {0} ({1})",
                        newUser.DisplayName, newUser.UserId);
                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((newUser.SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
                });
#endif
            }
        }

        public static void OnAuthStateChanged(Action<FirebaseUser> callbackOrFallback)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                FirebaseManager.Instance.Register(nameof(OnAuthStateChanged), tuple =>
                {
                    callbackOrFallback?.Invoke(tuple.error != null
                        ? null
                        : tuple.content != "{}"
                            ? tuple.content.DeserializeJsonTryCatch<FirebaseUser>()
                            : null);
                }, true);
                FirebaseAuth.OnAuthStateChanged(nameof(FirebaseManager), $"{nameof(OnAuthStateChanged)}Callback", $"{nameof(OnAuthStateChanged)}Fallback");            
            }
            else
            {
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE

            void OnDefaultInstanceOnStateChanged(object sender, EventArgs args)
            {
                if (sender is Firebase.Auth.FirebaseAuth auth)
                {
                    if (auth.CurrentUser != null)
                    {
                        var userJson = auth.CurrentUser.SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver);
                        TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke(userJson.DeserializeJsonTryCatch<FirebaseUser>()));
                    }
                    else
                    {
                        TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke(null));
                    }
                }
            }

            Firebase.Auth.FirebaseAuth.DefaultInstance.StateChanged -= OnDefaultInstanceOnStateChanged;
            Firebase.Auth.FirebaseAuth.DefaultInstance.StateChanged += OnDefaultInstanceOnStateChanged;
#endif
            }
        }

        public static void SignOut()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                FirebaseAuth.SignOut();            
            }
            else
            {
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            Firebase.Auth.FirebaseAuth.DefaultInstance.SignOut();
#endif
            }
        }
    }
}