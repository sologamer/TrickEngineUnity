#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;

public interface ITrickPool
{
    public bool IsClaimed { get; set; }
    AssetReferenceGameObject AssetReferenceGameObject { get; set; }
    AddressableGroupType AssetGroupType { get; set; }
    
    void Claim();
    void Release();

    void OnInstantiated();
}
#endif