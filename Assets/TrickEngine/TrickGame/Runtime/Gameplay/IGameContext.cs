using UnityEngine;

namespace TrickCore
{
    public interface IGameContext
    {
        Transform ContextParentRoot { get; set; }
        string GetContextName();
        void Tick();
        void RegisterTickable(ITickable tickable);
        void UnregisterTickable(ITickable tickable);
    }
}