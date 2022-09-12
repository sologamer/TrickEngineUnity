#if !NO_UNITY
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace TrickCore
{
    [Serializable]
    public sealed class RestResponse
    {
        [JsonProperty(PropertyName = "code")] public int Code;
        [JsonProperty(PropertyName = "message")] public string Message;
        [JsonProperty(PropertyName = "exception")] public string JsonException;
        [JsonProperty(PropertyName = "trace")] public Dictionary<string,object>[] Trace;

        [JsonProperty(PropertyName = "responseData")] public string ResponseData;
        public Exception Exception { get; set; }
        public bool HasError => Code >= 300;

        public void LogTrace()
        {
            if (Application.isEditor)
            {
                Debug.LogError($"REST: {Message}");
            }
            else
            {
                string str = $"REST: {Message}";
                int indexOf = str.IndexOf(" (SQL", StringComparison.Ordinal);
                Debug.LogError(indexOf != -1 ? str.Substring(0, indexOf) : str);
            }

            if (Trace != null)
            {
                int index = 0;
                foreach (Dictionary<string, object> o in Trace)
                {
                    object file = "";
                    if (o.ContainsKey("file")) file = o["file"];
                    object line = "";
                    if (o.ContainsKey("line")) line = o["line"].ToString();
                    object function = "";
                    if (o.ContainsKey("function")) function = o["function"];

                    Debug.LogError($"REST: #{index} - {function} {file}{(!Equals(line, "") ? $":{line}" : "")}");
                    index++;
                }
            }
        }

        public string GetDisplayMessage()
        {
            var str = string.IsNullOrEmpty(Message) ? GetMessageResponse() : Message;
            int indexOf = str.IndexOf(" (SQL", StringComparison.Ordinal);
            return indexOf != -1 ? str.Substring(0, indexOf) : str;
        }

        private string GetMessageResponse()
        {
            switch (Code)
            {
                // 300 codes
                case 300:
                    return "Multiple Choices";
                case 301:
                    return "Moved Permanently";
                case 302:
                    return @"Found (Previously ""Moved temporarily"")";
                case 303:
                    return "See Other (since HTTP/1.1)";
                case 304:
                    return "Not Modified";
                case 305:
                    return "Use Proxy (since HTTP/1.1)";
                case 306:
                    return "Switch Proxy";
                case 307:
                    return "Temporary Redirect";
                case 308:
                    return "Permanent Redirect";

                // 400 codes
                case 400:
                    return "Bad Request";
                case 401:
                    return "Unauthorized";
                case 402:
                    return "Payment Required";
                case 403:
                    return "Forbidden";
                case 404:
                    return "Not Found";
                case 405:
                    return "Method Not Allowed";
                case 406:
                    return "Not Acceptable";
                case 407:
                    return "Proxy Authentication Required";
                case 408:
                    return "Request Timeout";
                case 409:
                    return "Conflict";
                case 410:
                    return "Gone";
                case 411:
                    return "Length Required";
                case 412:
                    return "Precondition Failed";
                case 413:
                    return "Payload Too Large";
                case 414:
                    return "URI Too Long";
                case 415:
                    return "Unsupported Media Type";
                case 416:
                    return "Range Not Satisfiable";
                case 417:
                    return "Expectation Failed";
                case 418:
                    return "I'm a teapot";
                case 421:
                    return "Misdirected Request";
                case 422:
                    return "Unprocessable Entity";
                case 423:
                    return "Locked";
                case 424:
                    return "Failed Dependency";
                case 425:
                    return "Too Early";
                case 426:
                    return "Upgrade Required";
                case 428:
                    return "Precondition Required";
                case 429:
                    return "Too Many Requests";
                case 431:
                    return "Request Header Fields Too Large";
                case 451:
                    return "Unavailable For Legal Reasons";

                // code 500

                case 500:
                    return "Internal Server Error";
                case 501:
                    return "Not Implemented";
                case 502:
                    return "Bad Gateway";
                case 503:
                    return "Server is unavailable";
                case 504:
                    return "Gateway Timeout";
                case 505:
                    return "HTTP Version Not Supported";
                case 506:
                    return "Variant Also Negotiates";
                case 507:
                    return "Insufficient Storage";
                case 508:
                    return "Loop Detected";
                case 510:
                    return "Not Extended";
                case 511:
                    return "Network Authentication Required";
            }

            return "";
        }
    }

    [Serializable]
    public class RESTTrace
    {
        [JsonProperty(PropertyName = "file")]
        public string File;
        [JsonProperty(PropertyName = "line")]
        public string Line;
        [JsonProperty(PropertyName = "function")]
        public string Function;
        [JsonProperty(PropertyName = "class")]
        public string ClassName;
        [JsonProperty(PropertyName = "type")]
        public string Type;
        [JsonProperty(PropertyName = "args")]
        public Dictionary<string,object> Args;
    }
}
#endif