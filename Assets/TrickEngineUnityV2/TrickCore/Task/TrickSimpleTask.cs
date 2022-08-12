using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace TrickCore
{
    /// <summary>
    /// A simple task class.
    /// </summary>
    public class TrickSimpleTask
    {
        public delegate bool Condition();

        public delegate void Enumerator();

        /// <summary>
        /// Order of waiting
        /// </summary>
        public enum WaitOrderType
        {
            /// <summary>
            /// Unset
            /// </summary>
            Unassigned,

            /// <summary>
            /// First execute the task, then wait for the yield object
            /// </summary>
            TaskYield,

            /// <summary>
            /// First wait for the yield object, then execute the task
            /// </summary>
            YieldTask,
        };

        public enum TaskType
        {
            None,
            While,
            WaitWhile,
            WaitFor,
            WaitForTask,
            ContinueWith,
            Loop
        };

        public class TaskWork
        {
            public TaskType Type;
            public Condition Condition;
            public Enumerator Func;
            public TaskYield Yield;
            public TrickSimpleTask Task;

            public WaitOrderType Waitorder;

            public TaskWork()
            {
                Type = TaskType.None;
                Waitorder = WaitOrderType.Unassigned;
            }

            public override string ToString()
            {
                return $"{nameof(Type)}: {Type}, {nameof(Condition)}: {Condition}, {nameof(Func)}: {Func}, {nameof(Yield)}: {Yield}, {nameof(Task)}: {Task}, {nameof(Waitorder)}: {Waitorder}";
            }
        }

        private static int _taskIdIncr;
        private static readonly List<TrickSimpleTask> Tasks = new List<TrickSimpleTask>();
        private static ConcurrentQueue<KeyValuePair<TrickSimpleTask, bool>> _tasksQueue = new ConcurrentQueue<KeyValuePair<TrickSimpleTask, bool>>();


        public class TaskInfo
        {
            // Task identifier
            public int TaskId;

            // Threaded or not
            public bool Threaded;
            // The thread object
            public Thread ThreadObject;
            // Threading sleep time
            public int SleepTime;

            // The work list
            public readonly List<TaskWork> Workqueue = new List<TaskWork>();
            public int Workindex;

            // Weak reference to its own object
            public TrickSimpleTask Instance;

            /// <summary>
            /// Task scheduled to stop
            /// </summary>
            public bool Stop;

            /// <summary>
            /// Task completed?
            /// </summary>
            public bool Completed;

            /// <summary>
            /// Is the task running?
            /// </summary>
            public bool Running;

            /// <summary>
            /// Exception
            /// </summary>
            public Exception Exception;
        }

        private readonly TaskInfo _info = new TaskInfo();

        /// <summary>
        /// The current work being executed
        /// </summary>
        private TaskWork _current;

        private readonly string _name;

        public TrickSimpleTask(string name)
        {
            _name = name;
        }

        /// <summary>
        /// Add work to this thread
        /// </summary>
        /// <param name="work">Add work</param>
        private void AddWork(TaskWork work)
        {
            _info.Workqueue.Add(work);
        }

        /// <summary>
        /// Update a specific task job
        /// </summary>
        /// <param name="work">The job to update</param>
        private void ExecuteWork(TaskWork work)
        {
            if (work == null) return;

            switch (work.Type)
            {
                case TaskType.None:
                {
                    MoveNext();
                }
                    break;
                case TaskType.While:
                {
                    if (work.Condition != null && work.Condition())
                    {
                        work.Func?.Invoke();
                    }
                    else
                    {
                        MoveNext();
                    }
                }
                    break;
                case TaskType.WaitWhile:
                {
                    // Wait for the yield to finish, then execute our task
                    if (work.Yield == null || work.Yield.Ready())
                    {
                        if (work.Condition != null && work.Condition())
                        {
                            work.Func?.Invoke();
                            work.Yield?.Execute();
                        }
                        else
                        {
                            MoveNext();
                        }
                    }
                }
                    break;
                case TaskType.WaitFor:
                {
                    if (work.Yield != null)
                    {
                        if (work.Yield.Ready())
                        {
                            MoveNext();
                        }
                    }
                    else
                    {
                        MoveNext();
                    }
                }
                    break;
                case TaskType.WaitForTask:
                {
                    if (work.Task != null && !work.Task._info.Completed && !work.Task._info.Stop)
                    {
                        work.Task.ExecuteTask();
                    }
                    else
                    {
                        MoveNext();
                    }
                }
                    break;
                case TaskType.ContinueWith:
                {
                    work.Func?.Invoke();
                    MoveNext();
                }
                    break;
                case TaskType.Loop:
                {
                    _info.Workindex = -1;
                    MoveNext();
                }
                    break;
                default: break;
            }
        }

        /// <summary>
        /// Execute task update, this will update all the tasks work
        /// </summary>
        private void ExecuteTask()
        {
            try
            {
                // If we have an active task, keep on going
                if (_current != null)
                {
                    // Execute this task
                    ExecuteWork(_current);
                }
                else
                {
                    // Check if we need to move to the next task
                    if (MoveNext())
                    {
                        // Execute this task
                        ExecuteWork(_current);
                    }
                    else
                    {
                        _info.Completed = true;
                        _info.Running = false;
                        _info.Stop = true;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Core.LogException(e);

                _info.Exception = e;
                _info.Completed = true;
                _info.Running = false;
                _info.Stop = true;
                _current = null;
            }
        }

        /// <summary>
        /// Move to next task if any
        /// </summary>
        /// <returns>Returns true if there is any work left to do</returns>
        public bool MoveNext()
        {
            try
            {
                if (_info.Workqueue.Count == 0)
                {
                    _current = null;
                    return false;
                }

                if (++_info.Workindex < _info.Workqueue.Count)
                {
                    _current = _info.Workqueue[_info.Workindex];

                    if (_current == null) return false;

                    // Execute the yield on movenext
                    _current.Yield?.Execute();

                    if (_current == null) return false;

                    // Reset the target task
                    if (_current.Type == TaskType.WaitForTask)
                    {
                        // Reset the whole task when yielding another task, this will execute the task from the beginning
                        _current.Task._info.Workindex = -1;
                        _current.Task._info.Stop = false;
                        _current.Task._info.Completed = false;
                        _current.Task._current = null;
                    }
                    else if (_current.Type == TaskType.WaitWhile && _current.Waitorder == WaitOrderType.TaskYield)
                    {
                        _current.Func?.Invoke();
                    }

                    return _current != null;
                }
                _current = null;
                return false;
            }
            catch (Exception e)
            {
                Logger.Core.LogError("_current=" + _current);
                Logger.Core.LogException(e);
                return false;
            }
        }

        internal static void InternalCleanup()
        {
            try
            {
                for (var i = 0; i < Tasks.Count; i++)
                {
                    TrickSimpleTask task = Tasks[i];
                    if (task == null) continue;

                    try
                    {
                        if (task._info.ThreadObject != null)
                        {
                            if (!task._info.ThreadObject.Join(1000))
                                task._info.ThreadObject.Abort();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Core.LogException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Core.LogException(ex);
            }

            Tasks.Clear();
            // --- lock (TasksQueue)
            {
                _tasksQueue = new ConcurrentQueue<KeyValuePair<TrickSimpleTask, bool>>();
            }
        }

        /// <summary>
        /// Update all tasks
        /// </summary>
        internal static void InternalUpdateAllTasks()
        {
            // --- lock (TasksQueue)
            {
                if (!_tasksQueue.IsEmpty)
                {
                    while (_tasksQueue.TryDequeue(out KeyValuePair<TrickSimpleTask, bool> queue))
                    {
                        if (queue.Key != null)
                        {
                            if (queue.Value)
                            {
                                // Add if not exists
                                Tasks.Add(queue.Key);
                            }
                            else
                            {
                                // Remove the task
                                Tasks.Remove(queue.Key);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < Tasks.Count; i++)
            {
                var task = Tasks[i];

                // Check if task completed/stopped
                if (task == null || task._info.Completed || task._info.Stop)
                {
                    Tasks.RemoveAt(i);
                    i--;
                    continue;
                }

                // Check if task is running
                if (!task._info.Running)
                {
                    continue;
                }

                // Update the tasks
                if (task._info.Threaded)
                {
                    // Threaded task, start thread if not exists
                    if (task._info.ThreadObject == null)
                    {
                        task._info.ThreadObject = new Thread(delegate()
                        {
                            while (!task._info.Completed && !task._info.Stop)
                            {
                                task.ExecuteTask();
                                Thread.Sleep(task._info.SleepTime);
                            }
                        })
                        {
                            Name = "TrickSimpleTask",
                            IsBackground = true
                        };

                        task._info.ThreadObject.Start();
                    }
                }
                else
                {
                    // Non threaded task
                    task.ExecuteTask();
                }
            }
        }
        /// <summary>
        /// Start a synchronized task
        /// </summary>
        /// <param name="name">Task name</param>
        /// <returns>The task</returns>
        public static TrickSimpleTask Create(string name)
        {
            var task = new TrickSimpleTask(name);
            task._info.TaskId = ++_taskIdIncr;
            task._info.Threaded = false;
            task._info.Instance = task;
            task._info.Running = false;
            task._info.Workindex = -1;
            return task;
        }

        /// <summary>
        /// Start an asynchronized task
        /// </summary>
        /// <param name="name">Task name</param>
        /// <param name="sleepTime">The time to sleep</param>
        /// <returns>The task</returns>
        public static TrickSimpleTask CreateAsync(string name, int sleepTime = 1)
        {
            var task = new TrickSimpleTask(name);
            task._info.TaskId = ++_taskIdIncr;
            task._info.Threaded = true;
            task._info.SleepTime = sleepTime;
            task._info.Instance = task;
            task._info.Running = false;
            task._info.Workindex = -1;
            return task;
        }

        /// <summary>
        /// Stop a task
        /// </summary>
        /// <param name="task">The task to stop</param>
        public static void StopTask(TrickSimpleTask task)
        {
            task?.Stop();
        }

        /// <summary>
        /// While the condition is true, execute the function.
        /// </summary>
        /// <param name="condition">The condition function check</param>
        /// <param name="func">The function to execute </param>
        /// <returns>the current TaskPtr</returns>
        public TrickSimpleTask While(Condition condition, Enumerator func)
        {
            var work = new TaskWork
            {
                Type = TaskType.While,
                Condition = condition,
                Func = func
            };
            AddWork(work);
            return _info.Instance;
        }

        /// <summary>
        /// While the condition is true, execute the function.
        /// </summary>
        /// <param name="condition">The condition</param>
        /// <param name="func">The function to execute</param>
        /// <param name="taskyield">The yield object to wait for</param>
        /// <param name="waitorder">The order type of which goes first</param>
        /// <returns>The current task</returns>
        public TrickSimpleTask WaitWhile(Condition condition, Enumerator func, TaskYield taskyield, WaitOrderType waitorder)
        {
            var work = new TaskWork
            {
                Type = TaskType.WaitWhile,
                Condition = condition,
                Func = func,
                Yield = taskyield,
                Waitorder = waitorder
            };

            AddWork(work);
            return _info.Instance;
        }

        /// <summary>
        /// Wait for a yield object is completed. For example 'waitforsecond' or 'waituntil'
        /// </summary>
        /// <param name="taskyield">The yield construction</param>
        /// <returns>The current task</returns>
        public TrickSimpleTask WaitFor(TaskYield taskyield)
        {
            if (taskyield == null) return _info.Instance;

            var work = new TaskWork
            {
                Type = TaskType.WaitFor,
                Yield = taskyield
            };

            AddWork(work);
            return _info.Instance;
        }

        /// <summary>
        /// Wait until another task is completed
        /// </summary>
        /// <param name="task">Another task</param>
        /// <returns>The current task</returns>
        public TrickSimpleTask WaitFor(TrickSimpleTask task)
        {
            if (task == null) return _info.Instance;

            var work = new TaskWork
            {
                Type = TaskType.WaitForTask,
                Task = task
            };

            AddWork(work);
            return _info.Instance;
        }

        /// <summary>
        /// Continue with a function
        /// </summary>
        /// <param name="func">The function to execute</param>
        /// <returns>The current task</returns>
        public TrickSimpleTask ContinueWith(Enumerator func)
        {
            var work = new TaskWork
            {
                Type = TaskType.ContinueWith,
                Func = func
            };

            AddWork(work);
            return _info.Instance;
        }

        /// <summary>
        /// Loops the task
        /// </summary>
        /// <returns>The current task</returns>
        public TrickSimpleTask Loop()
        {
            var work = new TaskWork
            {
                Type = TaskType.Loop
            };

            AddWork(work);
            return _info.Instance;
        }

        /// <summary>
        /// Immediately stop the task.
        /// </summary>
        /// <returns>The current task</returns>
        public TrickSimpleTask Stop()
        {
            _current = null;
            _info.Stop = true;
            _info.Running = false;

            // --- lock (TasksQueue)
            {
                _tasksQueue.Enqueue(new KeyValuePair<TrickSimpleTask, bool>(_info.Instance, false));
            }

            return _info.Instance;
        }

        /// <summary>
        /// Start the task from the beginning.
        /// </summary>
        /// <returns>The current task</returns>
        public TrickSimpleTask Start()
        {
            _info.Exception = null;
            _info.Running = true;
            _info.Stop = false;
            _info.Workindex = -1;
            _info.Completed = false;
            _current = null;

            // --- lock (TasksQueue)
            {
                _tasksQueue.Enqueue(new KeyValuePair<TrickSimpleTask, bool>(_info.Instance, true));
            }

            return _info.Instance;
        }

        /// <summary>
        /// Pauses paused task.
        /// </summary>
        /// <returns>The current task</returns>
        public TrickSimpleTask Pause()
        {
            _info.Running = false;
            return _info.Instance;
        }

        /// <summary>
        /// Resume paused task.
        /// </summary>
        /// <returns>The current task</returns>
        public TrickSimpleTask Resume()
        {
            _info.Running = true;
            return _info.Instance;
        }

        /// <summary>
        /// Check if this task is running
        /// </summary>
        /// <returns>Returns true if task is running</returns>
        public bool IsRunning()
        {
            return _info.Running;
        }

        /// <summary>
        /// Check if this task is completed
        /// </summary>
        /// <returns>Returns true if task is running</returns>
        public bool IsCompleted()
        {
            return _info.Completed;
        }

        public Exception GetException()
        {
            return _info.Exception;
        }

        public override string ToString()
        {
            return $"[TrickSimpleTask-{_name}]";
        }
    }
}