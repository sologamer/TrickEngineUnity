using System;

namespace TrickCore
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = false)]
    public class TagAttribute : Attribute
    {
        public string Tag { get; }

        public TagAttribute(string tag)
        {
            Tag = tag;
        }
    }
}