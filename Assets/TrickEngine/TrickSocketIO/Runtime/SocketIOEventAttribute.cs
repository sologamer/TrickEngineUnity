using System;
using JetBrains.Annotations;

namespace TrickCore
{
    [UsedImplicitly]
    public sealed class SocketIOEventAttribute : Attribute
    {
        public string EventName { get; }

        public SocketIOEventAttribute(string eventName)
        {
            EventName = eventName;
        }
    }
}