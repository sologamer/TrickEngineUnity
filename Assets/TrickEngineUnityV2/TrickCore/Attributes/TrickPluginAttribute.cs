using System;

namespace TrickCore
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = false)]
    public class TrickPluginAttribute : Attribute
    {
        public bool AutoLoad;
        public int LoadOrder;

        public TrickPluginAttribute()
        {

        }
    }
}