using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

/// <summary>
/// A disposable stopwatch time logger, only logs inside the editor. Logs when object is Disposed
/// using (new TrickStepTimeLog("Name of the action to log"))
/// {
///     // actions to stopwatch
/// }
/// </summary>
public class TrickStepTimeLog : IDisposable
{
    private Stopwatch _sw;
    private string _type;

    public TrickStepTimeLog(string type)
    {
#if UNITY_EDITOR
        _type = type;
        _sw = Stopwatch.StartNew();
#endif
    }

    public void Dispose()
    {
#if UNITY_EDITOR
        Debug.Log($"[Step] '{_type}' took {_sw.Elapsed.TotalMilliseconds}ms");
#endif
    }
}