namespace TrickCore
{
    public interface IRandomizer
    {
        int Next();
        int Next(int min, int max);
        float Next(float min, float max);
        double NextDouble();
    }
}