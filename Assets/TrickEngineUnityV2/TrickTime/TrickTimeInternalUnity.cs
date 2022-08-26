using System;
using System.Collections;
using System.IO;
using System.Net;
using BeauRoutine;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TrickCore
{
    public class TrickTimeInternalUnity<T> : TrickTimeInternal where T : ITrickTimeServerTime
    {
        public Func<IEnumerator> FetchServerTimeFunc { get; set; }

        private Routine _fetchRoutine;

        public TrickTimeInternalUnity(string endpoint, bool isRestEndpoint, Action fetchFailCallback, bool injectRandomnessForNoCache = true)
        {
            if (injectRandomnessForNoCache) endpoint += $"?rnd={DateTime.Now.Ticks}";
            FetchServerTimeFunc = () => FetchServerTime(endpoint, isRestEndpoint, CalculateTimeDifference, fetchFailCallback);
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

        public static IEnumerator FetchServerTime(string endPoint, bool isRestEndpoint, Action<DateTime> fetchServerTimeResult, Action failedToFetch)
        {
            bool? succeeded = null;
            DateTime serverTime = DateTime.UtcNow;

            if (isRestEndpoint)
            {
                yield return new RESTGet<T>(endPoint, null, s =>
                {
                    succeeded = s != null;
                    if (s != null) serverTime = s.FetchedServerTime;
                });
            }
            else
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endPoint);
                request.Method = "GET";
                TrickTask.StartNewTask(async () =>
                {
                    var response = await request.GetResponseAsync();
                    using var dataStream = response.GetResponseStream();
                    if (dataStream != null)
                    {
                        using var reader = new StreamReader(dataStream);
                        var str = await reader.ReadToEndAsync();
                        var obj = str.DeserializeJson<T>();
                        if (obj != null) serverTime = obj.FetchedServerTime;
                        succeeded = obj != null;
                    }
                    else
                    {
                        succeeded = false;
                    }
                });
                yield return new WaitUntil(() => succeeded != null);
            }
            
            if (succeeded.GetValueOrDefault())
            {
                fetchServerTimeResult?.Invoke(serverTime);
            }
            else
            {
                failedToFetch?.Invoke();
            }
        }
    }
}