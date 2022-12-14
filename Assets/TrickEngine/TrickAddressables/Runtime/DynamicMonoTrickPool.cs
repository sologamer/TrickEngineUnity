#if UNITY_ADDRESSABLES
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace TrickCore
{
    /// <summary>
    /// A stub object to support dynamic instantiating of an object
    /// </summary>
    public sealed class DynamicMonoTrickPool : MonoBehaviour, ITrickPool
    {
        public bool IsClaimed
        {
            get => RedirectTarget?.IsClaimed ?? _isClaimed;
            set
            {
                _isClaimed = value;
                if (RedirectTarget != null) RedirectTarget.IsClaimed = _isClaimed;
            }
        }

        public AssetReferenceGameObject AssetReferenceGameObject { get; set; }
        public TrickAssetGroupId AssetGroupType { get; set; }

        public ITrickPool RedirectTarget;
    
        private bool _isClaimed;

        public void Claim()
        {
            RedirectTarget?.Claim();
        }

        public void Release()
        {
            RedirectTarget?.Release();
        }

        public void OnInstantiated()
        {
            RedirectTarget?.OnInstantiated();
        }
    }
}
#endif