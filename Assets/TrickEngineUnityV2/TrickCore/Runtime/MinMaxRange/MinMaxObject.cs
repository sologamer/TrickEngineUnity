#if !NO_UNITY
namespace TrickCore
{
    [System.Serializable]
    public struct MinMaxObject
    {
        public MinMaxRangeAttribute.ValueType ValueType;

        public float MinValueFloat;
        public float MaxValueFloat;
        public int MinValueInt;
        public int MaxValueInt;

        public bool IsValid(float value)
        {
            return MinValueFloat <= value && value <= MaxValueFloat;
        }
        public bool IsValid(int value)
        {
            return MinValueInt <= value && value <= MaxValueInt;
        }
    }
}
#endif