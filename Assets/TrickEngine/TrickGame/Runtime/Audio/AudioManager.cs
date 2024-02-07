using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrickCore
{
    public class AudioManager : MonoSingleton<AudioManager>
    {
        /// <summary>
        /// The way how we transform a TrickAudioId into a AudioClip
        /// </summary>
        public Action<ITrickAudioId, Action<AudioClip>> AudioClipResolver { get; set; } = DefaultAudioClipResolver;

        /// <summary>
        /// The number of audio sources to pool
        /// </summary>
        public int InitialAudioSourcePool = 10;

        /// <summary>
        /// The maximum number of pooled audio sources 
        /// </summary>
        public int MaxAudioSourcePool = 20;
        
        /// <summary>
        /// This is the default pitch range for all audio sources
        /// </summary>
        public Vector2 DefaultPitchRange = new Vector2(0.9f, 1.1f);
        
        /// <summary>
        /// The default main track to play
        /// </summary>
        public ITrickAudioId DefaultMainTrack;

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
            var source =
                new GameObject($"AudioSource {_sources.Count}", typeof(AudioSource)).GetComponent<AudioSource>();
            source.transform.SetParent(transform);
            TrickAudioSource newSource = new TrickAudioSource(source);
            _sources.Add(newSource);
            return newSource;
        }

        public TrickAudioSource GetAvailableAudioSource()
        {
            return _sources.Find(source => source.IsAvailable());
        }

        public TrickAudioSource PlayMainTrack(ITrickAudioId audioId)
        {
            if (audioId == null || !audioId.IsValid()) return null;
            
            if (ActiveMainTrack != null && ActiveMainTrack.IsPlaying())
            {
                AudioClipResolver(audioId, clip =>
                {
                    if (clip == ActiveMainTrack.GetActiveClip()) return;
                    ActiveMainTrack.Stop();
                    ActiveMainTrack = PlayLoop(audioId);
                });
            }
            else
            {
                ActiveMainTrack = PlayLoop(audioId);
            }

            return ActiveMainTrack;
        }

        public TrickAudioSource PlayLoop(List<ITrickAudioId> audioIds) =>
            PlayLoop(audioIds.Random(TrickIRandomizer.Default));

        public TrickAudioSource PlayLoop(ITrickAudioId audioId)
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

        public TrickAudioSource PlayOneShot(List<ITrickAudioId> audioIds)
        {
            return audioIds != null && audioIds.Count > 0
                ? PlayOneShot(audioIds.Random(TrickIRandomizer.Default))
                : null;
        }

        public TrickAudioSource PlayOneShot(ITrickAudioId audioId)
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
        public static void DefaultAudioClipResolver(ITrickAudioId arg1, Action<AudioClip> arg2)
        {
            if (arg1 is TrickAudioId trickAudioId)
            {
                arg2?.Invoke(trickAudioId.Clip);
            }
            else if (arg1 is TrickAudioIdAsync)
            {
                Debug.LogError("Async audio not supported yet, please implement your own resolver!");
            }
        }
    }
}