using Code.Units.Base;
using UnityEngine;

namespace Code.Units.Combat
{
    /// <summary>
    /// 전투 유닛의 데이터 ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "Combat Unit data", menuName = "Units/CombatUnit/Combat Unit data", order = 20)]
    public class CombatUnitSO : AbstractUnitSO
    {
        [field: SerializeField] public AttackConfigSO AttackConfig { get; private set; }

        public override object Clone()
        {
            CombatUnitSO copy = (CombatUnitSO)base.Clone();
            copy.AttackConfig = AttackConfig == null ? null : Instantiate(AttackConfig);
            return copy;
        }
    }
}
