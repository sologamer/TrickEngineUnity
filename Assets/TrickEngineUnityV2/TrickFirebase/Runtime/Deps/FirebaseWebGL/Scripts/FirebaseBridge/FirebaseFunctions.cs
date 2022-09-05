using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Proyecto26;

namespace TrickCore
{
    public static class FirebaseFunctions
    {
        /// <summary>
        /// Calls an HTTP Firebase cloud function
        /// Returns the response of the request 
        /// </summary>
        /// <param name="functionName"> Name of the function to call </param>
        /// <param name="parameters"> Possible function parameters </param>
        /// <param name="callback"> Method to call when the operation was successful </param>
        /// <param name="fallback"> Method to call when the operation was unsuccessful </param>
        public static void CallCloudFunction(string functionName, Dictionary<string, string> parameters,
            Action<ResponseHelper> callback,
            Action<Exception> fallback)
        {
            var projectId = GetCurrentProjectId();
            RestClient.Request(new RequestHelper
            {
                Method = "POST",
                Uri = $"https://{TrickFirebaseFunctions.Region}-{projectId}.cloudfunctions.net/{functionName}",
                Headers = new Dictionary<string, string>
                {
                    {"Access-Control-Allow-Origin", "*"}
                },
                Params = parameters
            }).Then(callback).Catch(fallback);
        }

        [DllImport("__Internal")]
        public static extern void CallCloudFunctionJava(string region, string functionName,
            string objectName, string callback,
            string fallback);
        
        [DllImport("__Internal")]
        public static extern void CallCloudFunctionArgsJava(string region, string functionName, string args,
            string objectName, string callback,
            string fallback);

        [DllImport("__Internal")]
        private static extern string GetCurrentProjectId();
    }
}