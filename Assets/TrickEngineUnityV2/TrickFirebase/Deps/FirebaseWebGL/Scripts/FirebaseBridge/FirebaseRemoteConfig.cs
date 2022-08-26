using System.Runtime.InteropServices;

namespace TrickCore
{
    public static class FirebaseRemoteConfig
    {
        [DllImport("__Internal")]
        public static extern void GetAll(string objectName, string callback, string fallback);

        [DllImport("__Internal")]
        public static extern void FetchAndActivate(string objectName, string callback, string fallback);
    }
}