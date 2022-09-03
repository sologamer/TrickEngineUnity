mergeInto(LibraryManager.library, {

    SignInAnonymously: function (objectName, callback, fallback) {
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);

        try {
            firebase.auth().signInAnonymously().then(function (result) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, JSON.stringify(result.user));
            }).catch(function (error) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
            });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
        }
    },
    
    CreateUserWithEmailAndPassword: function (email, password, objectName, callback, fallback) {
        var parsedEmail = UTF8ToString(email);
        var parsedPassword = UTF8ToString(password);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = parsedEmail+parsedPassword;

        try {

            firebase.auth().createUserWithEmailAndPassword(parsedEmail, parsedPassword).then(function (unused) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, JSON.stringify(unused.user)+persistentId);
            }).catch(function (error) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
            });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    },

    SignInWithEmailAndPassword: function (email, password, objectName, callback, fallback) {
        var parsedEmail = UTF8ToString(email);
        var parsedPassword = UTF8ToString(password);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = parsedEmail+parsedPassword;
        try {

            firebase.auth().signInWithEmailAndPassword(parsedEmail, parsedPassword).then(function (unused) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, JSON.stringify(unused.user)+persistentId);
            }).catch(function (error) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
            });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    },

    ChangePassword: function (email, currentPassword, newPassword, objectName, callback, fallback) {
        var parsedEmail = UTF8ToString(email);
        var parsedCurrentPassword = UTF8ToString(currentPassword);
        var parsedNewPassword = UTF8ToString(newPassword);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = parsedEmail;
        try {
			var user = firebase.auth().currentUser;
			var credential = firebase.auth.EmailAuthProvider.credential(
				parsedEmail, 
				parsedCurrentPassword
			);
			user.reauthenticateWithCredential(credential).then(function (authUser) {
                var reAuthUser = authUser.user;
				if (reAuthUser != null)
				{
					reAuthUser.updatePassword(parsedNewPassword).then(function () {
						unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, "{}"+persistentId);
					}).catch(function (error) {
						unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
					});
				}
				else
				{
					var error = {};
					error.message = "Failed to authenticate";
					unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
				}
			}).catch(function (error) {
				unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
			});

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    },

    SignInWithGoogle: function (objectName, callback, fallback) {
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);

        try {
            var provider = new firebase.auth.GoogleAuthProvider();
            firebase.auth().signInWithRedirect(provider).then(function (unused) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, JSON.stringify(unused.user));
            }).catch(function (error) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
            });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
        }
    },

    SignInWithFacebook: function (objectName, callback, fallback) {
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);

        try {
            var provider = new firebase.auth.FacebookAuthProvider();
            firebase.auth().signInWithRedirect(provider).then(function (unused) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, JSON.stringify(unused.user));
            }).catch(function (error) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
            });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
        }
    },

    OnAuthStateChanged: function (objectName, onUserSignedIn, onUserSignedOut) {
        var parsedObjectName = UTF8ToString(objectName);
        var parsedOnUserSignedIn = UTF8ToString(onUserSignedIn);
        var parsedOnUserSignedOut = UTF8ToString(onUserSignedOut);
        firebase.auth().onAuthStateChanged(function(user) {
            if (user) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedOnUserSignedIn, JSON.stringify(user));
            } else {
                unityInstance.Module.SendMessage(parsedObjectName, parsedOnUserSignedOut, "{}");
            }
        });

    },

    SignOut: function () {
        firebase.auth().signOut();
    }
});
