using Code.Buildings.Logic.DefenseBuilding;
using Code.Rendering;
using UnityEngine.Rendering;

namespace Code.Units.Combat.Calculator
{
    /// <summary>
    /// 유닛의 타워 탑승/하차 상태를 소유하고 센서 감지 범위 전환을 처리합니다.
    /// </summary>
    public class TowerMountCalculator
    {
        private readonly AbstractCombatUnit _unit;

        private MannedTowerBuilding _mountedTower;
        private int _mountedSlotIndex = -1;

        // 탑승 중 정렬·밀어내기를 갈아끼우기 위해 원래 값을 기억해 둔다.
        private SortingGroup _sortingGroup;
        private YSortByPosition _ySort;
        private int _savedSortingOrder;
        private float _savedSeparationRadius;

        /// <summary>
        /// 현재 타워에 탑승 중인지 여부
        /// </summary>
        public bool IsMounted { get; private set; }

        public TowerMountCalculator(AbstractCombatUnit unit)
            => _unit = unit;

        /// <summary>
        /// 타워의 지정 슬롯에 탑승합니다. 감지 범위가 RangeBonus만큼 증가합니다.
        /// </summary>
        public void Mount(MannedTowerBuilding tower, int slotIndex)
        {
            _mountedTower = tower;
            _mountedSlotIndex = slotIndex;
            IsMounted = true;

            tower.RegisterUnit(slotIndex, _unit);
            if (_unit.CombatSO?.AttackConfig != null)
                _unit.Sensor.Setup(_unit.Owner, _unit.CombatSO.AttackConfig.DetectRange + tower.GetRangeBonus());

            ApplyMountedSorting(tower);
            SuppressSeparation();
        }

        /// <summary>
        /// 탑승 중에는 Y 정렬을 끄고 타워보다 한 칸 앞에 그립니다.
        /// YSortByPosition은 Y가 높을수록 뒤로 보내므로, 슬롯이 타워 위에 있는 한
        /// 그대로 두면 탑승 유닛이 타워 스프라이트에 가려 보이지 않습니다.
        /// </summary>
        private void ApplyMountedSorting(MannedTowerBuilding tower)
        {
            _sortingGroup = _unit.GetComponent<SortingGroup>();
            _ySort = _unit.GetComponent<YSortByPosition>();

            if (_sortingGroup == null)
                return;

            _savedSortingOrder = _sortingGroup.sortingOrder;
            if (_ySort != null)
                _ySort.enabled = false;

            _sortingGroup.sortingOrder = tower.SortingOrder + 1;
        }

        /// <summary>
        /// 탑승 중에는 밀어내기를 끕니다. 슬롯 간격(0.3~0.6)이 분리 반경(0.4)보다 좁아
        /// 그대로 두면 탑승 유닛끼리 서로 밀어 타워에서 흘러내립니다.
        /// UnitSeparationManager가 반경 0을 조기 탈출로 처리하므로 값만 비우면 됩니다.
        /// </summary>
        private void SuppressSeparation()
        {
            _savedSeparationRadius = _unit.SeparationRadius;
            _unit.SeparationRadius = 0f;
        }

        /// <summary>
        /// 정렬을 탑승 전 상태로 되돌립니다. Y 정렬이 다시 켜지면 다음 이동에서 스스로 갱신합니다.
        /// </summary>
        private void RestoreSorting()
        {
            if (_sortingGroup == null)
                return;

            _sortingGroup.sortingOrder = _savedSortingOrder;
            if (_ySort != null)
                _ySort.enabled = true;

            _sortingGroup = null;
            _ySort = null;
        }

        /// <summary>
        /// 밀어내기 반경을 탑승 전 값으로 되돌립니다.
        /// </summary>
        private void RestoreSeparation()
            => _unit.SeparationRadius = _savedSeparationRadius;

        /// <summary>
        /// 타워에서 하차합니다. 감지 범위를 원래 값으로 복구하고 Stop 상태로 전환합니다.
        /// </summary>
        public void Unmount()
        {
            if (!IsMounted)
                return;

            int slotIndex = _mountedSlotIndex;
            MannedTowerBuilding tower = _mountedTower;

            _mountedTower = null;
            _mountedSlotIndex = -1;
            IsMounted = false;

            RestoreSorting();
            RestoreSeparation();

            // 타워가 무너져 하차하는 경로다 — ?.은 파괴된 타워를 통과시킨다
            if (tower != null)
                tower.UnregisterUnit(slotIndex);

            if (_unit.CombatSO != null && _unit.CombatSO.AttackConfig != null)
                _unit.Sensor.SetupFrom(_unit.Owner, _unit.CombatSO.AttackConfig);

            _unit.Stop();
        }
    }
}
