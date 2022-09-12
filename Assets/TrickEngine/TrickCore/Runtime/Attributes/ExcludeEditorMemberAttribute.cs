using System;

namespace TrickCore
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ExcludeEditorMemberAttribute : Attribute
    {

    }
}