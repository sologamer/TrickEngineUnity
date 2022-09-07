using System;

namespace TrickCore
{
    public abstract class TrickTimeInternal
    {
        public virtual DateTime CurrentServerTime => DateTime.UtcNow;
        public virtual DateTime ToServerTime(DateTime time) => time;

        public virtual void CalculateTimeDifference(DateTime fetchedServerTime)
        {
        
        }
    }
}