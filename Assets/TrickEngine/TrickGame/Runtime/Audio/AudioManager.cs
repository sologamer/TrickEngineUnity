using System;
using System.Collections;
using System.Collections.Generic;
using TrickCore;
using UnityEngine;

public class AudioManager : MonoSingleton<AudioManager>
{
    /// <summary>
    /// The way how we transform a TrickAudioId into a AudioClip
    /// </summary>
    public Action<TrickAudioId, Action<AudioClip>> AudioClipResolver { get; set; } = DefaultAudioClipResolver;

    /// <summary>
    /// The number of audio sources to pool
    /// </summary>
    public int InitialAudioSourcePool = 10;
    
    /// <summary>
    /// The maximum number of pooled audio sources 
    /// </summary>
    public int MaxAudioSourcePool = 20;

    /// <summary>
    /// The default main track to play
    /// </summary>
    public TrickAudioId DefaultMainTrack;
    
    private readonly List<TrickAudioSource> _sources = new List<TrickAudioSource>();

    /// <summary>
    /// The active playing maintrack
    /// </summary>
    public TrickAudioSource ActiveMainTrack { get; set; }

    protected override void Initialize()
    {
        base.Initialize();

        for (int i = 0; i < InitialAudioSourcePool; i++)
        {
            CreateNew();
        }

        if (DefaultMainTrack != null)
        {
            PlayMainTrack(DefaultMainTrack);
        }
    }


    private TrickAudioSource CreateNew()
    {
        var source = new GameObject($"AudioSource {_sources.Count}", typeof(AudioSource)).GetComponent<AudioSource>();
        source.transform.SetParent(transform);
        TrickAudioSource newSource = new TrickAudioSource(source);
        _sources.Add(newSource);
        return newSource;
    }

    public TrickAudioSource GetAvailableAudioSource()
    {
        return _sources.Find(source => source.IsAvailable());
    }

    public TrickAudioSource PlayMainTrack(TrickAudioId audioId)
    {
        if (audioId == null || !audioId.IsValid()) return null;
        if (ActiveMainTrack != null && ActiveMainTrack.IsPlaying())
        {
            // if same clip don't do anything
            if (audioId.Clip == ActiveMainTrack.GetActiveClip()) return ActiveMainTrack;
            ActiveMainTrack?.Stop();
            ActiveMainTrack = PlayLoop(audioId);
        }
        else
        {
            // not playing anything yet, lets play the audio id
            ActiveMainTrack = PlayLoop(audioId);
        }
        return ActiveMainTrack;
    }

    public TrickAudioSource PlayLoop(List<TrickAudioId> audioIds) => PlayLoop(audioIds.Random(TrickIRandomizer.Default));
    public TrickAudioSource PlayLoop(TrickAudioId audioId)
    {
        if (audioId == null || !audioId.IsValid()) return null;
        
        var source = GetAvailableAudioSource();
        if (source == null)
        {
            if (_sources.Count < MaxAudioSourcePool)
                source = CreateNew();
            else
                return null;
        }
        source?.PlayLoop(audioId);
        return source;
    }

    public TrickAudioSource PlayOneShot(List<TrickAudioId> audioIds) => PlayOneShot(audioIds.Random(TrickIRandomizer.Default));
    public TrickAudioSource PlayOneShot(TrickAudioId audioId)
    {
        if (audioId == null || !audioId.IsValid()) return null;
        
        var source = GetAvailableAudioSource();
        if (source == null)
        {
            if (_sources.Count < MaxAudioSourcePool)
                source = CreateNew();
            else
                return null;
        }
        source?.PlayOneShot(audioId);
        return source;
    }

    public void Stop(TrickAudioSource source)
    {
        source?.Stop();
    }

    public void StopAll()
    {
        _sources.ForEach(source => source.Stop());
    }

    /// <summary>
    /// Resolve the audio clip from an TrickAudioId.
    /// The default resolver uses a direct reference to the asset.
    /// Later we have different resolvers which can for example load an asset asynchronous (addressables or so)
    /// </summary>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    private static void DefaultAudioClipResolver(TrickAudioId arg1, Action<AudioClip> arg2) => arg2?.Invoke(arg1.Clip);
}