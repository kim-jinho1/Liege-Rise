using Code.Units.BT;
using Code.Units.Combat;
using UnityEngine;

namespace Code.Units.CombatUnits
{
    /// <summary>
    /// 기본 원거리 유닛
    /// 원거리 유닛의 추가 로직 필요시 사용
    /// </summary>
    public class RangeUnit : AbstractCombatUnit
    {
        [SerializeField] private Transform firePositionTrm; // 총알 발사 위치

        protected override void Start()
        {
            base.Start();
            SetVariableValue(BTVariables.FirePositionTrm, firePositionTrm);
        }
    }
}
