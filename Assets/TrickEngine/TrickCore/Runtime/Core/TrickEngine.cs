//#define USE_LOCK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

#if !USE_LOCK
using System.Collections.Concurrent;
#endif

namespace TrickCore
{
    public static class TrickEngine
    {
#if USE_LOCK
        private static readonly object LockObject = new object(); 
        private static readonly Queue<Action> DispatchQueue = new Queue<Action>();
#else
        private static ConcurrentQueue<Action> _dispatchQueue = new ConcurrentQueue<Action>();
#endif
        private static readonly Dictionary<int, ConcurrentQueue<Action>> ContainerDispatchQueue = new Dictionary<int, ConcurrentQueue<Action>>();
       
        private static int? _threadId;
        private static bool _initialized;

        public static bool IsMainThread => _threadId == Thread.CurrentThread.ManagedThreadId;

        public static void ContainerDispatch(DispatchContainerType type, Action action)
        {
            
            ContainerDispatch(GetContainerId(type), action);
        }

        public static void ContainerDispatch(int containerId, Action action)
        {
            try
            {
                if (!ContainerDispatchQueue.TryGetValue(containerId, out ConcurrentQueue<Action> value))
                    ContainerDispatchQueue.Add(containerId, value = new ConcurrentQueue<Action>());
                value.Enqueue(action);
            }
            catch (Exception e)
            {
                Logger.Core.LogException(e);
            }
        }

        public static void SimpleDispatch(Action action)
        {
            try
            {
#if USE_LOCK
                lock (LockObject)
#endif
                {
                    _dispatchQueue.Enqueue(action);
                }
            }
            catch (Exception e)
            {
                Logger.Core.LogException(e);
            }
        }

        public static void Init()
        {
            if (_initialized)
            {
                Logger.Core.LogError("TrickEngine already initialized!");
                throw new Exception("Already Initialized");
            }
            _threadId = Thread.CurrentThread.ManagedThreadId;
            _initialized = true;
        }
        
        public static void Update()
        {
            if (!_initialized)
            {
                Init();
            }

            TrickTask.InternalUpdateAllTasks();
            TrickSimpleTask.InternalUpdateAllTasks();
#if USE_LOCK
            lock (LockObject)
            {
                try
                {
                    while (DispatchQueue.Count > 0)
                    {
                        DispatchQueue.Dequeue()?.Invoke();
                    }
                }
                catch (Exception e)
                {
                    Logger.Core.LogException(e);
                }
            }
#else
            try
            {
                while (_dispatchQueue.Count > 0)
                {
                    if (_dispatchQueue.TryDequeue(out Action action))
                    {
                        action?.Invoke();
                    }
                    else break;
                }
            }
            catch (Exception e)
            {
                Logger.Core.LogException(e);
            }
#endif
        }

        public static int GetContainerId(DispatchContainerType type)
        {
            return (int) type;
        }

        public static void ExecuteDispatchContainer(DispatchContainerType containerId)
        {
            ExecuteDispatchContainer(GetContainerId(containerId));
        }

        public static void ExecuteDispatchContainer(int containerId)
        {
            if (ContainerDispatchQueue.TryGetValue(containerId, out var container))
            {
                try
                {
                    if (!container.IsEmpty)
                    {
                        while (container.TryDequeue(out var action)) action?.Invoke();
                    }
                }
                catch (Exception e)
                {
                    Logger.Core.LogException(e);
                }
            }
        }

        public static void Exit()
        {
            TrickSimpleTask.InternalCleanup();
            TrickTask.InternalCleanup();
            UnityTrickTask.InternalCleanup();
            TrickAsyncConsoleInput.Stop();
            _threadId = null;
#if USE_LOCK
            lock (LockObject) DispatchQueue.Clear();
#else
            _dispatchQueue = new ConcurrentQueue<Action>();
#endif

            _initialized = false;
        }
    }
}
