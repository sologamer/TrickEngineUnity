namespace TrickCore
{
    public interface IPackProcessor<TData>
    {
        byte[] PackToBytes(TData data);
        TData UnpackToObject(byte[] patchData);
    }
}