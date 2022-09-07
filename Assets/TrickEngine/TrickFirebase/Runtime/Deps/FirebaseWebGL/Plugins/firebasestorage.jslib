mergeInto(LibraryManager.library, {

    UploadFile: function(path, data, objectName, callback, fallback) {

        var parsedPath = UTF8ToString(path);
        var parsedData = UTF8ToString(data);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath;

        try {

            firebase.storage().ref(parsedPath).put(base64ToArrayBuffer(parsedData)).then(function(snapshot) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, parsedPath+persistentId);
            });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }

        function base64ToArrayBuffer(base64) {
            var binary_string = window.atob(base64);
            var len = binary_string.length;
            var bytes = new Uint8Array(len);
            for (var i = 0; i < len; i++) {
                bytes[i] = binary_string.charCodeAt(i);
            }
            return bytes.buffer;
        }
    },

    DownloadFile: function(path, objectName, callback, fallback) {

        var parsedPath = UTF8ToString(path);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath;

        try {

            firebase.storage().ref(parsedPath).getDownloadURL().then(function(url) {
                
                var xhr = new XMLHttpRequest();
                xhr.responseType = 'arraybuffer';
                xhr.onload = function(event) {
                  var data = xhr.response;
                  unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, arrayBufferToBase64(data)+persistentId);
                };
                xhr.open('GET', url);
                xhr.send();
              
              }).catch(function(error) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
              });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }

        function arrayBufferToBase64( buffer ) {
            var binary = '';
            var bytes = new Uint8Array( buffer );
            var len = bytes.byteLength;
            for (var i = 0; i < len; i++) {
                binary += String.fromCharCode( bytes[ i ] );
            }
            return window.btoa( binary );
        }
    },

    DeleteFile: function(path, objectName, callback, fallback) {

        var parsedPath = UTF8ToString(path);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedPath;

        try {

            firebase.storage().ref(parsedPath).delete().then(function() {
                  unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, "{}"+persistentId);
              }).catch(function(error) {
                unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
              });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    }
});
