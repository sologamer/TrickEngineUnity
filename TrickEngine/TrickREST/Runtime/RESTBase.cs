#if !NO_UNITY
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TrickCore
{
    /// <summary>
    /// REST helper, also supports ZLIB/LZ4/ZSTD
    /// </summary>
    public class RESTBase : CustomYieldInstruction
    {
        /// <summary>
        /// True if it's waiting for it to complete
        /// </summary>
        protected bool Wait;
        
        /// <summary>
        /// The enumerator we iterate on
        /// </summary>
        public IEnumerator Enumerator;
        
        public override bool keepWaiting { get; }


        #region REST Hooks (events)

        public static Func<int> CustomFetchUserId { get; set; }
        public static Action CustomStartRequestHook { get; set; }
        public static Action CustomEndRequestHook { get; set; }
        public static Func<bool> CustomUseThreadsForRequestsHook { get; set; }
        public static Func<TimeSpan> CustomThreadTimeoutHook { get; set; }
        public static Action<IEnumerator> CustomStartCoroutineHook { get; set; }

        #endregion

        #region REST Custom Response handling (events)
        
        public static Func<string, Dictionary<string,string>, string> CustomResponseHeader { get; set; } = DefaultResponseHeader;
        public static Action<string, string, string, Action> CustomShowOkModal { get; set; } = DefaultShowOkModal;
        public static Action<string, string, string, string, Action, Action> CustomShowYesNoModal { get; set; } = DefaultShowYesNoModal;
        public static Func<RestResponse, bool, Action> CustomExecuteGlobalResponse { get; set; } = DefaultCustomGlobalResponse;

        #endregion
        
        private static string DefaultResponseHeader(string responseData, Dictionary<string, string> responseHeaders)
        {  
#if ENABLE_ZLIB
            if (responseHeaders != null && responseHeaders.TryGetValue("zlib", out var zlibValue) && zlibValue == "1")
            {
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                var decoded = System.Text.Encoding.UTF8.GetString(responseData.ZLibDecodeBase64());
                var ms = (float) sw.Elapsed.TotalMilliseconds;
                Logger.Core.Log($"[ZLIB ({ms}ms)] server: {responseData.Length} / decoded: {decoded.Length}");
                responseData = decoded;
            }
#endif
                
#if ENABLE_LZ4
            if (responseHeaders != null && responseHeaders.TryGetValue("lz4", out var lz4Value) && lz4Value == "1")
            {
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                var decoded = System.Text.Encoding.UTF8.GetString(responseData.LZ4DecodeBase64());
                var ms = (float) sw.Elapsed.TotalMilliseconds;
                Logger.Core.Log($"[LZ4 ({ms}ms)] server: {responseData.Length} / decoded: {decoded.Length}");
                responseData = decoded;
            }
#endif
            
#if ENABLE_ZSTD
            if (responseHeaders != null && responseHeaders.TryGetValue("zstd", out var zstdValue) && zstdValue == "1")
            {
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                var decoded = System.Text.Encoding.UTF8.GetString(responseData.ZstdDecodeBase64());
                var ms = (float) sw.Elapsed.TotalMilliseconds;
                Logger.Core.Log($"[ZSTD ({ms}ms)] server: {responseData.Length} / decoded: {decoded.Length}");
                responseData = decoded;
            }
#endif

            return responseData;
        }


        private static Action DefaultCustomGlobalResponse(RestResponse restResponse, bool fromModal)
        {
            // Here we can do custom actions whenever we receive a message from the REST server.
            
            // if (restResponse.Message.StartsWith("Invalid login"))
            // {
            //     return GameManager.Instance.Logout;
            // }
            //
            // if (restResponse.Message.StartsWith("There is a new version available"))
            // {
            //     return () =>
            //     {
            //         UIManager.Instance.GetMenu<DGHMainMenu>().Show();
            //         Application.OpenURL($"https://{GameManager.BaseUrl}/game/app/{GameManager.Instance.GetPlatform()}");
            //     };
            // }

            return null;
        }

        public static void DefaultShowOkModal(string title, string description, string failHandlerOkText, Action failHandlerOkAction)
        {
            //ModalPopupMenu.ShowOkModal(title, description, failHandlerOkText, failHandlerOkAction);
        }

        public static void DefaultShowYesNoModal(string title, string description, string failHandlerYesText, string failHandlerNoText, Action failHandlerYesAction, Action failHandlerNoAction)
        {
            //ModalPopupMenu.ShowYesNoModal(title, description, failHandlerYesText, failHandlerNoText, failHandlerYesAction, failHandlerNoAction);
        }
    }
}
#endif

#if NO_UNITY
public class CustomYieldInstruction
{
    public virtual bool keepWaiting => false;
}
#endif