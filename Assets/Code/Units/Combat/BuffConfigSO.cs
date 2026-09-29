using UnityEngine;

namespace Code.Units.Combat
{
    /// <summary>
    /// 버프 유닛의 버프 설정 데이터입니다.
    /// AttackDamageBonus는 가산(+0.2 = 20% 증가), AttackDelayMultiplier는 배율(0.8 = 20% 빨라짐).
    /// </summary>
    [CreateAssetMenu(fileName = "Buff Config", menuName = "Units/CombatUnit/Buff Config", order = 21)]
    public class BuffConfigSO : ScriptableObject
    {
        [field: SerializeField] public float BuffRange { get; private set; } = 5f;
        [field: SerializeField] public float AttackDamageBonus { get; private set; } = 0.2f;
        [field: SerializeField] public float AttackDelayMultiplier { get; private set; } = 0.8f;
    }
}
