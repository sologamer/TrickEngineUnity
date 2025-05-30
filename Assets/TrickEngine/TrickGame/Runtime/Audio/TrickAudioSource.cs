using System;
using System.Collections;
using BeauRoutine;
using TrickCore;
using UnityEngine;

public class TrickAudioSource
{
    private Routine _volumeRoutine;
    private bool _isResolving;

    public AudioSource Source { get; }
    
    public bool IsResolving => _isResolving;
    
    public float Volume
    {
        get => Source.volume;
        set => Source.volume = value;
    }
    public float Time
    {
        get => Source.time;
        set => Source.time = value;
    }
    
    public TrickAudioSource(AudioSource source)
    {
        Source = source;
    }
    
    /// <summary>
    /// Is the source is not playing, we can use it
    /// </summary>
    /// <returns></returns>
    public bool IsAvailable() => !_isResolving && !Source.isPlaying;
    
    public IEnumerator WaitForResolve(Action<AudioClip> onResolve = null)
    {
        while (_isResolving) yield return null;
        onResolve?.Invoke(Source.clip);
    }

    /// <summary>
    /// Plays the audio in a loop
    /// </summary>
    /// <param name="audioId"></param>
    public void PlayLoop(ITrickAudioId audioId)
    {
        _isResolving = true;
        AudioManager.Instance.AudioClipResolver(audioId, clip => DefaultResolver(audioId, Vector3.zero, clip, true));
    }

    /// <summary>
    /// Plays the audio one time
    /// </summary>
    /// <param name="audioId"></param>
    /// <param name="position"></param>
    public void PlayOneShot(ITrickAudioId audioId, Vector3? position = default)
    {
        _isResolving = true;
        AudioManager.Instance.AudioClipResolver(audioId, clip => DefaultResolver(audioId, position, clip, false));
    }

    private void DefaultResolver(ITrickAudioId audioId, Vector3? position, AudioClip clip, bool loop)
    {
        Source.clip = clip;
        Source.outputAudioMixerGroup = audioId.Mixer;
        Source.loop = loop;
        Source.volume = audioId.VolumeFromTo.x;
        Source.transform.position = position.GetValueOrDefault(Camera.main is {} mainCamera ? mainCamera.transform.position : Vector3.zero);
        Source.spatialBlend = audioId.SpatialBlend ?? (loop ? AudioManager.Instance.DefaultSpatialBlendLoop : AudioManager.Instance.DefaultSpatialBlendNoLoop);
        if (Math.Abs(audioId.VolumeFromTo.x - audioId.VolumeFromTo.y) > float.Epsilon)
            _volumeRoutine.Replace(Source.VolumeTo(audioId.VolumeFromTo.y, audioId.VolumeTweenSettings).Play());
        else
            _volumeRoutine.Stop();

        if (audioId.IgnoreApplyDefaultPitch)
        {
            Source.pitch = TrickIRandomizer.Default.Next(audioId.PitchFromTo.x, audioId.PitchFromTo.y);
        }
        else
        {
            var range = AudioManager.Instance.DefaultPitchRange;
            Source.pitch = TrickIRandomizer.Default.Next(range.x, range.y);
        }

        if (audioId.Delay > 0)
            Source.PlayDelayed(audioId.Delay);
        else
            Source.Play();
        
        _isResolving = false;
    }
    
    /// <summary>
    /// Stops the audio source
    /// </summary>
    public void Stop()
    {
        Source.Stop();
        _isResolving = false;
    }

    public AudioClip GetActiveClip()
    {
        return Source != null ? Source.clip : null;
    }
    
    public bool IsPlaying()
    {
        return Source != null && Source.isPlaying;
    }
}