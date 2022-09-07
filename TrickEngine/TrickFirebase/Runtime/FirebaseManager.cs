using System;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace TrickCore
{
    public class FirebaseManager : MonoSingleton<FirebaseManager>
    {
        public static DefaultContractResolver FirebaseContractResolver { get; } = new DefaultContractResolver()
        {
            IgnoreSerializableAttribute = false,
            NamingStrategy = new CamelCaseNamingStrategy()
        };
        
        /// <summary>
        /// True if firebase is initialized, on some platform/devices it takes some time.
        /// </summary>
        public bool Initialized { get; set; }
        
        /// <summary>
        /// Event whenever the user state changes. The user 'null' means Logout.
        /// </summary>
        public UnityEvent<FirebaseUser> UserAuthStateChangedEvent { get; } = new UnityEvent<FirebaseUser>();
        
        private readonly Dictionary<string, Queue<Action<(string content, FirebaseError error)>>> _callbackQueueDict = new Dictionary<string, Queue<Action<(string content, FirebaseError error)>>>();
        private readonly Dictionary<string, Action<(string content, FirebaseError error)>> _callbackPersistentDict = new Dictionary<string, Action<(string content, FirebaseError error)>>();
        private readonly List<Action> _initializeCallback = new List<Action>();

        public void OnInitialize(Action action)
        {
            if (Initialized)
            {
                action?.Invoke();
                return;
            }
            _initializeCallback.Add(action);
        }
    
        protected override void Start()
        {
            base.Start();
            
            JsonExtensions.Converters.RemoveAll(converter => converter.GetType() == typeof(IsoDateTimeConverter));
            JsonExtensions.Converters.Insert(0, new FirebaseTimestampJsonConverter());
        
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available) {
                // Set a flag here to indicate whether Firebase is ready to use by your app.
                TrickFirebaseAuth.OnAuthStateChanged(FirebaseAuthStateChanged);
                _initializeCallback.ForEach(action => action?.Invoke());
                _initializeCallback.Clear();
                Initialized = true;
            } else {
                UnityEngine.Debug.LogError(System.String.Format(
                    "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });
#else
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                TrickFirebaseAuth.OnAuthStateChanged(FirebaseAuthStateChanged);
                _initializeCallback.ForEach(action => action?.Invoke());
                _initializeCallback.Clear();
                Initialized = true;
            }
#endif
        }

        public void FirebaseAuthStateChanged(FirebaseUser obj)
        {
            Debug.Log(obj);
            TrickEngine.SimpleDispatch(() =>
            {
                UserAuthStateChangedEvent?.Invoke(obj);
            });
        }

        public void Register(string method, Action<(string content, FirebaseError error)> callbackOrFallback, bool persistent, string persistentId = "")
        {
            if (persistent)
            {
                _callbackPersistentDict.AddOrReplace(method + persistentId, callbackOrFallback);
            }
            else
            {
                if (!_callbackQueueDict.TryGetValue(method + persistentId, out var queue))
                    _callbackQueueDict.Add(method + persistentId, queue = new Queue<Action<(string content, FirebaseError error)>>());
                queue.Enqueue(callbackOrFallback);
            }
        }

        /// <summary>
        /// Performs the invocation of the SendMessage from the JS side
        /// </summary>
        /// <param name="b"></param>
        /// <param name="persistent"></param>
        /// <param name="method"></param>
        /// <param name="s"></param>
        private void Exec(bool b, bool persistent, string method, string s)
        {
            const string persistentIdSeparator = "|*$|";
            var split = s.Split(persistentIdSeparator);
            int length = split.Length;
            string persistentId = length == 2 ? split[1] : string.Empty;
            string content = length == 1 ? s : split[0];
            if (!string.IsNullOrEmpty(persistentId)) Debug.Log($"Exec: {method}{persistentId}");
            if (persistent)
            {
                if (!_callbackPersistentDict.TryGetValue(method + persistentId, out var p)) return;
                p?.Invoke(b ? (content, null) : (content, content.DeserializeJsonTryCatch<FirebaseError>() ?? new FirebaseError()));
            }
            else
            {
                if (!_callbackQueueDict.TryGetValue(method + persistentId, out var queue)) return;
                if (queue.Count <= 0) return;
                var p = queue.Dequeue();
                p?.Invoke(b ? (content, null) : (content, content.DeserializeJsonTryCatch<FirebaseError>() ?? new FirebaseError()));
            }
        }

        #region AUTH
        [Preserve] public void CreateUserWithEmailAndPasswordCallback(string s) => Exec(true, false, nameof(CreateUserWithEmailAndPasswordCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void CreateUserWithEmailAndPasswordFallback(string s) => Exec(false, false, nameof(CreateUserWithEmailAndPasswordFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void SignInWithEmailAndPasswordCallback(string s) => Exec(true, false, nameof(SignInWithEmailAndPasswordCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void SignInWithEmailAndPasswordFallback(string s) => Exec(false, false, nameof(SignInWithEmailAndPasswordFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void OnAuthStateChangedCallback(string s) => Exec(true, true, nameof(OnAuthStateChangedCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void OnAuthStateChangedFallback(string s) => Exec(false, true, nameof(OnAuthStateChangedFallback).Replace("Fallback", string.Empty), s);
    
        #endregion
    
        #region REMOTE CONFIG
        [Preserve] public void FetchAndActivateCallback(string s) => Exec(true, false, nameof(FetchAndActivateCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void FetchAndActivateFallback(string s) => Exec(true, false, nameof(FetchAndActivateFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void GetAllCallback(string s) => Exec(true, false, nameof(GetAllCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void GetAllFallback(string s) => Exec(true, false, nameof(GetAllFallback).Replace("Fallback", string.Empty), s);
        #endregion
    
        #region STORAGE
    
        [Preserve] public void UploadFileCallback(string s) => Exec(true, false, nameof(UploadFileCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void UploadFileFallback(string s) => Exec(true, false, nameof(UploadFileFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void DownloadFileCallback(string s) => Exec(true, false, nameof(DownloadFileCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void DownloadFileFallback(string s) => Exec(true, false, nameof(DownloadFileFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void DeleteFileCallback(string s) => Exec(true, false, nameof(DeleteFileCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void DeleteFileFallback(string s) => Exec(true, false, nameof(DeleteFileFallback).Replace("Fallback", string.Empty), s);

        #endregion
        #region FUNCTIONS
        [Preserve] public void CallCloudFunctionJavaCallback(string s) => Exec(true, false, nameof(CallCloudFunctionJavaCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void CallCloudFunctionJavaFallback(string s) => Exec(true, false, nameof(CallCloudFunctionJavaFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void CallCloudFunctionArgsJavaCallback(string s) => Exec(true, false, nameof(CallCloudFunctionArgsJavaCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void CallCloudFunctionArgsJavaFallback(string s) => Exec(true, false, nameof(CallCloudFunctionArgsJavaFallback).Replace("Fallback", string.Empty), s);
        #endregion
    
        #region FIRESTORE
        [Preserve] public void GetDocumentCallback(string s) => Exec(true, false, nameof(GetDocumentCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void GetDocumentFallback(string s) => Exec(false, false, nameof(GetDocumentFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void GetDocumentsInCollectionCallback(string s) => Exec(true, false, nameof(GetDocumentsInCollectionCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void GetDocumentsInCollectionFallback(string s) => Exec(false, false, nameof(GetDocumentsInCollectionFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void SetDocumentCallback(string s) => Exec(true, false, nameof(SetDocumentCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void SetDocumentFallback(string s) => Exec(false, false, nameof(SetDocumentFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void AddDocumentCallback(string s) => Exec(true, false, nameof(AddDocumentCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void AddDocumentFallback(string s) => Exec(false, false, nameof(AddDocumentFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void UpdateDocumentCallback(string s) => Exec(true, false, nameof(UpdateDocumentCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void UpdateDocumentFallback(string s) => Exec(false, false, nameof(UpdateDocumentFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void DeleteDocumentCallback(string s) => Exec(true, false, nameof(DeleteDocumentCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void DeleteDocumentFallback(string s) => Exec(false, false, nameof(DeleteDocumentFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void DeleteFieldCallback(string s) => Exec(true, false, nameof(DeleteFieldCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void DeleteFieldFallback(string s) => Exec(false, false, nameof(DeleteFieldFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void AddElementInArrayFieldCallback(string s) => Exec(true, false, nameof(AddElementInArrayFieldCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void AddElementInArrayFieldFallback(string s) => Exec(false, false, nameof(AddElementInArrayFieldFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void RemoveElementInArrayFieldCallback(string s) => Exec(true, false, nameof(RemoveElementInArrayFieldCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void RemoveElementInArrayFieldFallback(string s) => Exec(false, false, nameof(RemoveElementInArrayFieldFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void IncrementFieldValueCallback(string s) => Exec(true, false, nameof(IncrementFieldValueCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void IncrementFieldValueFallback(string s) => Exec(false, false, nameof(IncrementFieldValueFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void ListenForDocumentChangeCallback(string s) => Exec(true, true, nameof(ListenForDocumentChangeCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void ListenForDocumentChangeFallback(string s) => Exec(false, true, nameof(ListenForDocumentChangeFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void StopListeningForDocumentChangeCallback(string s) => Exec(true, false, nameof(StopListeningForDocumentChangeCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void StopListeningForDocumentChangeFallback(string s) => Exec(false, false, nameof(StopListeningForDocumentChangeFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void ListenForCollectionChangeCallback(string s) => Exec(true, true, nameof(ListenForCollectionChangeCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void ListenForCollectionChangeFallback(string s) => Exec(false, true, nameof(ListenForCollectionChangeFallback).Replace("Fallback", string.Empty), s);
        [Preserve] public void StopListeningForCollectionChangeCallback(string s) => Exec(true, false, nameof(StopListeningForCollectionChangeCallback).Replace("Callback", string.Empty), s);
        [Preserve] public void StopListeningForCollectionChangeFallback(string s) => Exec(false, false, nameof(StopListeningForCollectionChangeFallback).Replace("Fallback", string.Empty), s);

        #endregion
    }

    public static class FireBaseManagerExtensions
    {
        public static bool HasError(this (string content, FirebaseError error) tuple, bool checkContentForFirebaseError = false)
        {
            if (tuple.error != null) return true;

            if (checkContentForFirebaseError)
            {
                var firebaseError = tuple.content.DeserializeJsonTryCatch<FirebaseError>();
                if (firebaseError != null)
                {
                    if (!string.IsNullOrEmpty(firebaseError.message) && !string.IsNullOrEmpty(firebaseError.name))
                        return true;
                }
            }

            return false;
        }

        public static void LogModal(this (string content, FirebaseError error) tuple, bool checkContentForFirebaseError = false)
        {
            if (tuple.error != null)
            {
                ModalPopupMenu.ShowError(tuple.error.ToString());
            }
            else
            {
                if (checkContentForFirebaseError)
                {
                    var firebaseError = tuple.content.DeserializeJsonTryCatch<FirebaseError>();
                    if (firebaseError != null)
                    {
                        if (!string.IsNullOrEmpty(firebaseError.message) && !string.IsNullOrEmpty(firebaseError.name))
                        {
                            ModalPopupMenu.ShowError(firebaseError.ToString());
                            return;
                        }
                    }
                }
            
                ModalPopupMenu.ShowOkModal("Debug", tuple.content, "ok", null);
            }
        }

        public static void LogConsole(this (string content, FirebaseError error) tuple, string appendWith = "")
        {
            if (tuple.error != null) Debug.LogError($"{(string.IsNullOrEmpty(appendWith) ? string.Empty : $"[{appendWith}]: ")}{tuple.error}");
            else Debug.Log($"{(string.IsNullOrEmpty(appendWith) ? string.Empty : $"[{appendWith}]: ")}{tuple.content}");
        }
    }
}