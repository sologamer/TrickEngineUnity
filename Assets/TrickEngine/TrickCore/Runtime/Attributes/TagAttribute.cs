using System;

namespace TrickCore
{
    /// <summary>
    /// Used for finding a member field by tagging using ReflectionHelperExtension.GetMemberByTag(...)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class TagAttribute : Attribute
    {
        public string Tag { get; }

        public TagAttribute(string tag)
        {
            Tag = tag;
        }
    }
}