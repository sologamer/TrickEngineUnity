#if !NO_UNITY
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace TrickCore
{
    public sealed class WaitForThreadedTask : WaitForThreadedTask<int>
    {
        public WaitForThreadedTask(Action subTask, TimeSpan timeout) : base(subTask, timeout)
        {
        }
    }
    
    public class WaitForThreadedTask<TResult> : CustomYieldInstruction
    {
        private bool _wait;
        private readonly Task<Task> _task;
        private readonly Stopwatch _sw;
        private readonly TimeSpan _timeout;
        private readonly Stopwatch _pausedStopWatch;
        public override bool keepWaiting
        {
            get
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPaused)
                {
                    if (!_pausedStopWatch.IsRunning) _pausedStopWatch.Start();
                }
                else
                {
                    if (_pausedStopWatch.IsRunning) _pausedStopWatch.Stop();
                }
#endif
                
                return _wait && (_task != null && _task.Status < TaskStatus.RanToCompletion) && _sw.Elapsed < _timeout + _pausedStopWatch.Elapsed;
            }
        }

        public bool IsTimedOut => _sw.Elapsed >= _timeout + _pausedStopWatch.Elapsed;
        public Exception CurrentException { get; set; }

        public WaitForThreadedTask(Action subTask, TimeSpan timeout, bool useUnityTrickTask = false)
        {
            _wait = true;
            _timeout = timeout;
            _sw = Stopwatch.StartNew();
            _pausedStopWatch = new Stopwatch();
            try
            {
                if (useUnityTrickTask)
                {
                    _task = UnityTrickTask.StartNewTask(async () =>
                    {
                        try
                        {
                            subTask?.Invoke();
                        }
                        catch (Exception e)
                        {
                            Logger.Game.LogException(e);
                            CurrentException = e;
                        }
                        await Task.CompletedTask;
                        _wait = false;
                    }, TaskCreationOptions.PreferFairness);
                    if (_task == null) _wait = false;
                }
                else
                {
                    _task = TrickTask.StartNewTask(async () =>
                    {
                        try
                        {
                            subTask?.Invoke();
                        }
                        catch (Exception e)
                        {
                            Logger.Game.LogException(e);
                            CurrentException = e;
                        }
                        await Task.CompletedTask;
                        _wait = false;
                    }, TaskCreationOptions.PreferFairness);
                    if (_task == null) _wait = false;
                }
            }
            catch (Exception e)
            {
                Logger.Game.LogException(e);
                CurrentException = e;
                _wait = false;
            }
        }
    }
}
#endif