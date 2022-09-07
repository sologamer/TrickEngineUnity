using System;
using System.Collections.Generic;
using System.Linq;

namespace TrickCore
{
    public static class EnumExtensions
    {
        public static T NextEnumValue<T>(this T value) where T : Enum
        {
            int num = (int) (object) value;
            if (num == 0) num = Enum.GetValues(typeof(T)).Cast<int>().FirstOrDefault(x => x != 0);
            return (T) (object) (num << 1) is var ev && Enum.IsDefined(typeof(T), ev) ? ev : default;
        }

        public static T PreviousEnumValue<T>(this T value) where T : Enum => (T) (object) ((int) (object) value >> 1) is var ev && Enum.IsDefined(typeof(T), ev) ? ev : default;

        public static List<T> EnumToList<T>(this Enum value) where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>().Where(n => value.HasFlag(n)).ToList();
        }
        
        // https://stackoverflow.com/questions/527486/c-enum-isdefined-on-combined-flags
        public static bool IsDefinedEx(this Enum yourEnum)
        {
            char firstDigit = yourEnum.ToString()[0];
            if (Char.IsDigit(firstDigit) || firstDigit == '-')  // Account for signed enums too..
                return false;

            return true;
        }
        
        public static bool TryEnumParseEx(this string value, Type type, bool ignoreCase, out Enum enumValue)
        {
            try
            {
                enumValue = Enum.Parse(type, value, ignoreCase) as Enum;
                return true;
            }
            catch (Exception)
            {
                enumValue = null;
                return false;
            }
        }
    }
}