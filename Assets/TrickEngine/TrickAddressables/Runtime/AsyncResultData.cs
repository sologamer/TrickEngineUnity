namespace TrickCore
{
    public sealed class AsyncResultData
    {
        public bool? Result;
        public object Data;

        public bool GetValueOrDefault()
        {
            return Result.GetValueOrDefault();
        }
    }
}