using System.Collections;
using BeauRoutine;
using UnityEngine;

namespace TrickCore
{
    public class TrickAutoDestroyParticleOnComplete : MonoBehaviour
    {
        private const float DestroyDelay = 1.0f;
        private const float StateChangeDelay = 0.1f; // Small delay after state change
        
        [SerializeField] private bool _debugMode = false;
        
        private ParticleSystem _particleSystem;
        private ParticleSystem[] _childParticleSystems;
        private bool _isCurrentlyPlaying;
        private bool _wasPlayingLastFrame;
        private IPoolObject _poolObject;
        private bool _hasBeenPlayedSinceEnable;
        private bool _returnToPool;
        private bool _isSetup;
        private float _stoppedTime;

        public void Setup(PoolObject effect, IGameContext context)
        {
            if (_isSetup)
            {
                if (_debugMode) Debug.LogWarning($"[{name}] Setup called multiple times - ignoring duplicate");
                return;
            }

            _poolObject = effect;

            // Get main particle system
            if (_particleSystem == null) 
                _particleSystem = GetComponent<ParticleSystem>();

            // Get all child particle systems for comprehensive monitoring
            _childParticleSystems = GetComponentsInChildren<ParticleSystem>();

            if (_particleSystem != null)
            {
                _isSetup = true;
                if (_debugMode) Debug.Log($"[{name}] Setup complete - Main duration: {_particleSystem.main.duration}, Child systems: {_childParticleSystems.Length}");
            }
            else
            {
                Debug.LogWarning($"[{name}] AutoDestroyParticleOnComplete: No ParticleSystem found");
                ReturnToPool(_poolObject, DestroyDelay);
            }
        }

        private void Update()
        {
            if (!_isSetup || _returnToPool || _particleSystem == null) return;
            
            _isCurrentlyPlaying = IsAnyParticleSystemPlaying();
            
            // Track if particle has been played since enable
            _hasBeenPlayedSinceEnable = _hasBeenPlayedSinceEnable || _isCurrentlyPlaying;

            // Detect transition from playing to stopped
            if (_wasPlayingLastFrame && !_isCurrentlyPlaying)
            {
                _stoppedTime = Time.time;
                if (_debugMode) Debug.Log($"[{name}] Particle stopped playing at {_stoppedTime}");
            }

            // Only proceed if particle has been played and is now stopped
            if (!_hasBeenPlayedSinceEnable || _isCurrentlyPlaying) 
            {
                _wasPlayingLastFrame = _isCurrentlyPlaying;
                return;
            }

            // Add small delay to ensure particle is truly finished
            if (Time.time - _stoppedTime < StateChangeDelay)
            {
                _wasPlayingLastFrame = _isCurrentlyPlaying;
                return;
            }

            // Additional safety check: ensure no particles are alive
            if (HasAliveParticles())
            {
                if (_debugMode) Debug.Log($"[{name}] Particles still alive (count: {GetTotalParticleCount()}), waiting...");
                _wasPlayingLastFrame = _isCurrentlyPlaying;
                return;
            }

            // Ready to return to pool
            _poolObject ??= GetComponent<IPoolObject>();
            if (_poolObject != null)
            {
                var main = _particleSystem.main;
                var additionalDelay = CalculateAdditionalDelay(main);
                
                if (_debugMode) 
                {
                    Debug.Log($"[{name}] Returning to pool - Delay: {DestroyDelay + additionalDelay}s, Particle count: {GetTotalParticleCount()}");
                }
                
                ReturnToPool(_poolObject, DestroyDelay + additionalDelay);
            }
            
            _returnToPool = true;
        }

        private bool IsAnyParticleSystemPlaying()
        {
            if (_particleSystem != null && _particleSystem.isPlaying) return true;
            
            // Check child particle systems
            if (_childParticleSystems != null)
            {
                foreach (var childSystem in _childParticleSystems)
                {
                    if (childSystem != null && childSystem.isPlaying)
                        return true;
                }
            }
            
            return false;
        }

        private bool HasAliveParticles()
        {
            if (_particleSystem != null && _particleSystem.particleCount > 0) return true;
            
            // Check child particle systems
            if (_childParticleSystems != null)
            {
                foreach (var childSystem in _childParticleSystems)
                {
                    if (childSystem != null && childSystem.particleCount > 0)
                        return true;
                }
            }
            
            return false;
        }

        private int GetTotalParticleCount()
        {
            int totalCount = 0;
            
            if (_particleSystem != null)
                totalCount += _particleSystem.particleCount;
            
            if (_childParticleSystems != null)
            {
                foreach (var childSystem in _childParticleSystems)
                {
                    if (childSystem != null)
                        totalCount += childSystem.particleCount;
                }
            }
            
            return totalCount;
        }

        private float CalculateAdditionalDelay(ParticleSystem.MainModule main)
        {
            // Calculate based on particle lifetime to ensure all particles have finished
            float maxLifetime = main.startLifetime.constantMax;
            float duration = main.duration;
            
            // If looping is enabled, don't add extra delay
            if (main.loop)
            {
                if (_debugMode) Debug.LogWarning($"[{name}] Particle system is looping - using minimal delay");
                return 0f;
            }
            
            // Use the difference between max lifetime and duration, but cap it
            float additionalDelay = Mathf.Max(0f, maxLifetime - duration);
            return Mathf.Min(additionalDelay, 3f); // Cap at 3 seconds
        }

        private void ReturnToPool(IPoolObject poolObject, float destroyDelay)
        {
            if (_debugMode) Debug.Log($"[{name}] Scheduling return to pool in {destroyDelay}s");
            
            Routine.StartDelay(() =>
            {
                poolObject?.ReturnToPool();
            }, destroyDelay);
        }

        public void OnEnable()
        {
            _hasBeenPlayedSinceEnable = false;
            _returnToPool = false;
            _wasPlayingLastFrame = false;
            _isCurrentlyPlaying = false;
            _stoppedTime = 0f;
            
            if (_particleSystem != null) 
            {
                _particleSystem.Play();
                if (_debugMode) Debug.Log($"[{name}] OnEnable - Starting particle system");
            }
        }

        public void OnDisable()
        {
            if (_debugMode) Debug.Log($"[{name}] OnDisable - Stopping particle systems");
            
            // Stop all particle systems
            if (_particleSystem != null) 
                _particleSystem.Stop();
                
            if (_childParticleSystems != null)
            {
                foreach (var childSystem in _childParticleSystems)
                {
                    if (childSystem != null)
                        childSystem.Stop();
                }
            }
            
            _returnToPool = false;
            _hasBeenPlayedSinceEnable = false;
        }

        // Configuration method
        public void SetDebugMode(bool enabled)
        {
            _debugMode = enabled;
        }
    }
}