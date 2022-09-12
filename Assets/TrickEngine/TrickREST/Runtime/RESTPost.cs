#if !NO_UNITY
using System;
using System.Collections;
using System.Collections.Generic;
using Proyecto26;
using TrickCore;
using UnityEngine;
using UnityEngine.Events;

namespace TrickCore
{
    public sealed class RESTPost : RESTPost<object>
    {
        public RESTPost(string uri, KeyValuePair<string, string>[] param) : base(uri, param)
        {
        }
    }

    public class RESTPost<T> : RESTBase
    {
        public override bool keepWaiting => Wait;

        public RESTPost(string uri, KeyValuePair<string, string>[] param = null, Action<T> onCallback = null, RequestFailHandler failHandler = null)
        {
            Wait = true;
            WWWForm form = new WWWForm();
            if (param == null) param = new KeyValuePair<string, string>[0];
            foreach (KeyValuePair<string, string> pair in param)
            {
                if (pair.Key != null && pair.Value != null) form.AddField(pair.Key, pair.Value);
                else
                {
                    Debug.LogException(new ApplicationException($"RESTPost key/value null (uri={uri}, key={pair.Key})"));
                }
            }

            KeyValuePair<string, string>? postData = RESTHelper.Settings.PostData;
            if(postData?.Key != null && postData.Value.Value != null) form.AddField(postData.Value.Key, postData.Value.Value);

            CustomStartRequestHook?.Invoke();
            var helper = new RequestHelper
            {
                Uri = RESTHelper.Settings.GetUrl() + (uri.StartsWith("/") ? uri : $"/{uri}"),
                FormData = form,
                Method = "POST",
                Timeout = 15,
                Retries = 1, //Number of retries
                RetrySecondsDelay = 2, //Seconds of delay to make a retry
                RetryCallback = (err, retries) => { }, //See the error before retrying the request
                EnableDebug = Application.isEditor, //See logs of the requests for debug mode
                IgnoreHttpException = true, //Prevent to catch http exceptions
                UseHttpContinue = true,
                RedirectLimit = 32,
                DefaultContentType = false, //Disable JSON content type by default
                ParseResponseBody = false,
                //CertificateHandler = new RESTCertificateHandler()
            };
            if (!string.IsNullOrEmpty(RESTHelper.Settings.Token))
            {
                helper.Headers = new Dictionary<string, string>
                {
                    {"Authorization", $"Bearer {RESTHelper.Settings.Token}"}
                };
            }
            RestClient.Request(helper).Then(response =>
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

    public class RequestFailHandler
    {
        public enum FailType
        {
            Ok,
            YesNo
        }

        public FailType Type;
        public Action OkAction;
        public Action YesAction;
        public Action NoAction;
        public string OkText = "Ok";
        public string YesText = "Yes";
        public string NoText = "No";

        public bool ExecGlobalResponse { get; set; }

        public RequestFailHandler(Action okAction, bool executeGlobalResponse)
        {
            Type = FailType.Ok;
            OkAction = okAction;
            ExecGlobalResponse = executeGlobalResponse;
        }

        public RequestFailHandler(Action yesAction, Action noAction, bool executeGlobalResponse)
        {
            Type = FailType.YesNo;
            YesAction = yesAction;
            NoAction = noAction;
            ExecGlobalResponse = executeGlobalResponse;
        }
    }
}
#endif