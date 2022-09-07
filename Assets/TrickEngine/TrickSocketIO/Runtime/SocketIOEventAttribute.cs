using System;

namespace TrickCore
{
    public sealed class SocketIOEventAttribute : Attribute
    {
        public string EventName { get; }

        public SocketIOEventAttribute(string eventName)
        {
            EventName = eventName;
        }
    }
}