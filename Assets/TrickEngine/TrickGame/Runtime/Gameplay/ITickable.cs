using System.Collections;

namespace TrickCore
{
    public interface ITickable
    {
        IEnumerator Tick(int tick, int tickRate);
    }
}