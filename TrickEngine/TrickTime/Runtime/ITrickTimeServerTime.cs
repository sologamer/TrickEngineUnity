using System;

namespace TrickCore
{
    public interface ITrickTimeServerTime
    {
        DateTime FetchedServerTime { get; set; }
    }
}