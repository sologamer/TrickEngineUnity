using System;
using System.Diagnostics;

#if !NO_UNITY
using UnityEngine;
#endif

namespace TrickCore
{
    /// <summary>
    /// Base class of a task yield object
    /// </summary>
    public class TaskYield : CustomYieldInstruction
    {
        public TaskYield()
        {
            
        }

        /// <summary>
        /// Check if the yield is ready
        /// </summary>
        /// <returns>Returns true if ready</returns>
        public virtual bool Ready()
        {
            return true;
        }

        /// <summary>
        /// Task yield executes
        /// </summary>
        public virtual void Execute()
        {
            
        }

        public override bool keepWaiting { get; }
    }

    /// <summary>
    /// A TaskYield object that waits for an amount of seconds before continuing.
    /// </summary>
    public class TrickWaitForSeconds : TaskYield
    {
        private readonly Func<float> _timeFunc;
        public float Time;
        private Stopwatch _sw;

        public TrickWaitForSeconds(float seconds)
        {
            Time = seconds;
        }

        public TrickWaitForSeconds(Func<float> timeFunc)
        {
            _timeFunc = timeFunc;
        }

        /// <inheritdoc />
        public override bool Ready()
        {
            return _sw.Elapsed.TotalSeconds >= Time;
        }


        /// <inheritdoc />
        public override void Execute()
        {
            _sw = Stopwatch.StartNew();

            if (_timeFunc != null)
                Time = _timeFunc();
        }

        public override bool keepWaiting => Ready();
    };

    /// <summary>
    /// A TaskYield object that waits until a function is completed before continuing.
    /// </summary>
    public class TrickWaitUntil : TaskYield
    {
        private readonly Func<bool> _waitUntil;

        public TrickWaitUntil(Func<bool> waitUntil)
        {
            _waitUntil = waitUntil;
        }

        public override bool Ready()
        {
            return _waitUntil == null || _waitUntil();
        }

        public override void Execute()
        {

        }

        public override bool keepWaiting => Ready();
    };
    
    public sealed class TrickWaitUntilTimed : CustomYieldInstruction
    {
        private readonly Func<bool> _predicate;
        private readonly float _waitTime;

#if !NO_UNITY
        private float _targetTime;

        public override bool keepWaiting
        {
            get
            {
                // First check the timer
                if (!(Time.time >= _targetTime)) return true;
                
                // Restart the timer, so it will have to wait for the startTime again
                _targetTime = Time.time + _waitTime;
                return !_predicate();
            }
        }

        public TrickWaitUntilTimed(Func<bool> predicate, float waitTime)
        {
            _predicate = predicate;
            _waitTime = waitTime;
            _targetTime = Time.time + _waitTime;
        }
#else
        private readonly Stopwatch _stopWatch;
        
        public override bool keepWaiting
        {
            get
            {
                // First check the timer
                if (_stopWatch.Elapsed.TotalSeconds <= _waitTime) return true;
                
                // Restart the timer, so it will have to wait for the startTime again
                _stopWatch.Reset();
                return !_predicate();
            }
        }

        public TrickWaitUntilTimed(Func<bool> predicate, float waitTime)
        {
            _predicate = predicate;
            _waitTime = waitTime;
            _stopWatch = Stopwatch.StartNew();
        }
#endif
        
    }
}