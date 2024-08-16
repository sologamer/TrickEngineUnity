using System.Collections;
using UnityEngine;

namespace TrickCore
{
    public interface IGameContext
    {
        Transform ContextParentRoot { get; set; }
        string GetContextName();
        IEnumerator Tick();
        void RegisterTickable(ITickable tickable);
        void UnregisterTickable(ITickable tickable);
    }
}