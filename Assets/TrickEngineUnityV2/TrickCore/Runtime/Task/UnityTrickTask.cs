using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace TrickCore
{
    public static class UnityTrickTask
    {
        public static int MaxThreads = 4;
        private static TrickTaskScheduler GetAvailableTaskScheduler()
        {
            lock (TrickTaskScheduler.TaskSchedulers)
            {
                int numSchedulers = TrickTaskScheduler.TaskSchedulers.Count;
                if (numSchedulers == MaxThreads)
                {
                    // Take the least busy scheduler. We order by the queue count, and then by if it's executing a task
                    return TrickTaskScheduler.TaskSchedulers.OrderBy(scheduler => scheduler.QueueCount).ThenBy(scheduler => scheduler.IsExecutingTask).First();
                }
                // Create a new scheduler
                return new TrickTaskScheduler(_globalCancellationTokenSource.Token);
            }
            
        }

        private static CancellationTokenSource _globalCancellationTokenSource = new CancellationTokenSource();
        

        private static async Task<KeyValuePair<Task, TResult>> SubTaskWrapper<TResult>(Func<TResult> subTask)
        {
            TResult result = default(TResult);

            try
            {
                Task castedTask = null;
                await TrickTask.ExecuteSynchronously(async () =>
                {
                    result = subTask();
                    if (typeof(TResult) == typeof(Task))
                    {
                        castedTask = (Task)(object)result;
                        await castedTask;
                    }
                });
                return new KeyValuePair<Task, TResult>(castedTask, result);
            }
            catch (TaskCanceledException)
            {
                // Task cancelled, don't do anything
            }
            catch (Exception e)
            {
                Logger.Core.LogException(e);
            }

            return new KeyValuePair<Task, TResult>(null, result);
        }

        private static async Task<KeyValuePair<Task, TResult>> SubTaskWrapper<TResult>(Func<Task<TResult>> subTask)
        {
            TResult result = default(TResult);

            try
            {
                Task castedTask = null;
                await TrickTask.ExecuteSynchronously(async () =>
                {
                    result = await subTask();
                    if (typeof(TResult) == typeof(Task))
                    {
                        castedTask = (Task)(object)result;
                        await castedTask;
                    }
                });
                return new KeyValuePair<Task, TResult>(castedTask, result);
            }
            catch (TaskCanceledException)
            {
                // Task cancelled, don't do anything
            }
            catch (Exception e)
            {
                Logger.Core.LogException(e);
            }

            return new KeyValuePair<Task, TResult>(null, result);
        }

        private static async Task<TResult> InternalStartNewTask<TResult>(Func<TResult> subTask, CancellationToken cancellationToken, TaskCreationOptions creationOptions = TaskCreationOptions.None)
        {
            KeyValuePair<Task, TResult> innerSubTask = new KeyValuePair<Task, TResult>();

            try
            {
                Task task = await Task.Factory.StartNew(
                    // Handle child task exceptions correctly
                    async () => innerSubTask = await SubTaskWrapper(subTask), 
                    cancellationToken, 
                    creationOptions,
                    // Using our custom task scheduler
                    GetAvailableTaskScheduler());

                await task;

                if (innerSubTask.Key == null) return innerSubTask.Value;
                try
                {
                    await innerSubTask.Key;
                }
                catch (Exception e)
                {
                    Logger.Core.LogException(e);
                }
            }
            catch (TaskCanceledException)
            {
                // Task cancelled, don't do anything
            }
            catch (Exception e)
            {
                Logger.Core.LogException(e);
            }

            return innerSubTask.Value;
        }

        private static async Task<TResult> InternalStartNewTask<TResult>(Func<Task<TResult>> subTask, CancellationToken cancellationToken, TaskCreationOptions creationOptions = TaskCreationOptions.None)
        {
            KeyValuePair<Task, TResult> innerSubTask = new KeyValuePair<Task, TResult>();

            try
            {
                Task task = await Task.Factory.StartNew(async () => innerSubTask = await SubTaskWrapper(subTask), cancellationToken, creationOptions, GetAvailableTaskScheduler());
                await task;

                if (innerSubTask.Key == null) return innerSubTask.Value;
                try
                {
                    await innerSubTask.Key;
                }
                catch (Exception e)
                {
                    Logger.Core.LogException(e);
                }
            }
            catch (TaskCanceledException)
            {
                // Task cancelled, don't do anything
            }
            catch (Exception e)
            {
                Logger.Core.LogException(e);
            }

            return innerSubTask.Value;
        }


        public static Task<TResult> StartNewTask<TResult>(Func<TResult> subTask, TaskCreationOptions creationOptions = TaskCreationOptions.None)
        {
            return InternalStartNewTask(subTask, _globalCancellationTokenSource.Token, creationOptions);
        }

        public static Task<TResult> StartNewTask<TResult>(Func<TResult> subTask, CancellationToken cancellationToken)
        {
            return InternalStartNewTask(subTask, cancellationToken);
        }

        public static Task<TResult> StartNewTask<TResult>(Func<Task<TResult>> subTask)
        {
            return InternalStartNewTask(subTask, _globalCancellationTokenSource.Token);
        }

        public static void InternalCleanup()
        {
            _globalCancellationTokenSource.Cancel();
            _globalCancellationTokenSource = new CancellationTokenSource();

            TrickTaskScheduler.Cleanup();
        }

        public static TaskAwaiter WaitForSeconds(float seconds)
        {
            return Task.Delay((int)(seconds * 1000)).GetAwaiter();
        }

        public static TaskAwaiter WaitForTimeSpan(TimeSpan timeSpan)
        {
            return Task.Delay(timeSpan).GetAwaiter();
        }


        public static Task WhenAll(IEnumerable<Func<Task>> tasks)
        {
            return Task.WhenAll(tasks.Select(task => (Task)StartNewTask(async () => await task())));
        }

        public static Task WhenAny(IEnumerable<Func<Task>> tasks)
        {
            return Task.WhenAny(tasks.Select(task => (Task)StartNewTask(async () => await task())));
        }

        public static Task<TResult[]> WhenAll<TResult>(IEnumerable<Func<Task<TResult>>> tasks)
        {
            return Task.WhenAll(tasks.Select(task => StartNewTask(async () => await task())));
        }

        public static Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Func<Task<TResult>>> tasks)
        {
            return Task.WhenAny(tasks.Select(task => StartNewTask(async () => await task())));
        }

        public static Task WhenAll(params Func<Task>[] tasks)
        {
            return Task.WhenAll(tasks.Select(task => (Task)StartNewTask(async () => await task())));
        }

        public static Task WhenAny(params Func<Task>[] tasks)
        {
            return Task.WhenAny(tasks.Select(task => (Task)StartNewTask(async () => await task())));
        }

        public static Task<TResult[]> WhenAll<TResult>(params Func<Task<TResult>>[] tasks)
        {
            return Task.WhenAll(tasks.Select(task => StartNewTask(async () => await task())));
        }

        public static Task<Task<TResult>> WhenAny<TResult>(params Func<Task<TResult>>[] tasks)
        {
            return Task.WhenAny(tasks.Select(task => StartNewTask(async () => await task())));
        }

        public static async Task WaitUntil(Func<bool> predicate, int spinSleep = 1)
        {
            while (predicate != null && !predicate())
            {
                await Task.Delay(spinSleep);
            }
        }

        public static async Task WaitWhile(Func<bool> predicate)
        {
            while (predicate != null && predicate())
            {
                await Task.Delay(1);
            }
        }

        public static Task WaitForEndOfFrame()
        {
            TaskCompletionSource<bool> source = new TaskCompletionSource<bool>();
            TrickEngine.ContainerDispatch(DispatchContainerType.WaitForEndOfFrame, () =>
            {
                source.SetResult(true);
            });
            return source.Task;
        }

        public static Task WaitForFixedUpdate()
        {
            TaskCompletionSource<bool> source = new TaskCompletionSource<bool>();
            TrickEngine.ContainerDispatch(DispatchContainerType.WaitForFixedUpdate, () =>
            {
                source.SetResult(true);
            });
            return source.Task;
        }

        public static Task WaitForNewFrame()
        {
            TaskCompletionSource<bool> source = new TaskCompletionSource<bool>();
            TrickEngine.ContainerDispatch(DispatchContainerType.WaitForNewFrame, () =>
            {
                source.SetResult(true);
            });
            return source.Task;
        }

        /// <summary>
        /// Creates a new linked <see cref="CancellationTokenSource"/> which is linked to the Global cancellationTokenSource from TrickEngine.
        /// </summary>
        /// <returns>Returns the linked token source</returns>
        public static CancellationTokenSource NewLinkedCancellationTokenSource()
        {
            return CancellationTokenSource.CreateLinkedTokenSource(_globalCancellationTokenSource.Token);
        }
    }
}