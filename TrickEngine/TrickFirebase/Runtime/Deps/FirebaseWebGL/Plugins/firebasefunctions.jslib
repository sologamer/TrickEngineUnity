mergeInto(LibraryManager.library, {

    GetCurrentProjectId: function () {
        var projectId = firebaseConfig.projectId;
        var bufferSize = lengthBytesUTF8(projectId) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(projectId, buffer, bufferSize);
        return buffer;
    },
	
	CallCloudFunctionJava: function(region, functionName, objectName, callback, fallback) {
        var parsedRegion = UTF8ToString(region);
        var parsedFunctionName = UTF8ToString(functionName);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedFunctionName;

        try {
			
			try {
				firebase.app().functions(parsedRegion).httpsCallable(parsedFunctionName)().then(res => {
					unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, JSON.stringify(res.data)+persistentId);
				}).catch((error) => {
				unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
			  });
			} catch (error) {
				unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
			}
        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    },
	
	CallCloudFunctionArgsJava: function(region, functionName, args, objectName, callback, fallback) {
        var parsedRegion = UTF8ToString(region);
        var parsedFunctionName = UTF8ToString(functionName);
        var parsedArgs = UTF8ToString(args);
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);
		var persistentId = "|*$|" + parsedFunctionName;
		
        try {
			
			try {
				firebase.app().functions(parsedRegion).httpsCallable(parsedFunctionName)(JSON.parse(parsedArgs)).then(res => {
					unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, JSON.stringify(res.data)+persistentId);
				}).catch((error) => {
				unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
			  });
			} catch (error) {
				unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
			}
        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error))+persistentId);
        }
    }

});