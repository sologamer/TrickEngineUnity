using System;

namespace TrickCore
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ExcludeEditorMemberAttribute : Attribute
    {

    }
}