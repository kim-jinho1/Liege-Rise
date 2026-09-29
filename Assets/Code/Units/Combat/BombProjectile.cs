using Code.Commandable.Interfaces;
using Code.Units.Util;
using UnityEngine;

namespace Code.Units.Combat
{
    /// <summary>
    /// BombFish 전용 3단계 발사체입니다.
    /// 착지 → 점화 애니메이션 → 광역 데미지 → 폭발 이펙트 → 풀 반환 순서로 동작합니다.
    /// </summary>
    public class BombProjectile : Projectile
    {
        [Header("Bomb Phases")]
        [SerializeField] private Animator animator;
        [SerializeField] private string igniteTrigger = "Ignite";
        [SerializeField] private string explodeTrigger = "Explode";
        [SerializeField] private float ignitionDuration = 0.6f;
        [SerializeField] private float explosionDuration = 0.5f;

        [Header("Splash")]
        [SerializeField] private float splashRadius = 2f;
        [SerializeField] private LayerMask damageableLayers;

        protected override bool DeferPoolReturn => true;

        /// <summary>
        /// 착지 후 3단계 폭발 시퀀스를 비동기로 시작합니다.
        /// 데미지는 점화 후 폭발 시점에 적용됩니다.
        /// </summary>
        protected override void OnHit(IDamageable target, Vector3 normal)
        {
            _ = RunBombSequenceAsync(target.Transform.position, normal);
        }

        private async Awaitable RunBombSequenceAsync(Vector3 center, Vector3 normal)
        {
            transform.rotation = Quaternion.identity;
            animator?.SetTrigger(igniteTrigger);
            await Awaitable.WaitForSecondsAsync(ignitionDuration);

            ApplySplashDamage(center, normal);
            animator?.SetTrigger(explodeTrigger);

            await Awaitable.WaitForSecondsAsync(explosionDuration);
            ReturnToPool();
        }

        private void ApplySplashDamage(Vector3 center, Vector3 normal)
            => SplashUtil.Apply(Damage, center, splashRadius, damageableLayers, normal);
    }
}
