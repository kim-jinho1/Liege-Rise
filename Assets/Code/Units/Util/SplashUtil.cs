using System;
using Code.Commandable.Interfaces;
using UnityEngine;

namespace Code.Units.Util
{
    /// <summary>
    /// 광역(splash) 피해를 반경 내 모든 IDamageable에 적용하는 공용 유틸리티입니다.
    /// </summary>
    public static class SplashUtil
    {
        private const int MaxTargets = 32;
        private static readonly Collider2D[] Buffer = new Collider2D[MaxTargets];

        /// <summary>
        /// 광역 피해를 반경 내 모든 IDamageable에 적용합니다. ContactFilter2D(useTriggers=false)를 사용합니다.
        /// </summary>
        public static void Apply(int damage, Vector3 center, float radius, ContactFilter2D filter,
            Vector3 normal, Action<IDamageable> onHit = null)
        {
            int count = Physics2D.OverlapCircle(center, radius, filter, Buffer);
            if (count >= MaxTargets)
                Debug.LogWarning($"[SplashUtil] 대상 수가 버퍼 한도({MaxTargets})에 도달해 일부가 누락될 수 있습니다.");

            for (int i = 0; i < count; i++)
            {
                if (!Buffer[i].TryGetComponent(out IDamageable hit))
                    continue;

                hit.TakeDamage(damage, center, normal);
                onHit?.Invoke(hit);
            }
        }

        /// <summary>
        /// 광역 피해를 반경 내 모든 IDamageable에 적용합니다.
        /// LayerMask를 사용하여 필터링하며, ContactFilter2D(useTriggers=false)를 사용합니다.
        /// </summary>
        public static void Apply(int damage, Vector3 center, float radius, LayerMask layers,
            Vector3 normal, Action<IDamageable> onHit = null)
        {
            ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(layers);
            Apply(damage, center, radius, filter, normal, onHit);
        }
    }
}
