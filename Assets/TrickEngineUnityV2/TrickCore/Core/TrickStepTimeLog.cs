using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

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