using System;

namespace TrickCore
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class FlagTypeEditorAttribute : Attribute
    {
        public ShowFlagType FlagType { get; }

        public FlagTypeEditorAttribute(ShowFlagType type)
        {
            FlagType = type;
        }
    }

    public enum ShowFlagType
    {
        Normal,
        Mask,
    }
}