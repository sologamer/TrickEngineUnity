#if !NO_UNITY
using UnityEngine;

namespace TrickCore
{
    public class MinMaxRangeAttribute : PropertyAttribute
    {
        public enum ValueType
        {
            Float,
            Integer
        }

        public string Name;
        public string Description;
        public float MinValueFloat;
        public float MaxValueFloat;
        public int MinValueInt;
        public int MaxValueInt;
    
        public readonly ValueType Type;

        public MinMaxRangeAttribute(float minValue, float maxValue)
        {
            MinValueFloat = minValue;
            MaxValueFloat = maxValue;
            Type = ValueType.Float;
        }

        public MinMaxRangeAttribute(int minValue, int maxValue)
        {
            MinValueInt = minValue;
            MaxValueInt = maxValue;
            Type = ValueType.Integer;
        }

        public MinMaxRangeAttribute()
        {

        }
    }
}
#endif