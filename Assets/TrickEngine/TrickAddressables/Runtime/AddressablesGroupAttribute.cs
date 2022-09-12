using System;

namespace TrickCore
{
    public sealed partial class AddressablesGroupAttribute : Attribute
    {
        public string Value { get; }
        public AddressablesGroupAttribute(string groupName)
        {
            Value = groupName;
        }
    }
}