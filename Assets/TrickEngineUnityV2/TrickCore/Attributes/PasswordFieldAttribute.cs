using System;

namespace TrickCore
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PasswordFieldAttribute : Attribute
    {
    }
}