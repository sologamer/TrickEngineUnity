using UnityEngine;

public class TrickAudioSource
{
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
            Source.loop = true;
            
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
            
            if (audioId.Delay > 0)
                Source.PlayDelayed(audioId.Delay);
            else
                Source.Play();
        });
    }
}