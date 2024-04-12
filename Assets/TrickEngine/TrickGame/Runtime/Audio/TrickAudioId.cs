using System;
using System.Collections.Generic;
using BeauRoutine;
using TrickCore;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.Serialization;

public interface ITrickAudioId
{
    AudioMixerGroup Mixer { get; }
    float Delay { get; }
    Vector2 VolumeFromTo { get; }
    TweenSettings VolumeTweenSettings { get; }
    Vector2 PitchFromTo { get; }
    TweenSettings PitchTweenSettings { get; }
    void PlayLoop();
    void PlayOneShot();
    bool IsValid();
    bool IgnoreApplyDefaultPitch { get; }
}

[Serializable]
public class TrickAudioIdAsync : ITrickAudioId
{
    [SerializeField] private AssetReferenceT<AudioClip> clip;
    [SerializeField] private List<AssetReferenceT<AudioClip>> clips;
    [SerializeField] private AudioMixerGroup mixer;
    [SerializeField] private float delay;
    [SerializeField] private Vector2 volumeFromTo = new Vector2(1.0f, 1.0f);
    [SerializeField] private TweenSettings volumeTweenSettings;
    
    [SerializeField] private Vector2 pitchFromTo = new Vector2(1.0f, 1.0f);
    [SerializeField] private TweenSettings pitchTweenSettings;
    [SerializeField] private bool ignoreApplyDefaultPitch;

    public AssetReferenceT<AudioClip> Clip => clip;
    public List<AssetReferenceT<AudioClip>> Clips => clips;
    public AudioMixerGroup Mixer => mixer;
    public float Delay => delay;
    public Vector2 VolumeFromTo => volumeFromTo;
    public TweenSettings VolumeTweenSettings => volumeTweenSettings;
    public Vector2 PitchFromTo => pitchFromTo;
    public TweenSettings PitchTweenSettings => pitchTweenSettings;
    
    
    public void PlayLoop() => AudioManager.Instance.PlayLoop(this);
    public void PlayOneShot() => AudioManager.Instance.PlayOneShot(this);

    public bool IsValid() => Clip != null || (Clips != null && Clips.Count > 0);
    public bool IgnoreApplyDefaultPitch => ignoreApplyDefaultPitch;
}

[Serializable]
public class TrickAudioId : ITrickAudioId
{
    [SerializeField] private AudioClip clip;
    [SerializeField] private List<AudioClip> clips;
    [SerializeField] private AudioMixerGroup mixer;
    [SerializeField] private float delay;
    [SerializeField] private Vector2 volumeFromTo = new Vector2(1.0f, 1.0f);
    [SerializeField] private TweenSettings volumeTweenSettings;
    
    [SerializeField] private Vector2 pitchFromTo = new Vector2(1.0f, 1.0f);
    [SerializeField] private TweenSettings pitchTweenSettings;
    [SerializeField] private bool ignoreApplyDefaultPitch;
    
    public AudioClip Clip => clip;
    public List<AudioClip> Clips => clips;
    public AudioMixerGroup Mixer => mixer;
    public float Delay => delay;
    public Vector2 VolumeFromTo => volumeFromTo;
    public TweenSettings VolumeTweenSettings => volumeTweenSettings;
    public Vector2 PitchFromTo => pitchFromTo;
    public TweenSettings PitchTweenSettings => pitchTweenSettings;
    
    
    public void PlayLoop() => AudioManager.Instance.PlayLoop(this);
    public void PlayOneShot() => AudioManager.Instance.PlayOneShot(this);

    public bool IsValid() => Clip != null || (Clips != null && Clips.Count > 0);
    public bool IgnoreApplyDefaultPitch => ignoreApplyDefaultPitch;
}