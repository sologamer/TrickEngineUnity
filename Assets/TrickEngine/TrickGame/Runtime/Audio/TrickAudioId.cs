using UnityEngine;

public class TrickAudioId
{
    //public ClipLoadType LoadType;
    
    /// <summary>
    /// The audio clip
    /// </summary>
    public AudioClip Clip;

    // Later for the addressables resolver
    //public AssetReferenceT<AudioClip> ClipAsset;

    public float Volume;
    public float Delay;

    public void PlayLoop() => AudioManager.Instance.PlayLoop(this);
    public void PlayOneShot() => AudioManager.Instance.PlayOneShot(this);
}