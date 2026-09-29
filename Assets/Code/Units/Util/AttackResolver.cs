using Code.Commandable.Interfaces;
using Code.Core.Bus;
using Code.GameEvents.Unit;
using Code.Units.Combat;
using UnityEngine;

namespace Code.Units.Util
{
    public static class AttackResolver
    {
        public static void Resolve(Owner owner, Transform self, IDamageable target, Transform firePos,
            AttackConfigSO config, float damageMultiplier, float meleeOffset)
        {
            int damage = Mathf.RoundToInt(config.Damage * damageMultiplier);

            if (firePos == null)
            {
                Vector3 hitPos = target.Transform.position + Vector3.up * meleeOffset;
                Vector3 normal = (self.position - target.Transform.position).normalized;

                if (config.IsSplash)
                    SplashUtil.Apply(damage, hitPos, config.SplashRadius, config.DamageableLayers, normal, hit =>
                    {
                        if (config.AppliesDoT && hit is ICorrodable targetCorrodable)
                            targetCorrodable.ApplyCorrosion(config.DoTDamagePerTick, config.DoTTickInterval, config.DoTDuration);
                    });
                else
                    target.TakeDamage(damage, hitPos, normal);
            }
            else
            {
                float yOffset = firePos.position.y - self.position.y;
                Vector3 targetPos = target.Transform.position + Vector3.up * yOffset;
                Bus<ProjectileLaunchEvent>.Raise(owner,
                    new ProjectileLaunchEvent(firePos.position, targetPos, target,
                        config.ProjectileSpeed, damage, config.ProjectileItem,
                        config.IsSplash, config.SplashRadius, config.DamageableLayers));
            }

            if (!config.IsSplash && config.AppliesDoT && target is ICorrodable corrodable)
                corrodable.ApplyCorrosion(config.DoTDamagePerTick, config.DoTTickInterval, config.DoTDuration);
        }
    }
}
