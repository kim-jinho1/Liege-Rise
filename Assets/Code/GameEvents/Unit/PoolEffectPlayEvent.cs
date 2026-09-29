using Code.Core.Bus;
using ObjectPool.RunTime;
using UnityEngine;

namespace Code.GameEvents.Unit
{
    public struct PoolEffectPlayEvent : IEvent
    {
        public PoolingItemSO ImpactEffect { get; private set; }
        public Vector3 ImpactPos { get; private set; }
        public Quaternion Identity { get; private set; }
        public float Duration { get; private set; }

        public PoolEffectPlayEvent(PoolingItemSO impactEffect, Vector3 impactPos, Quaternion identity, float f)
        {
            ImpactEffect = impactEffect;
            ImpactPos = impactPos;
            Identity = identity;
            Duration = f;
        }
    }
}
