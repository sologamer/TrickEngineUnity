using System;

namespace TrickCore
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class IncludeEditorMemberAttribute : Attribute
    {
        public SortType SortingType = SortType.Insert;
        public IncludeObjectType IncludeType = IncludeObjectType.MemberSelf;
        public bool IncludeProperty = true;
    }

    public enum IncludeObjectType
    {
        /// <summary>
        /// Only show the member itself
        /// </summary>
        MemberSelf,
        /// <summary>
        /// Only show the contents of the member
        /// </summary>
        MemberContent,
    }

    public enum SortType
    {
        /// <summary>
        /// Insert into the collection using the current index
        /// </summary>
        Insert,
        /// <summary>
        /// Add to the end of the collection
        /// </summary>
        Add,
    }
}