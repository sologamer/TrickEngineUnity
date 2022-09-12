#if UNITY_ADDRESSABLES
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace TrickCore
{
    public sealed class AddressableAssetHelper : MonoBehaviour
    {
        private UnityEngine.Object Data { get; set; }
        public bool IsUsingManager { get; set; }

        public void AddData(UnityEngine.Object data)
        {
            if (Data is { }) TryRelease(Data);
            Data = data;
        }
    
        public void OnDestroy()
        {
            if (!enabled) return;
            if (Data is { }) TryRelease(Data);
            Data = null;
        }

        private void TryRelease(UnityEngine.Object data)
        {
            if (AddressablesManager.Instance == null) return;
            if (data == null) return;

            if (IsUsingManager)
            {
                if (data is GameObject go) AddressablesManager.Instance.ReleaseAsset(TrickAssetGroupId.ManualReleaseAssetGroupId, go);
                else if (data != null) AddressablesManager.Instance.ReleaseAsset(TrickAssetGroupId.ManualReleaseAssetGroupId, data);
            }
            else
            {
                if (data is GameObject go) Addressables.ReleaseInstance(go);
                else if (data != null) Addressables.Release(data);
            }

        }
    }
}
#endif