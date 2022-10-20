using System;
using UnityEngine;

namespace TrickCore
{
    public static class TrickFirebaseAuth
    {
        public static void CreateUserWithEmailAndPassword(string email, string password, Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(CreateUserWithEmailAndPassword), callbackOrFallback, false, email+password);
                FirebaseAuth.CreateUserWithEmailAndPassword(email, password,
                    nameof(FirebaseManager), $"{nameof(CreateUserWithEmailAndPassword)}Callback", $"{nameof(CreateUserWithEmailAndPassword)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            Firebase.Auth.FirebaseAuth.DefaultInstance.CreateUserWithEmailAndPasswordAsync(email, password)
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    // Firebase user has been created.
                    Firebase.Auth.FirebaseUser newUser = task.Result;
                    Debug.LogFormat("Firebase user created successfully: {0} ({1})",
                        newUser.DisplayName, newUser.UserId);

                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((
                        newUser.SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
                });
#endif
        }
    
        public static void SignInWithEmailAndPassword(string email, string password,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(SignInWithEmailAndPassword), callbackOrFallback, false,
                email + password);
            FirebaseAuth.SignInWithEmailAndPassword(email, password,
                nameof(FirebaseManager), $"{nameof(SignInWithEmailAndPassword)}Callback",
                $"{nameof(SignInWithEmailAndPassword)}Fallback");
#endif


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

        public static void OnAuthStateChanged(Action<FirebaseUser> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(OnAuthStateChanged), tuple =>
            {
                callbackOrFallback?.Invoke(tuple.error != null
                    ? null
                    : tuple.content != "{}"
                        ? tuple.content.DeserializeJsonTryCatch<FirebaseUser>()
                        : null);
            }, true);
            FirebaseAuth.OnAuthStateChanged(nameof(FirebaseManager), $"{nameof(OnAuthStateChanged)}Callback",
                $"{nameof(OnAuthStateChanged)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE

            void OnDefaultInstanceOnStateChanged(object sender, EventArgs args)
            {
                if (sender is Firebase.Auth.FirebaseAuth auth)
                {
                    if (auth.CurrentUser != null)
                    {
                        var userJson =
 auth.CurrentUser.SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver);
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

        public static void SignOut()
        {

#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseAuth.SignOut();
#endif


#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            Firebase.Auth.FirebaseAuth.DefaultInstance.SignOut();
#endif

        }
        
    
        public static void ChangePassword(string email, string currentPassword, string newPassword,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {

#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(ChangePassword), callbackOrFallback, false, email);
            FirebaseAuth.ChangePassword(email, currentPassword, newPassword,
                nameof(FirebaseManager), $"{nameof(ChangePassword)}Callback", $"{nameof(ChangePassword)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE

                if (Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null)
                {
                    Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser
                        .ReauthenticateAsync(Firebase.Auth.EmailAuthProvider.GetCredential(email, currentPassword))
                        .ContinueWith(
                            task =>
                            {
                                if (task.IsCanceled || task.IsFaulted)
                                {
                                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                                    return;
                                }
                                
                                Debug.Log("Reauthenticate successful");
                                
                                Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UpdatePasswordAsync(newPassword).ContinueWith(
                                    changePasswordTask =>
                                    {
                                        if (changePasswordTask.IsCanceled || changePasswordTask.IsFaulted)
                                        {
                                            TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((null, FirebaseError.FromException(changePasswordTask.Exception))));
                                            return;
                                        }
                                        Debug.Log("Change password successfully");
                                        TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((new object().SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
                                    });
                            });
                }
#endif

        }
        public static void ForgetPassword(string email, Action<(string content, FirebaseError error)> callbackOrFallback)
        {

#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(ForgetPassword), callbackOrFallback, false, email);
            FirebaseAuth.ForgetPassword(email, nameof(FirebaseManager), $"{nameof(ForgetPassword)}Callback", $"{nameof(ForgetPassword)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            if (Firebase.Auth.FirebaseAuth.DefaultInstance != null)
            {
                Firebase.Auth.FirebaseAuth.DefaultInstance
                    .SendPasswordResetEmailAsync(email)
                    .ContinueWith(
                        task =>
                        {
                            if (task.IsCanceled || task.IsFaulted)
                            {
                                TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                                return;
                            }
                                
                            TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((new object().SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
                        });
            }
#endif

        }
    }
}