using System;
using BeauRoutine;
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
    public void PlayLoop(TrickAudioId audioId)
    {
        AudioManager.Instance.AudioClipResolver(audioId, clip =>
        {
            Source.clip = clip;
            Source.outputAudioMixerGroup = audioId.Mixer;
            Source.loop = true;
            Source.volume = audioId.VolumeFromTo.x;
            if (Math.Abs(audioId.VolumeFromTo.x - audioId.VolumeFromTo.y) > float.Epsilon)
                _volumeRoutine.Replace(Source.VolumeTo(audioId.VolumeFromTo.y, audioId.VolumeTweenSettings).Play());
            else
                _volumeRoutine.Stop();
            
            Source.pitch = audioId.PitchFromTo.x;
            if (Math.Abs(audioId.PitchFromTo.x - audioId.PitchFromTo.y) > float.Epsilon)
                _pitchRoutine.Replace(Source.PitchTo(audioId.PitchFromTo.y, audioId.PitchTweenSettings).Play());
            else
                _pitchRoutine.Stop();

            if (audioId.Delay > 0)
                Source.PlayDelayed(audioId.Delay);
            else
                Source.Play();
        });
    }

    /// <summary>
    /// Plays the audio one time
    /// </summary>
    /// <param name="audioId"></param>
    public void PlayOneShot(TrickAudioId audioId)
    {
        AudioManager.Instance.AudioClipResolver(audioId, clip =>
        {
            Source.clip = clip;
            Source.outputAudioMixerGroup = audioId.Mixer;
            Source.loop = false;
            Source.volume = audioId.VolumeFromTo.x;
            if (Math.Abs(audioId.VolumeFromTo.x - audioId.VolumeFromTo.y) > float.Epsilon)
                _volumeRoutine.Replace(Source.VolumeTo(audioId.VolumeFromTo.y, audioId.VolumeTweenSettings).Play());
            else
                _volumeRoutine.Stop();
            
            Source.pitch = audioId.PitchFromTo.x;
            if (Math.Abs(audioId.PitchFromTo.x - audioId.PitchFromTo.y) > float.Epsilon)
                _pitchRoutine.Replace(Source.PitchTo(audioId.PitchFromTo.y, audioId.PitchTweenSettings).Play());
            else
                _pitchRoutine.Stop();
            
            if (audioId.Delay > 0)
                Source.PlayDelayed(audioId.Delay);
            else
                Source.Play();
        });
    }
    
    /// <summary>
    /// Stops the audio source
    /// </summary>
    public void Stop()
    {
        Source.Stop();
    }
}