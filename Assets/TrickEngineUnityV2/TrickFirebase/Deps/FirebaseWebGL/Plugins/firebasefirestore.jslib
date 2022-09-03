mergeInto(LibraryManager.library, {

    GetDocument: function (collectionPath, documentId, objectName, callback, fallback) {
        var parsedPath = UTF8ToString(collectionPath);
        var parsedId = UTF8ToString(documentId);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath+parsedId;

        try {
            firebase.firestore().collection(parsedPath).doc(parsedId).get().then(function (doc) {

                if (doc.exists) {
                    unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, JSON.stringify(fixDateTimeObject(doc.data()))+persistentId);
                } else {
                    unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, "{}"+persistentId);
                }
            }).catch(function(error) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
            });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
		
		function fixDateTimeObject(obj) {
			var fixedObj = {};
			for (var key in obj) 
			{
				if(obj.hasOwnProperty(key))
				{
					var pValue = obj[key];
					if(pValue.hasOwnProperty("seconds") && pValue.hasOwnProperty("nanoseconds"))
					{
						fixedObj[key] = pValue.toDate().toISOString();
					}
					else
					{
						fixedObj[key] = pValue;
					}
				}
			}
			return fixedObj;
		}
    },

    GetDocumentsInCollection: function (collectionPath, objectName, callback, fallback) {
        var parsedPath = UTF8ToString(collectionPath);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath;

        try {
            firebase.firestore().collection(parsedPath).get().then(function (querySnapshot) {

                var docs = {};
                querySnapshot.forEach(function(doc) {
					docs[doc.id] = fixDateTimeObject(doc.data());
                });

                unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, JSON.stringify(docs)+persistentId);
            }).catch(function(error) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
            });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
		
		function fixDateTimeObject(obj) {
			var fixedObj = {};
			for (var key in obj) 
			{
				if(obj.hasOwnProperty(key))
				{
					var pValue = obj[key];
					if(pValue.hasOwnProperty("seconds") && pValue.hasOwnProperty("nanoseconds"))
					{
						fixedObj[key] = pValue.toDate().toISOString();
					}
					else
					{
						fixedObj[key] = pValue;
					}
				}
			}
			return fixedObj;
		}
    },

    SetDocument: function (collectionPath, documentId, value, objectName, callback, fallback) {
        var parsedPath = UTF8ToString(collectionPath);
        var parsedId = UTF8ToString(documentId);
        var parsedValue = UTF8ToString(value);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath+parsedId;

        try {

            firebase.firestore().collection(parsedPath).doc(parsedId).set(JSON.parse(parsedValue)).then(function() {
                unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, "{}"+persistentId);
            })
                .catch(function(error) {
                    unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
                });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    },

    AddDocument: function (collectionPath, value, objectName, callback, fallback) {
        var parsedPath = UTF8ToString(collectionPath);
        var parsedValue = UTF8ToString(value);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + collectionPath;

        try {

            firebase.firestore().collection(parsedPath).add(JSON.parse(parsedValue)).then(function(unused) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, parsedPath + "/" + unused.id+persistentId);
            })
                .catch(function(error) {
                    unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
                });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    },

    UpdateDocument: function (collectionPath, documentId, value, objectName, callback, fallback) {
        var parsedPath = UTF8ToString(collectionPath);
        var parsedId = UTF8ToString(documentId);
        var parsedValue = UTF8ToString(value);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath+parsedId;

        try {

            firebase.firestore().collection(parsedPath).doc(parsedId).update(JSON.parse(parsedValue)).then(function() {
                unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, "{}"+persistentId);
            })
                .catch(function(error) {
                    unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
                });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    },

    DeleteDocument: function (collectionPath, documentId, objectName, callback, fallback) {
        var parsedPath = UTF8ToString(collectionPath);
        var parsedId = UTF8ToString(documentId);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath+parsedId;
		
        try {

            firebase.firestore().collection(parsedPath).doc(parsedId).delete().then(function() {
                unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, "{}"+persistentId);
            })
                .catch(function(error) {
                    unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
                });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    },

    GetField: function (collectionPath, documentId, field, objectName, callback, fallback) {
        var parsedPath = UTF8ToString(collectionPath);
        var parsedId = UTF8ToString(documentId);
        var parsedField = UTF8ToString(field);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath+parsedId+parsedField;

        try {
			firebase.firestore().collection(parsedPath).get().then(function (querySnapshot) {

                var docs = {};
				var map = querySnapshot.docs.first.data();
				docs["data"] = map.get(parsedField);

                unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, JSON.stringify(docs)+persistentId);
            }).catch(function(error) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
            });
        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    },

    DeleteField: function (collectionPath, documentId, field, objectName, callback, fallback) {
        var parsedPath = UTF8ToString(collectionPath);
        var parsedId = UTF8ToString(documentId);
        var parsedField = UTF8ToString(field);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath+parsedId+parsedField;

        try {

            var value = {};
            value[parsedField] = firebase.firestore.FieldValue.delete();

            firebase.firestore().collection(parsedPath).doc(parsedId).update(value).then(function() {
                unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, "{}"+persistentId);
            })
                .catch(function(error) {
                    unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
                });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    },

    AddElementInArrayField: function (collectionPath, documentId, field, value, objectName, callback, fallback) {
        var parsedPath = UTF8ToString(collectionPath);
        var parsedId = UTF8ToString(documentId);
        var parsedField = UTF8ToString(field);
        var parsedValue = UTF8ToString(value);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath+parsedId+parsedField;

        try {

            var value = {};
            value[parsedField] = firebase.firestore.FieldValue.arrayUnion(JSON.parse(parsedValue));

            firebase.firestore().collection(parsedPath).doc(parsedId).update(value).then(function() {
                unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, "{}"+persistentId);
            })
                .catch(function(error) {
                    unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
                });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    },

    RemoveElementInArrayField: function (collectionPath, documentId, field, value, objectName, callback, fallback) {
        var parsedPath = UTF8ToString(collectionPath);
        var parsedId = UTF8ToString(documentId);
        var parsedField = UTF8ToString(field);
        var parsedValue = UTF8ToString(value);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath+parsedId+parsedField;

        try {

            var value = {};
            value[parsedField] = firebase.firestore.FieldValue.arrayRemove(JSON.parse(parsedValue));

            firebase.firestore().collection(parsedPath).doc(parsedId).update(value).then(function() {
                unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, "{}"+persistentId);
            })
                .catch(function(error) {
                    unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
                });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    },

    IncrementFieldValue: function (collectionPath, documentId, field, increment, objectName, callback, fallback) {
        var parsedPath = UTF8ToString(collectionPath);
        var parsedId = UTF8ToString(documentId);
        var parsedField = UTF8ToString(field);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath+parsedId+parsedField;

        try {

            var value = {};
            value[parsedField] = firebase.firestore.FieldValue.increment(increment);

            firebase.firestore().collection(parsedPath).doc(parsedId).update(value).then(function() {
                unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, "{}"+persistentId);
            })
                .catch(function(error) {
                    unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
                });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    },

    ListenForDocumentChange: function (collectionPath, documentId, includeMetadataChanges, objectName, callback, fallback) {
        var parsedPath = UTF8ToString(collectionPath);
        var parsedId = UTF8ToString(documentId);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath+parsedId+(includeMetadataChanges == 1 ? "True" : "False");
		try {

            if (typeof firestorelisteners === 'undefined') firestorelisteners = {};
            this.firestorelisteners[parsedPath + "/" + parsedId]();
        } catch (error) {
		
        }
		
        try {

            if (typeof firestorelisteners === 'undefined') firestorelisteners = {};

            this.firestorelisteners[parsedPath + "/" + parsedId] = firebase.firestore().collection(parsedPath).doc(parsedId)
                .onSnapshot({
                    includeMetadataChanges: (includeMetadataChanges == 1)
                }, function(doc) {
					if (doc.exists)
					{
						unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, JSON.stringify(fixDateTimeObject(doc.data()))+persistentId);
					}
					else
					{
						console.log("unsub because doc doesn't exists: " + parsedPath + "/" + parsedId);
						firestorelisteners[parsedPath + "/" + parsedId]();
					}
                }, function(error) {
                    unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
                });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
		
		function fixDateTimeObject(obj) {
			var fixedObj = {};
			for (var key in obj) 
			{
				if(obj.hasOwnProperty(key))
				{
					var pValue = obj[key];
					if(pValue.hasOwnProperty("seconds") && pValue.hasOwnProperty("nanoseconds"))
					{
						fixedObj[key] = pValue.toDate().toISOString();
					}
					else
					{
						fixedObj[key] = pValue;
					}
				}
			}
			return fixedObj;
		}
    },

    StopListeningForDocumentChange: function (collectionPath, documentId, objectName, callback, fallback) {
        var parsedPath = UTF8ToString(collectionPath);
        var parsedId = UTF8ToString(documentId);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath+parsedId;

        try {

            if (typeof firestorelisteners === 'undefined') firestorelisteners = {};

            this.firestorelisteners[parsedPath + "/" + parsedId]();
            unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, "{}"+persistentId);
        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    },

    ListenForCollectionChange: function (collectionPath, includeMetadataChanges, objectName, callback, fallback) {
        var parsedPath = UTF8ToString(collectionPath);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath+(includeMetadataChanges == 1 ? "True" : "False");
		
		try {
            if (typeof firestorelisteners === 'undefined') firestorelisteners = {};
            this.firestorelisteners[parsedPath + "/collection/"]();
        } catch (error) {
		
        }
		
        try {

            if (typeof firestorelisteners === 'undefined') firestorelisteners = {};

            this.firestorelisteners[parsedPath + "/collection/"] = firebase.firestore().collection(parsedPath)
                .onSnapshot({
                    includeMetadataChanges: (includeMetadataChanges == 1)
                }, function(querySnapshot) {

                    var docs = {};
                    querySnapshot.forEach(function(doc) {
						docs[doc.id] = fixDateTimeObject(doc.data());
                    });

                    unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, JSON.stringify(docs)+persistentId);

                }, function(error) {
                    unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
                });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
		
		
		function fixDateTimeObject(obj) {
			var fixedObj = {};
			for (var key in obj) 
			{
				if(obj.hasOwnProperty(key))
				{
					var pValue = obj[key];
					if(pValue.hasOwnProperty("seconds") && pValue.hasOwnProperty("nanoseconds"))
					{
						fixedObj[key] = pValue.toDate().toISOString();
					}
					else
					{
						fixedObj[key] = pValue;
					}
				}
			}
			return fixedObj;
		}
    },

    StopListeningForCollectionChange: function (collectionPath, objectName, callback, fallback) {
        var parsedPath = UTF8ToString(collectionPath);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath;

        try {

            if (typeof firestorelisteners === 'undefined') firestorelisteners = {};

            this.firestorelisteners[parsedPath + "/collection/"]();
            unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, "{}"+persistentId);
        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    }

});