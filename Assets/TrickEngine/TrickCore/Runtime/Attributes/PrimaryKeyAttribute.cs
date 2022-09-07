using System;

namespace TrickCore
{
    /// <summary>
    /// Mark the field or property as the primary key.
    /// This member will be used for finding the primary key of most likely used by the TrickSQL module.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute
    {

    }
}