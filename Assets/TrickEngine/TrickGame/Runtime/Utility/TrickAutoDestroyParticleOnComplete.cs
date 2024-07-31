using BeauRoutine;
using UnityEngine;

namespace TrickCore
{
    public class TrickAutoDestroyParticleOnComplete : MonoBehaviour, ITickable
    {
        private const float DestroyDelay = 1.0f;
        private ParticleSystem _particleSystem;
        private bool _isEnabled;
        private IPoolObject _poolObject;
        private bool _hasBeenPlayedSinceEnable;
        private bool _returnToPool;
        private IGameContext _context;

        public void Initialize(PoolObject effect, IGameContext context)
        {
            _poolObject = effect;
            _context = context;

            if (_particleSystem == null) _particleSystem = GetComponent<ParticleSystem>();

            if (_particleSystem != null)
            {
                _context.RegisterTickable(this);
            }
            else
            {
                Debug.LogWarning("AutoDestroyParticleOnComplete: No ParticleSystem found on " + name);
                ReturnToPool(_poolObject, DestroyDelay);
            }
        }

        public void Tick(int tick, int tickRate)
        {
            if (_particleSystem == null) return;
            _isEnabled = _particleSystem.isPlaying;

            _hasBeenPlayedSinceEnable = _hasBeenPlayedSinceEnable || _isEnabled;

            // We only want to destroy the particle if it has been played since OnEnable was called
            if (!_hasBeenPlayedSinceEnable) return;

            if (_isEnabled) return;
            if (_returnToPool) return;

            _poolObject ??= GetComponent<IPoolObject>();
            if (_poolObject != null)
            {
                var main = _particleSystem.main;
                var diff = Mathf.Abs(main.duration - main.startLifetime.constantMax);
                ReturnToPool(_poolObject, DestroyDelay + diff);
            }
            _returnToPool = true;
        }

        private void ReturnToPool(IPoolObject poolObject, float destroyDelay)
        {
            Routine.StartDelay(() =>
            {
                _context.UnregisterTickable(this);
                poolObject?.ReturnToPool();
            }, destroyDelay);
        }

        public void OnEnable()
        {
            _hasBeenPlayedSinceEnable = false;
            if (_particleSystem != null) _particleSystem.Play();
            _returnToPool = false;
        }

        public void OnDisable()
        {
            // Ensure particle stops playing
            if (_particleSystem != null) _particleSystem.Stop();
            _returnToPool = false;
        }
    }
}