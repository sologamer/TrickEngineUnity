using System;
using BeauRoutine;
using UnityEngine;

public class TrickAudioSource
{
    private Routine _volumeRoutine;
    private Routine _pitchRoutine;
    public AudioSource Source { get; }

    public TrickAudioSource(AudioSource source)
    {
        Source = source;
    }

    public bool IsAvailable() => Source.isPlaying;

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

    public void PlayOneShot(TrickAudioId audioId)
    {
        AudioManager.Instance.AudioClipResolver(audioId, clip =>
        {
            Source.clip = clip;
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
}