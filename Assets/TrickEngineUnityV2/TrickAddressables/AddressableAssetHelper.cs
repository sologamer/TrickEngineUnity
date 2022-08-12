#if UNITY_ADDRESSABLES
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AddressableAssetHelper : MonoBehaviour
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
            if (data is GameObject go) AddressablesManager.Instance.ReleaseAsset(AddressableGroupType.ReleaseOnDestroy, go);
            else if (data != null) AddressablesManager.Instance.ReleaseAsset(AddressableGroupType.ReleaseOnDestroy, data);
        }
        else
        {
            if (data is GameObject go) Addressables.ReleaseInstance(go);
            else if (data != null) Addressables.Release(data);
        }

    }
}
#endif