using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
public static class TrickWebGLFunctions
{
    [DllImport("__Internal")]
    public static extern void ReloadPage();
}
#endif
