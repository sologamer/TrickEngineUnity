using System;
using BeauRoutine;
using TrickCore;
using UnityEngine;

public class TrickAudioSource
{
    private Routine _volumeRoutine;
    private Routine _pitchRoutine;
    private AudioSource Source { get; }
    public TrickAudioSource(AudioSource source)
    {
        Source = source;
    }
    
    /// <summary>
    /// Is the source is not playing, we can use it
    /// </summary>
    /// <returns></returns>
    public bool IsAvailable() => !Source.isPlaying;

    /// <summary>
    /// Plays the audio in a loop
    /// </summary>
    /// <param name="audioId"></param>
    public void PlayLoop(ITrickAudioId audioId)
    {
        AudioManager.Instance.AudioClipResolver(audioId, clip => DefaultResolver(audioId, clip, true));
    }

    /// <summary>
    /// Plays the audio one time
    /// </summary>
    /// <param name="audioId"></param>
    public void PlayOneShot(ITrickAudioId audioId)
    {
        AudioManager.Instance.AudioClipResolver(audioId, clip => DefaultResolver(audioId, clip, false));
    }

    private void DefaultResolver(ITrickAudioId audioId, AudioClip clip, bool loop)
    {
        Source.clip = clip;
        Source.outputAudioMixerGroup = audioId.Mixer;
        Source.loop = loop;
        Source.volume = audioId.VolumeFromTo.x;
        if (Math.Abs(audioId.VolumeFromTo.x - audioId.VolumeFromTo.y) > float.Epsilon)
            _volumeRoutine.Replace(Source.VolumeTo(audioId.VolumeFromTo.y, audioId.VolumeTweenSettings).Play());
        else
            _volumeRoutine.Stop();

        Source.pitch = audioId.PitchFromTo.x;
        if (Math.Abs(audioId.PitchFromTo.x - audioId.PitchFromTo.y) > float.Epsilon)
            _pitchRoutine.Replace(Source.PitchTo(audioId.PitchFromTo.y, audioId.PitchTweenSettings).Play());
        else
        {
            if (audioId.IgnoreApplyDefaultPitch)
            {
                _pitchRoutine.Stop();
            }
            else
            {
                var range = AudioManager.Instance.DefaultPitchRange;
                Source.pitch = range.x;
                _pitchRoutine.Replace(Source.PitchTo(range.y, audioId.PitchTweenSettings).Play());
            }
        }

        if (audioId.Delay > 0)
            Source.PlayDelayed(audioId.Delay);
        else
            Source.Play();
    }
    
    /// <summary>
    /// Stops the audio source
    /// </summary>
    public void Stop()
    {
        Source.Stop();
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