using System;
using BeauRoutine;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public class TrickAudioId
{
    //public ClipLoadType LoadType;
    
    /// <summary>
    /// The audio clip
    /// </summary>
    public AudioClip Clip;
    public AudioMixerGroup Mixer;

    // Later for the addressables resolver
    //public AssetReferenceT<AudioClip> ClipAsset;

    [Header("Settings")]
    public float Delay;
    
    public Vector2 VolumeFromTo = new Vector2(1.0f, 1.0f);
    public TweenSettings VolumeTweenSettings;
    
    public Vector2 PitchFromTo = new Vector2(1.0f, 1.0f);
    public TweenSettings PitchTweenSettings;
    
    public void PlayLoop() => AudioManager.Instance.PlayLoop(this);
    public void PlayOneShot() => AudioManager.Instance.PlayOneShot(this);

    public bool IsValid()
    {
        return Clip != null;
    }
}