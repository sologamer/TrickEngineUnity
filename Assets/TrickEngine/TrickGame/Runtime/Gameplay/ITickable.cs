using System.Collections;

namespace TrickCore
{
    public interface ITickable
    {
        void Tick(int tick, int tickRate);
    }
}