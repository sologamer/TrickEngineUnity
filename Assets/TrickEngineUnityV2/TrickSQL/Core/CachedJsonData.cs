namespace TrickCore
{
    public struct CachedJsonData<T> : ICacheObject<T> where T : new()
    {
        public T Value;
        public string ValueData;
        public bool Base64;

        public T Get(string originalData)
        {
            if ((Value != null && !Value.Equals(default(T))) && ValueData == originalData)
                return Value;

            ValueData = originalData;
            if (originalData != null)
            {
                string unescape = originalData.StartsWith("\\") ? originalData.Replace("\\", string.Empty) : originalData;
                return Value = (Base64 ? unescape.DeserializeJsonBase64<T>() : unescape.DeserializeJson<T>());
            }
            else
                return Value = new T();
        }

        public void Reset()
        {
            Value = default(T);
            ValueData = null;
        }

        public void Set(ref string originalData, string newJson)
        {
            originalData = newJson;
            Value = default(T);
            ValueData = null;
        }
    }
}