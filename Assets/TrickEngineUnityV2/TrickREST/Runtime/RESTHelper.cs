#if !NO_UNITY
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace TrickCore
{
    public class RESTSettings
    {
        public string Protocol = "https://";
        public Func<string> BaseUrl = () => "example.com";
        public string ServerConfigName = string.Empty; // "s1", "s2" etc...
        public bool ServerConfigSubdomain;
        public string Session = null;
        public string Token = null;

        public string GetUrl()
        {
            if (string.IsNullOrEmpty(ServerConfigName))
                return $"{Protocol}{BaseUrl?.Invoke()}";

            if (ServerConfigSubdomain)
                return $"{Protocol}{ServerConfigName}.{BaseUrl?.Invoke()}/";
            return $"{Protocol}{BaseUrl?.Invoke()}/{ServerConfigName}";
        }
        
        public string GetData => string.IsNullOrEmpty(Session) ? string.Empty : $"ssid={Session}";
        public KeyValuePair<string, string>? PostData => string.IsNullOrEmpty(Session) ? (KeyValuePair<string, string>?) null : new KeyValuePair<string, string>("ssid", Session);
    }
    public static class RESTHelper
    {
        public static readonly RESTSettings Settings = new RESTSettings();

        public static IEnumerator GetAsync<T>(string uri, KeyValuePair<string, string>[] param, RequestFailHandler failHandler, Action<RESTResult<T>> onCallback)
        {
            uri = Settings.GetUrl() + (uri.StartsWith("/") ? uri : $"/{uri}");
            if (param == null) param = new KeyValuePair<string, string>[0];
            if (param.Length > 0)
            {
                uri += "?" + string.Join("&", param.Select(pair => $"{pair.Key}={pair.Value}"));

                KeyValuePair<string, string>? postData = Settings.PostData;
                if (postData != null) uri += $"&{postData.Value.Key}={postData.Value.Value}";
            }
            else
            {
                KeyValuePair<string, string>? postData = Settings.PostData;
                if (postData != null) uri += $"?{postData.Value.Key}={postData.Value.Value}";
            }

            if (Application.isEditor || Debug.isDebugBuild) 
                Logger.Core.Log($"GET: {uri}");

            using (var uwr = UnityWebRequest.Get(uri))
            {
                uwr.timeout = 30;
                var op = uwr.SendWebRequest();
                yield return new WaitUntil(() => op.isDone);
                onCallback?.Invoke(new RESTResult<T>(new UWRData(uwr), failHandler));
            }
        }

        public static IEnumerator PostAsync<T>(string uri, KeyValuePair<string, string>[] param, RequestFailHandler failHandler, Action<RESTResult<T>> onCallback)
        {
            uri = Settings.GetUrl() + (uri.StartsWith("/") ? uri : $"/{uri}");
            
            WWWForm form = new WWWForm();
            if (param == null) param = new KeyValuePair<string, string>[0];
            foreach (KeyValuePair<string, string> pair in param)
            {
                if (pair.Key != null && pair.Value != null) form.AddField(pair.Key, pair.Value);
            }

            KeyValuePair<string, string>? postData = Settings.PostData;
            if(postData?.Key != null && postData.Value.Value != null) form.AddField(postData.Value.Key, postData.Value.Value);

            if (Application.isEditor || Debug.isDebugBuild) Debug.Log($"POST: {uri} (data length={form.data.Length})");

            using (var uwr = UnityWebRequest.Post(uri, form))
            {
                uwr.timeout = 15;
                yield return uwr.SendWebRequest();
                onCallback?.Invoke(new RESTResult<T>(new UWRData(uwr), failHandler));
            }
        }

    }
}
#endif