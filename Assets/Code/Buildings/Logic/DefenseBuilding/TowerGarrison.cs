using Code.Units.Combat;
using UnityEngine;

namespace Code.Buildings.Logic.DefenseBuilding
{
    /// <summary>
    /// 타워가 세워질 때 사수를 함께 만들어 태웁니다. 적 타워를 붙박이 포탑처럼 쓰는 용도입니다.
    /// 타워가 부서지면 MannedTowerBuilding이 하차시키므로 사수는 땅에서 계속 싸웁니다.
    /// </summary>
    [RequireComponent(typeof(MannedTowerBuilding))]
    public class TowerGarrison : MonoBehaviour
    {
        /// <summary>
        /// 태울 유닛입니다. MountTower 분기는 RangeUnitBT에만 있어 원거리 유닛이 아니면 조용히 실패합니다
        /// </summary>
        [SerializeField] private CombatUnitSO unit;

        /// <summary>
        /// 태울 인원입니다. 타워의 자리 수보다 많으면 남는 인원은 타워 밑에 섭니다.
        /// </summary>
        [SerializeField, Min(0)] private int count = 1;

        private MannedTowerBuilding _tower;

        private void Awake()
            => _tower = GetComponent<MannedTowerBuilding>();

        private void Start()
        {
            if (unit == null || unit.Prefab == null)
                return;

            for (int i = 0; i < count; i++)
                _ = DeployAsync();
        }

        /// <summary>
        /// 사수 1기를 타워 자리 위에 만들어 탑승 명령을 내립니다.
        /// </summary>
        private async Awaitable DeployAsync()
        {
            GameObject spawned = Instantiate(unit.Prefab, transform.position, Quaternion.identity);
            if (!spawned.TryGetComponent(out AbstractCombatUnit combatUnit))
                return;

            combatUnit.Owner = _tower.Owner;
            combatUnit.AbstractCommandableSO = unit; // Start 전이라 체력·공격 설정이 이 값으로 잡힌다

            // BT 변수 표는 유닛의 Start에서 만들어진다. 그 전에 내린 명령은 조용히 사라진다
            await Awaitable.NextFrameAsync(destroyCancellationToken);

            if (combatUnit != null && _tower != null)
                combatUnit.OrderMountTower(_tower);
        }
    }
}
