#if !NO_UNITY
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Proyecto26;
using UnityEngine;
using UnityEngine.Networking;

namespace TrickCore
{
    public class RESTDelete : RESTDelete<int>
    {
        public RESTDelete(string uri, KeyValuePair<string, string>[] param = null) : base(uri, param)
        {
            
        }
    }
    
    public class RESTDelete<T> : RESTBase
    {
        public override bool keepWaiting => Wait;

        public RESTDelete(string uri, KeyValuePair<string, string>[] param = null, Action<T> onCallback = null, RequestFailHandler failHandler = null)
        {
            CustomStartRequestHook?.Invoke();
            
            uri = RESTHelper.Settings.GetUrl() + (uri.StartsWith("/") ? uri : $"/{uri}");
            if (param == null) param = new KeyValuePair<string, string>[0];
            if (param.Length > 0)
            {
                uri += "?" + string.Join("&", param.Select(pair => $"{pair.Key}={pair.Value}"));

                KeyValuePair<string, string>? postData = RESTHelper.Settings.PostData;
                if (postData != null) uri += $"&{postData.Value.Key}={postData.Value.Value}";
            }
            else
            {
                KeyValuePair<string, string>? postData = RESTHelper.Settings.PostData;
                if (postData != null) uri += $"?{postData.Value.Key}={postData.Value.Value}";
            }

            Wait = true;
            
            RestClient.Request(new RequestHelper()
            {
                Uri = uri,
                Method = "DELETE",
                Timeout = 15,
                Retries = 1, //Number of retries
                RetrySecondsDelay = 2, //Seconds of delay to make a retry
                RetryCallback = (err, retries) => { }, //See the error before retrying the request
                EnableDebug = Application.isEditor, //See logs of the requests for debug mode
                IgnoreHttpException = true, //Prevent to catch http exceptions
                UseHttpContinue = true,
                RedirectLimit = 32,
                DefaultContentType = false, //Disable JSON content type by default
                ParseResponseBody = false //Don't encode and parse downloaded data as JSON
            }).Then(response =>
            {
                var uwrData = new UWRData(response.Request);
                if (CustomUseThreadsForRequestsHook?.Invoke() ?? true)
                {
                    HandleRestResultWithThreads(uwrData, onCallback, failHandler);
                }
                else
                {
                    HandleRestResultWithoutThreads(uwrData, onCallback, failHandler);
                }
            }).Catch(err => {
                if (failHandler != null)
                {
                    switch (failHandler.Type)
                    {
                        case RequestFailHandler.FailType.Ok:
                            CustomShowOkModal?.Invoke("Error", err.Message, failHandler.OkText, failHandler.OkAction);
                            break;
                        case RequestFailHandler.FailType.YesNo:
                            CustomShowYesNoModal?.Invoke("Error", err.Message, failHandler.YesText, failHandler.NoText, failHandler.YesAction, failHandler.NoAction);
                            break;
                    }
                }
                
                Debug.LogException(err);
                Wait = false;
            });
        }

        private void HandleRestResultWithThreads(UWRData uwrData, Action<T> onCallback, RequestFailHandler failHandler)
        {
            IEnumerator Work()
            {
                WaitForThreadedTask threadedJob;
                RESTResult<T> result = null;
                yield return threadedJob = new WaitForThreadedTask(() =>
                {
                    result = new RESTResult<T>(uwrData, failHandler);
                }, CustomThreadTimeoutHook?.Invoke() ?? TimeSpan.FromSeconds(5.0f));
                if (result != null)
                {
                    onCallback?.Invoke(result.Value);
                    CustomEndRequestHook?.Invoke();
                    Wait = false;
                }
                // If we have never done the job (Wait=true), and we are t/o or had an exception. Fallback to non-threads
                if (Wait && (threadedJob.IsTimedOut || threadedJob.CurrentException != null))
                {
                    HandleRestResultWithoutThreads(uwrData, onCallback, failHandler);
                }
            }
            
            CustomStartCoroutineHook?.Invoke(Work());
        }

        private void HandleRestResultWithoutThreads(UWRData uwrData, Action<T> onCallback,
            RequestFailHandler failHandler)
        {
            var result = new RESTResult<T>(uwrData, failHandler);
            onCallback?.Invoke(result.Value);
            CustomEndRequestHook?.Invoke();
            Wait = false;
        }
    }
    
    
}
#endif