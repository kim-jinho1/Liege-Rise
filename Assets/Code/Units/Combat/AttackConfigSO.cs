using ObjectPool.RunTime;
using UnityEngine;

namespace Code.Units.Combat
{
    /// <summary>
    /// 공격 관련 설정을 담고 있는 ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "Attack config", menuName = "Units/Attack config", order = 7)]
    public class AttackConfigSO : ScriptableObject
    {
        [field: Header("Base Stats")]
        [field: SerializeField] public float DetectRange { get; private set; } = 8f;
        [field: SerializeField] public float AttackRange { get; private set; } = 1.5f;
        [field: SerializeField] public float AttackDelay { get; private set; } = 1f;
        [field: SerializeField] public int Damage { get; private set; } = 5;

        [field: Header("Projectile Settings")]
        [field: SerializeField] public PoolingItemSO ProjectileItem { get; private set; }
        [field: SerializeField] public float ProjectileSpeed { get; private set; } = 20f;

        [field: Header("Area of Effect")]
        [field: SerializeField] public bool IsSplash { get; private set; } // 광범위 공격 여부
        [field: SerializeField] public float SplashRadius { get; private set; } = 2f; // 피해 반경
        [field: SerializeField] public LayerMask DamageableLayers { get; private set; } // 타격 가능한 레이어

        [field: Header("Damage over Time")]
        [field: SerializeField] public bool AppliesDoT { get; private set; }
        [field: SerializeField] public int DoTDamagePerTick { get; private set; } = 5;
        [field: SerializeField] public float DoTTickInterval { get; private set; } = 0.5f;
        [field: SerializeField] public float DoTDuration { get; private set; } = 3f;
    }
}
