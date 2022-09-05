namespace TrickCore
{
    public interface ICacheObject
    {
        void Reset();
        void Set(ref string originalData, string newJson);
    }
    
    public interface ICacheObject<out T> : ICacheObject where T : new()
    {
        T Get(string originalData);
    }
}