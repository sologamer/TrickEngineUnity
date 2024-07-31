using UnityEngine;

namespace TrickCore
{
    public interface IPoolObject
    {
        string PoolId { get; set; }
        int InstanceId { get; set; }
        bool IsInPool { get; set; }
        IGameContext Context { get; set; }

        void OnSpawn(Transform parent);
        void OnDespawn();

        void ReturnToPool(float delay = 0.0f);
        string GetObjectName();
        GameObject GetGameObject();
    }
}