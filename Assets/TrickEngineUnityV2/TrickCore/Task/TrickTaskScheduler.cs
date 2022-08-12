using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TrickCore
{
    public sealed class TrickTaskScheduler : TaskScheduler, IDisposable
    {
        #region variarables

        public static readonly List<TrickTaskScheduler> TaskSchedulers = new List<TrickTaskScheduler>();
        public int QueueCount => _taskQueue.Count;
        public bool IsExecutingTask { get; private set; }

        public static bool AllowCleanupSchedulers = false;
        public static double AutoCleanupTime = 20.0;

        private readonly BlockingCollection<Task> _taskQueue = new BlockingCollection<Task>();
        private readonly Thread _thread;
        private volatile bool _disposed;

        private DateTime? _inactiveTime;

        public int SchedulerId { get; set; }

        private static int _incrId = 0;


        #endregion

        public TrickTaskScheduler(CancellationToken token)
        {
            SchedulerId = ++_incrId;
            _thread = new Thread(() => Run(token))
            {
                Name = $"TrickTaskScheduler-{SchedulerId}",
                IsBackground = true
            };
            _thread.Start();
            lock (TaskSchedulers) TaskSchedulers.Add(this);
        }

        private void Run(CancellationToken token)
        {
            try
            {
                while (!_disposed)
                {
                    if (_taskQueue.Count > 0)
                    {
                        try
                        {
                            Task task = _taskQueue.Take(token);
                            // Work executed on the main thread
                            TrickEngine.SimpleDispatch(() =>
                            {
                                IsExecutingTask = true;

                                try
                                {
                                    if (TryExecuteTask(task))
                                    {
                                        _inactiveTime = DateTime.Now.AddSeconds(AutoCleanupTime);
                                    }
                                }
                                catch (ThreadAbortException)
                                {
                                    // ignore thread abort ex
                                }
                                catch (Exception ex)
                                {
                                    // ignore
                                    Logger.Core.LogException(ex);
                                }

                                IsExecutingTask = false;
                            });
                        }
                        catch (ThreadAbortException)
                        {
                            // ignore thread abort ex
                        }
                        catch (Exception ex)
                        {
                            // ignore
                            Logger.Core.LogException(ex);
                        }
                    }
                    else
                    {
                        if (AllowCleanupSchedulers)
                        {
                            // Doing nothing
                            if (!IsExecutingTask && _inactiveTime != null)
                            {
                                if (DateTime.Now > _inactiveTime)
                                {
                                    // Time to shutdown this scheduler
                                    Dispose();
                                    break;
                                }
                            }

                        }

                        Thread.Sleep(1);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                // ignore thread abort ex
            }
            catch (Exception e)
            {
                Logger.Core.LogException(e);
            }
        }

        protected override void QueueTask(Task task)
        {
            if (_disposed) return;
            _taskQueue.TryAdd(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (Thread.CurrentThread == _thread)
            {
                TrickEngine.SimpleDispatch(() =>
                {
                    try
                    {
                        if (TryExecuteTask(task))
                        {
                            _inactiveTime = DateTime.Now.AddSeconds(AutoCleanupTime);
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        // ignore thread abort ex
                    }
                    catch (Exception ex)
                    {
                        // ignore
                        Logger.Core.LogException(ex);
                    }
                });
                return true;
            }
            return false;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _taskQueue;
        }

        public void Dispose()
        {
            _taskQueue?.Dispose();
            _disposed = true;

            lock (TaskSchedulers) TaskSchedulers.Remove(this);
        }

        internal static void Cleanup()
        {
            lock (TaskSchedulers)
            {
                if (TaskSchedulers.Count > 0)
                {
                    Logger.Core.LogInfo($"Cleaning up {TaskSchedulers.Count} task schedulers.");
                }
                
                for (int i = TaskSchedulers.Count - 1; i >= 0; i--)
                {
                    // Directly dispose
                    TaskSchedulers[i]._taskQueue?.Dispose();
                    TaskSchedulers[i]._disposed = true;

                    try
                    {
                        var thread = TaskSchedulers[i]._thread;
                        thread?.Abort();
                    }
                    catch (Exception ex)
                    {
                        //
                        Logger.Core.LogException(ex);
                    }
                }

                TaskSchedulers.Clear();
            }
        }
    }
}