namespace TrickCore
{
    public sealed class TrickUnityLogger : LogTarget
    {
        public TrickUnityLogger()
        {
            DefaultLog += UnityEngine.Debug.Log;
            InfoLog += UnityEngine.Debug.Log;
            WarningLog += UnityEngine.Debug.LogWarning;
            ErrorLog += UnityEngine.Debug.LogError;

            DefaultLogFormat += (format, arguments) => UnityEngine.Debug.LogFormat(format != null ? format.ToString() : string.Empty, arguments);
            InfoLogFormat += (format, arguments) => UnityEngine.Debug.LogFormat(format != null ? format.ToString() : string.Empty, arguments);
            WarningLogFormat += (format, arguments) => UnityEngine.Debug.LogWarningFormat(format != null ? format.ToString() : string.Empty, arguments);
            ErrorLogFormat += (format, arguments) => UnityEngine.Debug.LogErrorFormat(format != null ? format.ToString() : string.Empty, arguments);
        
            ExceptionLog += UnityEngine.Debug.LogException;
        }
    }
}