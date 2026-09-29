using Code.Commandable.Interfaces;
using Code.Core.Bus;
using ObjectPool.RunTime;
using UnityEngine;

namespace Code.GameEvents.Unit
{
    public struct ProjectileLaunchEvent : IEvent
    {
        public Vector3 StartPosition { get; private set; }
        public Vector3 EndPosition { get; private set; }
        public IDamageable Target { get; private set; }
        public float Speed { get; private set; }
        public int Damage { get; private set; }
        public PoolingItemSO Item { get; private set; }
        public bool IsSplash { get; private set; }
        public float SplashRadius { get; private set; }
        public LayerMask DamageableLayers { get; private set; }

        public ProjectileLaunchEvent(Vector3 startPosition, Vector3 endPosition, IDamageable target,
            float speed, int damage, PoolingItemSO item,
            bool isSplash = false, float splashRadius = 0f, LayerMask damageableLayers = default)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            Target = target;
            Speed = speed;
            Damage = damage;
            Item = item;
            IsSplash = isSplash;
            SplashRadius = splashRadius;
            DamageableLayers = damageableLayers;
        }
    }
}
