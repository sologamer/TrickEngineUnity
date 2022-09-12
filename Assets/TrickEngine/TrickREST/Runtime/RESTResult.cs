#if !NO_UNITY
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace TrickCore
{
    
    public struct UWRData
    {
        public bool IsNetworkError;
        public bool IsEditor;
        public bool IsDebugBuild;
        
        public string Error;
        public string DownloadHandlerText;
        public Dictionary<string, string> ResponseHeaders;

        public UWRData(UnityWebRequest uwr)
        {
            IsNetworkError = uwr.result == UnityWebRequest.Result.ConnectionError;
            Error = uwr.error;
            IsEditor = Application.isEditor;
            IsDebugBuild = Debug.isDebugBuild;

            DownloadHandlerText = uwr.downloadHandler.text;
            ResponseHeaders = uwr.GetResponseHeaders();
        }
    }

    public sealed class RESTResult<T> : IRESTResult
    {
        public T Value { get; }
        public string Error;
        public bool HasError { get; }

        public RESTResult(UWRData uwr, RequestFailHandler failHandler)
        {
            if (uwr.IsNetworkError)
            {
                if (failHandler != null)
                {
                    switch (failHandler.Type)
                    {
                        case RequestFailHandler.FailType.Ok:
                            RESTBase.CustomShowOkModal?.Invoke("Error", uwr.Error, failHandler.OkText, failHandler.OkAction);
                            break;
                        case RequestFailHandler.FailType.YesNo:
                            RESTBase.CustomShowYesNoModal?.Invoke("Error", uwr.Error, failHandler.YesText, failHandler.NoText, failHandler.YesAction, failHandler.NoAction);
                            break;
                    }
                }

                Value = default;
                Error = uwr.Error;
                HasError = true;
                return;
            }

            if (typeof(T) == typeof(string))
            {
                string responseData = uwr.DownloadHandlerText;

                if (RESTBase.CustomResponseHeader != null)
                {
                    responseData = RESTBase.CustomResponseHeader?.Invoke(responseData, uwr.ResponseHeaders);
                }
                
                Value = (T) (object) responseData;
            }
            else if (typeof(T).IsPrimitive)
            {
                string responseData = uwr.DownloadHandlerText;

                if (RESTBase.CustomResponseHeader != null)
                {
                    responseData = RESTBase.CustomResponseHeader?.Invoke(responseData, uwr.ResponseHeaders);
                }

                if (long.TryParse(responseData, out var l))
                {
                    var conv = Convert.ChangeType(l, typeof(T));
                    Value = conv is T conv1 ? conv1 : default;
                }
                else if (int.TryParse(responseData, out var i))
                {
                    var conv = Convert.ChangeType(i, typeof(T));
                    Value = conv is T conv1 ? conv1 : default;
                }
            }
            else
            {
                if (uwr.DownloadHandlerText == null || uwr.DownloadHandlerText == "null")
                {
                    if (uwr.IsEditor || uwr.IsDebugBuild) Logger.Core.Log(uwr.DownloadHandlerText);
                    Value = default;
                    return;
                }

                try
                {
                    string responseData = uwr.DownloadHandlerText;

                    if (RESTBase.CustomResponseHeader != null)
                    {
                        responseData = RESTBase.CustomResponseHeader?.Invoke(responseData, uwr.ResponseHeaders);
                    }

                    if (uwr.IsEditor || uwr.IsDebugBuild) Logger.Core.Log(responseData);

                    if (responseData == null) responseData = string.Empty;
                    
                    // Make sure it's not an array
                    if (responseData.Length <= 0 || responseData[0] != '[' ||
                        responseData[responseData.Length - 1] != ']')
                    {
                        RestResponse defaultResponse;
                        if (string.IsNullOrEmpty(responseData))
                            defaultResponse = null;
                        else
                        {
                            if (responseData.StartsWith(
                                "<?xml version=\"1.0\" encoding=\"utf-8\"?><!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\"><!-- msdeploy:"))
                            {
                                defaultResponse = new RestResponse()
                                {
                                    Code = 404,
                                    Message = "Server is under maintenance."
                                };
                            }
                            else
                            {
                                defaultResponse = long.TryParse(responseData, out var p)
                                    ? new RestResponse()
                                    {
                                        ResponseData = responseData,
                                    }
                                    : responseData.DeserializeJson<RestResponse>();
                            }
                        }

                        if (defaultResponse != null && defaultResponse.HasError)
                        {
                            if (failHandler != null)
                            {
                                if (failHandler.ExecGlobalResponse)
                                    RESTBase.CustomExecuteGlobalResponse?.Invoke(defaultResponse, false)?.Invoke();
                                
                                switch (failHandler.Type)
                                {
                                    case RequestFailHandler.FailType.Ok:
                                        RESTBase.CustomShowOkModal?.Invoke("Error", defaultResponse.GetDisplayMessage(),
                                            failHandler.OkText, () =>
                                            {
                                                failHandler.OkAction?.Invoke();
                                                if (failHandler.ExecGlobalResponse)
                                                    RESTBase.CustomExecuteGlobalResponse?.Invoke(defaultResponse, true)?.Invoke();
                                            });
                                        break;
                                    case RequestFailHandler.FailType.YesNo:
                                        RESTBase.CustomShowYesNoModal?.Invoke("Error", defaultResponse.GetDisplayMessage(),
                                            failHandler.YesText, failHandler.NoText, () =>
                                            {
                                                failHandler.YesAction?.Invoke();
                                                if (failHandler.ExecGlobalResponse)
                                                    RESTBase.CustomExecuteGlobalResponse?.Invoke(defaultResponse, true)?.Invoke();
                                            }, () =>
                                            {
                                                failHandler.NoAction?.Invoke();
                                                if (failHandler.ExecGlobalResponse)
                                                    RESTBase.CustomExecuteGlobalResponse?.Invoke(defaultResponse, true)?.Invoke();
                                            });
                                        break;
                                }
                            }
                            else
                            {
                                RESTBase.CustomExecuteGlobalResponse?.Invoke(defaultResponse, false)?.Invoke();
                                
                                RESTBase.CustomShowOkModal?.Invoke("Error", defaultResponse.GetDisplayMessage(), "Ok",
                                    () => RESTBase.CustomExecuteGlobalResponse?.Invoke(defaultResponse, true)?.Invoke());
                            }

                            Error = defaultResponse.Message;
                            HasError = true;
                            Value = default;

                            defaultResponse.LogTrace();
                            return;
                        }
                    }

                    if (typeof(T) == typeof(RestResponse) && long.TryParse(responseData, out var l))
                    {
                        Value = (T) (object) new RestResponse()
                        {
                            Message = responseData
                        };
                    }
                    else
                    {
                        Value = responseData.DeserializeJson<T>();
                    }
                }
                catch (Exception e)
                {
                    Logger.Core.LogWarning("Text length: " + uwr.DownloadHandlerText.Length);
                    Logger.Core.LogWarning("Text: " + uwr.DownloadHandlerText);
                    Logger.Core.LogError("RestResult error (" + this + ")");
                    Logger.Core.LogException(e);

                    var defaultResponse = new RestResponse()
                    {
                        Message = e.Message,
                        Exception = e,
                    };

                    // We have an error
                    if (failHandler != null)
                    {
                        if (failHandler.ExecGlobalResponse)
                            RESTBase.CustomExecuteGlobalResponse?.Invoke(defaultResponse, false)?.Invoke();
                        
                        switch (failHandler.Type)
                        {
                            case RequestFailHandler.FailType.Ok:
                                RESTBase.CustomShowOkModal?.Invoke("Error", defaultResponse.GetDisplayMessage(),
                                    failHandler.OkText, () =>
                                    {
                                        failHandler.OkAction?.Invoke();
                                        if (failHandler.ExecGlobalResponse)
                                            RESTBase.CustomExecuteGlobalResponse?.Invoke(defaultResponse, true)?.Invoke();
                                    });
                                break;
                            case RequestFailHandler.FailType.YesNo:
                                RESTBase.CustomShowYesNoModal?.Invoke("Error", defaultResponse.GetDisplayMessage(),
                                    failHandler.YesText, failHandler.NoText, () =>
                                    {
                                        failHandler.YesAction?.Invoke();
                                        if (failHandler.ExecGlobalResponse)
                                            RESTBase.CustomExecuteGlobalResponse?.Invoke(defaultResponse, true)?.Invoke();
                                    }, () =>
                                    {
                                        failHandler.NoAction?.Invoke();
                                        if (failHandler.ExecGlobalResponse)
                                            RESTBase.CustomExecuteGlobalResponse?.Invoke(defaultResponse, true)?.Invoke();
                                    });
                                break;
                        }
                    }
                    else
                    {
                        RESTBase.CustomExecuteGlobalResponse?.Invoke(defaultResponse, false)?.Invoke();
                        
                        RESTBase.CustomShowOkModal?.Invoke("Error", defaultResponse.GetDisplayMessage(), "Ok",
                            () => RESTBase.CustomExecuteGlobalResponse?.Invoke(defaultResponse, true)?.Invoke());
                    }
                }

                if (Value is RestResponse restResponse)
                {
                    restResponse.ResponseData = uwr.DownloadHandlerText;
                }
            }
        }

        public object GetResult()
        {
            return Value;
        }

        public T1 GetResultAs<T1>()
        {
            return Value is T1 ? (T1) (object) Value : default;
        }
    }
    public interface IRESTResult
    {
        object GetResult();
        T GetResultAs<T>();
    }
}
#endif