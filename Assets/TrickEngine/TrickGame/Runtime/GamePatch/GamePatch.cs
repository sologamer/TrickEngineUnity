namespace TrickCore
{
    public interface IPackProcessor<TData>
    {
        byte[] PackToBytes(TData data);
        byte[] UnpackConvertBytes(byte[] data);
    }
}