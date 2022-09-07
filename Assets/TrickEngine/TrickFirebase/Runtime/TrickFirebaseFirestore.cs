using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrickCore
{
    public static class TrickFirebaseFirestore
    {
        public static void GetDocument(string collectionPath, string documentId,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("[GetDocument]: " + collectionPath + "/" + documentId);
            FirebaseManager.Instance.Register(nameof(GetDocument), callbackOrFallback, false,
                collectionPath + documentId);
            FirebaseFirestore.GetDocument(collectionPath, documentId,
                nameof(FirebaseManager), $"{nameof(GetDocument)}Callback", $"{nameof(GetDocument)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            Firebase.Firestore.FirebaseFirestore.DefaultInstance.Document($"{collectionPath}/{documentId}")
                .GetSnapshotAsync(Firebase.Firestore.Source.Server)
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    Firebase.Firestore.DocumentSnapshot document = task.Result;
                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((
                        document.ToDictionary(Firebase.Firestore.ServerTimestampBehavior.Estimate).SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver),
                        null)));
                });
#endif

        }

        public static void GetDocumentsInCollection(string collectionPath,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(GetDocumentsInCollection), callbackOrFallback, false,
                collectionPath);
            FirebaseFirestore.GetDocumentsInCollection(collectionPath,
                nameof(FirebaseManager), $"{nameof(GetDocumentsInCollection)}Callback",
                $"{nameof(GetDocumentsInCollection)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            Firebase.Firestore.FirebaseFirestore.DefaultInstance.Collection($"{collectionPath}").GetSnapshotAsync(Firebase.Firestore.Source.Server)
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    var querySnapshot = task.Result;
                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((
                        querySnapshot.Documents.ToDictionary(snapshot => snapshot.Id, snapshot => snapshot.ToDictionary()).SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
                });
#endif

        }

        public static void SetDocument(string collectionPath, string documentId, Dictionary<string, object> value,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(SetDocument), callbackOrFallback, false,
                collectionPath + documentId);
            FirebaseFirestore.SetDocument(collectionPath,
                documentId, value.SerializeToJson(false, true), nameof(FirebaseManager),
                $"{nameof(SetDocument)}Callback",
                $"{nameof(SetDocument)}Fallback");
#endif
            
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            Firebase.Firestore.FirebaseFirestore.DefaultInstance.Document($"{collectionPath}/{documentId}").SetAsync(value)
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((new object().SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
                });
#endif

        }

        public static void AddDocument(string collectionPath, Dictionary<string, object> value,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(AddDocument), callbackOrFallback, false, collectionPath);
            FirebaseFirestore.AddDocument(collectionPath,
                value.SerializeToJson(false, true), nameof(FirebaseManager), $"{nameof(AddDocument)}Callback",
                $"{nameof(AddDocument)}Fallback");
#endif
            
#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            Firebase.Firestore.FirebaseFirestore.DefaultInstance.Collection($"{collectionPath}").AddAsync(value)
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((task.Result.Path, null)));
                });
#endif

        }

        public static void UpdateDocument(string collectionPath, string documentId, Dictionary<string,object> value,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(UpdateDocument), callbackOrFallback, false,
                collectionPath + documentId);
            FirebaseFirestore.UpdateDocument(collectionPath,
                documentId, value.SerializeToJson(false, true), nameof(FirebaseManager),
                $"{nameof(UpdateDocument)}Callback", $"{nameof(UpdateDocument)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            Firebase.Firestore.FirebaseFirestore.DefaultInstance.Document($"{collectionPath}/{documentId}").UpdateAsync(value)
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((new object().SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
                });
#endif

        }

        public static void DeleteDocument(string collectionPath, string documentId,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(DeleteDocument), callbackOrFallback, false,
                collectionPath + documentId);
            FirebaseFirestore.DeleteDocument(collectionPath,
                documentId, nameof(FirebaseManager),
                $"{nameof(DeleteDocument)}Callback", $"{nameof(DeleteDocument)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            Firebase.Firestore.FirebaseFirestore.DefaultInstance.Document($"{collectionPath}/{documentId}").DeleteAsync()
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((new object().SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
                });
#endif

        }

        public static void GetField(string collectionPath, string documentId, string field,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(GetField), callbackOrFallback, false,
                collectionPath + documentId + field);
            FirebaseFirestore.GetField(collectionPath,
                documentId, field, nameof(FirebaseManager),
                $"{nameof(GetField)}Callback", $"{nameof(GetField)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
                Debug.Log("GET FIELD");
                
            Firebase.Firestore.FirebaseFirestore.DefaultInstance.Document($"{collectionPath}/{documentId}")
                //.UpdateAsync(value)
                .GetSnapshotAsync(Firebase.Firestore.Source.Server)
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    if (task.Result.TryGetValue<string>(field, out var data))
                    {
                        TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((
                            new
                            {
                                data = data
                            }.SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
                    }
                    else
                    {
                        TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((
                            new object().SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
                    }
                });
#endif

        }

        public static void DeleteField(string collectionPath, string documentId, string field,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(DeleteField), callbackOrFallback, false,
                collectionPath + documentId + field);
            FirebaseFirestore.DeleteField(collectionPath,
                documentId, field, nameof(FirebaseManager),
                $"{nameof(DeleteField)}Callback", $"{nameof(DeleteField)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            var value = new Dictionary<string, object>()
            {
                { field, Firebase.Firestore.FieldValue.Delete }
            };
            Firebase.Firestore.FirebaseFirestore.DefaultInstance.Document($"{collectionPath}/{documentId}")
                //.UpdateAsync(value)
                .UpdateAsync(field, Firebase.Firestore.FieldValue.Delete)
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((new object().SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
                });
#endif

        }

        public static void AddElementInArrayField(string collectionPath, string documentId, string field, string value,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(AddElementInArrayField), callbackOrFallback, false,
                collectionPath + documentId + field);
            FirebaseFirestore.AddElementInArrayField(collectionPath,
                documentId, field, value, nameof(FirebaseManager),
                $"{nameof(AddElementInArrayField)}Callback", $"{nameof(AddElementInArrayField)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            var element = new Dictionary<string, object>()
            {
                { field, Firebase.Firestore.FieldValue.ArrayUnion(value) }
            };
            Firebase.Firestore.FirebaseFirestore.DefaultInstance.Document($"{collectionPath}/{documentId}").UpdateAsync(element)
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((new object().SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
                });
#endif

        }

        public static void RemoveElementInArrayField(string collectionPath, string documentId, string field, string value,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(RemoveElementInArrayField), callbackOrFallback, false,
                collectionPath + documentId + field + value);

            FirebaseFirestore.RemoveElementInArrayField(collectionPath,
                documentId, field, value,
                nameof(FirebaseManager),
                $"{nameof(RemoveElementInArrayField)}Callback", $"{nameof(RemoveElementInArrayField)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            var element = new Dictionary<string, object>()
            {
                { field, Firebase.Firestore.FieldValue.ArrayRemove(value) }
            };
            Firebase.Firestore.FirebaseFirestore.DefaultInstance.Document($"{collectionPath}/{documentId}").UpdateAsync(element)
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((new object().SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
                });
#endif

        }

        public static void IncrementFieldValue(string collectionPath, string documentId, string field, int increment,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(IncrementFieldValue), callbackOrFallback, false,
                collectionPath + documentId + field + increment);

            FirebaseFirestore.IncrementFieldValue(collectionPath,
                documentId, field, increment, nameof(FirebaseManager),
                $"{nameof(IncrementFieldValue)}Callback", $"{nameof(IncrementFieldValue)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            var element = new Dictionary<string, object>()
            {
                { field, Firebase.Firestore.FieldValue.Increment(increment) }
            };
            Firebase.Firestore.FirebaseFirestore.DefaultInstance.Document($"{collectionPath}/{documentId}").UpdateAsync(element)
                .ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        TrickEngine.SimpleDispatch(() =>
                            callbackOrFallback?.Invoke((null, FirebaseError.FromException(task.Exception))));
                        return;
                    }

                    TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((new object().SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
                });
#endif

        }

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
    private static Dictionary<string, Firebase.Firestore.ListenerRegistration> FirebaseListeners = new Dictionary<string, Firebase.Firestore.ListenerRegistration>();
#endif

        public static void ListenForDocumentChange(string collectionPath, string documentId, bool includeMetadataUpdates,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(ListenForDocumentChange), callbackOrFallback, true,
                collectionPath + documentId + includeMetadataUpdates);

            FirebaseFirestore.ListenForDocumentChange(
                collectionPath, documentId, includeMetadataUpdates,
                nameof(FirebaseManager), $"{nameof(ListenForDocumentChange)}Callback",
                $"{nameof(ListenForDocumentChange)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            void Listen(Firebase.Firestore.DocumentSnapshot snapshot)
            {
                TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((
                    snapshot.ToDictionary().SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver),
                    null)));
            }

            var documentReference =
                Firebase.Firestore.FirebaseFirestore.DefaultInstance.Collection(collectionPath).Document($"{documentId}");
            FirebaseListeners[$"Document{collectionPath}/{documentId}"] = documentReference.Listen(
                includeMetadataUpdates
                    ? Firebase.Firestore.MetadataChanges.Include
                    : Firebase.Firestore.MetadataChanges.Exclude, Listen);
#endif

        }

        public static void StopListeningForDocumentChange(string collectionPath, string documentId,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(StopListeningForDocumentChange), callbackOrFallback, false,
                collectionPath + documentId);

            FirebaseFirestore.StopListeningForDocumentChange(collectionPath,
                documentId, nameof(FirebaseManager), $"{nameof(StopListeningForDocumentChange)}Callback",
                $"{nameof(StopListeningForDocumentChange)}Fallback");
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            FirebaseListeners.Remove($"Document{collectionPath}/{documentId}");
            
            TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((new object().SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
#endif

        }

        public static void ListenForCollectionChange(string collectionPath, bool includeMetadataUpdates,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(ListenForCollectionChange), callbackOrFallback, true,
                collectionPath + includeMetadataUpdates);
            FirebaseFirestore.ListenForCollectionChange(
                collectionPath, includeMetadataUpdates, nameof(FirebaseManager),
                $"{nameof(ListenForCollectionChange)}Callback", $"{nameof(ListenForCollectionChange)}Fallback"
            );
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            void Listen(Firebase.Firestore.QuerySnapshot querySnapshot)
            {
                TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((
                    querySnapshot.Documents.ToDictionary(snapshot => snapshot.Id, snapshot => snapshot.ToDictionary(Firebase.Firestore.ServerTimestampBehavior.Estimate)).SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver),
                    null)));
            }

            var collectionReference =
                Firebase.Firestore.FirebaseFirestore.DefaultInstance.Collection($"{collectionPath}");
            FirebaseListeners[$"Collection{collectionPath}"] = collectionReference.Listen(includeMetadataUpdates
                ? Firebase.Firestore.MetadataChanges.Include
                : Firebase.Firestore.MetadataChanges.Exclude, Listen);
#endif

        }
        public static void StopListeningForCollectionChange(string collectionPath,
            Action<(string content, FirebaseError error)> callbackOrFallback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FirebaseManager.Instance.Register(nameof(StopListeningForCollectionChange), callbackOrFallback, false,
                collectionPath);

            FirebaseFirestore.StopListeningForCollectionChange(collectionPath,
                nameof(FirebaseManager), $"{nameof(StopListeningForCollectionChange)}Callback",
                $"{nameof(StopListeningForCollectionChange)}Fallback"
            );
#endif

#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || (!UNITY_EDITOR && !UNITY_WEBGL)) && USE_FIREBASE
            FirebaseListeners.Remove($"Collection{collectionPath}");
            TrickEngine.SimpleDispatch(() => callbackOrFallback?.Invoke((new object().SerializeToJson(true, true, FirebaseManager.FirebaseContractResolver), null)));
#endif

        }
    }
}