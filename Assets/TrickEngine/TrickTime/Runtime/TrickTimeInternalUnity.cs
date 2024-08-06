using System;
using System.Collections;
using BeauRoutine;
using UnityEngine;

namespace TrickCore
{
    public class TrickTimeInternalUnity<T> : TrickTimeInternal where T : ITrickTimeServerTime
    {
        public Func<IEnumerator> FetchServerTimeFunc { get; set; }

        private Routine _fetchRoutine;

        public TrickTimeInternalUnity(Func<IEnumerator> fetchServerTimeFunc)
        {
            FetchServerTimeFunc = fetchServerTimeFunc;
        }

        /// <summary>
        /// Time difference between the SERVER and the CLIENT
        /// </summary>
        public TimeSpan TimeDifference { get; private set; }
        public float LastClockDiff { get; set; }

        public float LocalClockTotalDiff { get; set; }
        public float InitLocalClockTotalDiff { get; set; }

        public DateTime LastUserLocalTime { get; set; }
        public float LastSyncedGameTime { get; set; }

        public override DateTime CurrentServerTime
        {
            get
            {
                //if (TrickEngine.IsMainThread)
                UpdateLocalTime();
            
                return (DateTime.UtcNow + TimeDifference).AddSeconds(InitLocalClockTotalDiff);
            }
        }

        public override DateTime ToServerTime(DateTime time)
        {
            return time.AddSeconds(LocalClockTotalDiff);
        }

        public override void CalculateTimeDifference(DateTime fetchedServerTime)
        {
            TimeDifference = fetchedServerTime - DateTime.UtcNow;
            LastUserLocalTime = DateTime.UtcNow;
            LastSyncedGameTime = Time.unscaledTime;

            InitLocalClockTotalDiff = 0;
            LocalClockTotalDiff = 0;
            LastClockDiff = 0;
        }

        private void UpdateLocalTime()
        {
            if (!Application.isPlaying || !(LastSyncedGameTime > 0)) return;
            
            TimeSpan estimateTimeDiff = LastUserLocalTime.AddSeconds(Time.unscaledTime - LastSyncedGameTime) -
                                        DateTime.UtcNow;

            if (Math.Abs(InitLocalClockTotalDiff) < float.Epsilon)
            {
                InitLocalClockTotalDiff = (float) (estimateTimeDiff.TotalSeconds);
                LocalClockTotalDiff = InitLocalClockTotalDiff;
                LastClockDiff = InitLocalClockTotalDiff;
            }

            // Detect if the user forwarded their clock or not
            var prevClockDiff = LastClockDiff - (float) (estimateTimeDiff.TotalSeconds);
            LastClockDiff = (float) (estimateTimeDiff.TotalSeconds);
            float compensate = LastClockDiff - InitLocalClockTotalDiff;
            LocalClockTotalDiff = compensate;

            // the difference is too small, let's ignore it
            if (!(Mathf.Abs(prevClockDiff) >= 0.1f)) return;
            
            InitLocalClockTotalDiff -= prevClockDiff;
            LocalClockTotalDiff += prevClockDiff;

            // We are somehow off by some seconds, lets try to fetch the server time
            if (!(Mathf.Abs(prevClockDiff) >= 2.5f * Time.timeScale)) return;
            
            // We local compensate, but we MUST make a request to the server, otherwise we will never know if it was forwarded or not
            if (!_fetchRoutine.Exists() && FetchServerTimeFunc != null)
                _fetchRoutine = Routine.Start(FetchServerTimeFunc.Invoke());
        }
    }
}