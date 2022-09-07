mergeInto(LibraryManager.library, {

    FetchAndActivate: function(objectName, callback, fallback) {
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);

        try {
			firebase.remoteConfig().fetchAndActivate().then((b) => {
				unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, b ? "true" : "false");
			  })
			  .catch((error) => {
				unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
			  });

        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
        }
    },

    GetAll: function(objectName, callback, fallback) {
        var parsedObjectName = UTF8ToString(objectName);
        var parsedCallback = UTF8ToString(callback);
        var parsedFallback = UTF8ToString(fallback);

        try {
			try {
				var parameters = firebase.remoteConfig().getAll();
				var arr = {};
				for (var key in parameters) 
				{
					if(parameters.hasOwnProperty(key))
					{
						arr[key] = parameters[key].asString();
					}
				}
				unityInstance.Module.SendMessage(parsedObjectName, parsedCallback, JSON.stringify(arr));
			} catch (error) {
				unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
			}
			
        } catch (error) {
            unityInstance.Module.SendMessage(parsedObjectName, parsedFallback, JSON.stringify(error, Object.getOwnPropertyNames(error)));
        }
    }
});
