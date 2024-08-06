using System;
using UnityEngine;

namespace TrickCore
{
    /// <summary>
    /// A utility class to get the current time on the local machine with local compensation if the time is OFF.
    /// This is to prevent from players forwarding/reversing their local time
    /// Note: On default, this class will return the current time on the local machine
    /// Use the static method SetTimeInstance to set the instance of the time class
    /// </summary>
    public static class TrickTime
    {
        public static DateTime CurrentServerTime => _instance.CurrentServerTime;
        public static DateTime ToServerTime(DateTime time) => _instance.ToServerTime(time);
        private static TrickTimeInternal _instance = new TrickTimeInternalConsole();

        public static void SetTimeInstance(TrickTimeInternal newInstance)
        {
            _instance = newInstance;
        }

        public static void CalculateTimeDifference(DateTime fetchedServerTime)
        {
            _instance.CalculateTimeDifference(fetchedServerTime);
        }
    }
}