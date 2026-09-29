using System;
using Code.Commandable;
using UnityEngine;

namespace Code.Buildings.Logic.EnemyBuilding
{
    /// <summary>
    /// 생산 건물이 스폰할 유닛과 그 확률을 정의합니다.
    /// </summary>
    [Serializable]
    public struct SpawnUnitInfo
    {
        [field: SerializeField] public float Probability { get; private set; }
        [field: SerializeField] public AbstractCommandableSO UnitData { get; private set; }

        public SpawnUnitInfo(float probability, AbstractCommandableSO unitData)
        {
            Probability = probability;
            UnitData = unitData;
        }
    }
}
