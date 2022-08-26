#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;

namespace TrickCore
{
    public interface ITrickPool
    {
        /// <summary>
        /// True if the pooled object is claimed (active), to release it call the TryReleaseInstance()
        /// </summary>
        public bool IsClaimed { get; set; }
    
        /// <summary>
        /// The asset reference
        /// </summary>
        AssetReferenceGameObject AssetReferenceGameObject { get; set; }
    
        /// <summary>
        /// The group where the pooled object belongs to
        /// </summary>
        TrickAssetGroupId AssetGroupType { get; set; }
    
        /// <summary>
        /// Called when this pooled object is claimed (GET)
        /// </summary>
        void Claim();
    
        /// <summary>
        /// Called when this pooled is released (RELEASE)
        /// </summary>
        void Release();

        /// <summary>
        /// Called when this pooled object is instantiated (GET, but doesn't exists)
        /// </summary>
        void OnInstantiated();

    
        /// <summary>
        /// Tries to release the object, if not released.
        /// </summary>
        /// <returns>Returns true if successfully released</returns>
        public bool TryReleaseInstance()
        {
            if (!IsClaimed) return false;
            var pool = ObjectPoolManager.Instance.GetAssetReferencePool(AssetGroupType, AssetReferenceGameObject);
            if (pool == null) return false;
            pool.Release(this);
            return true;
        }
    }
}
#endif