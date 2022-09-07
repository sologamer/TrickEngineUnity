using System.Runtime.InteropServices;

namespace TrickCore
{
#if UNITY_WEBGL && !UNITY_EDITOR
    public static class FirebaseRemoteConfig
    {
        [DllImport("__Internal")]
        public static extern void GetAll(string objectName, string callback, string fallback);

        [DllImport("__Internal")]
        public static extern void FetchAndActivate(string objectName, string callback, string fallback);
    }
#endif
}