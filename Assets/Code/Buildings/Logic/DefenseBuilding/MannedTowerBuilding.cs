using Code.Buildings.Base;
using Code.Buildings.Data;
using Code.Units.Combat;
using UnityEngine;
using UnityEngine.Rendering;

namespace Code.Buildings.Logic.DefenseBuilding
{
    /// <summary>
    /// 유닛 탑승형 타워 건물입니다. 유닛이 슬롯 위치에 탑승해 공격합니다.
    /// 탑승한 유닛은 RangeBonus만큼 감지 범위가 늘어납니다.
    /// </summary>
    public class MannedTowerBuilding : AbstractBuilding
    {
        [Header("Unit Positions")]
        [SerializeField] private Transform[] unitPositions;

        private MannedTowerBuildingSO _mannedSO;
        private AbstractCombatUnit[] _mountedUnits;
        private SortingGroup _sortingGroup;

        protected override void Awake()
        {
            base.Awake();
            _mannedSO = BuildingSO as MannedTowerBuildingSO;
            _mountedUnits = new AbstractCombatUnit[unitPositions.Length];
            _sortingGroup = GetComponent<SortingGroup>();
        }

        protected override void OnDestroy()
        {
            for (int i = 0; i < _mountedUnits.Length; i++)
            {
                AbstractCombatUnit unit = _mountedUnits[i];
                _mountedUnits[i] = null;

                // 타워가 무너지면 사수도 같이 사라지는 경우가 있다. ?.은 파괴된 유닛을 통과시킨다
                if (unit != null)
                    unit.UnmountTower();
            }

            base.OnDestroy();
        }

        /// <summary>
        /// 빈 슬롯을 찾아 인덱스와 Transform을 반환합니다.
        /// 슬롯이 없으면 false를 반환합니다.
        /// </summary>
        public bool TryGetFreeSlot(out int slotIndex, out Transform slotTransform)
        {
            for (int i = 0; i < unitPositions.Length; i++)
            {
                if (_mountedUnits[i] == null)
                {
                    slotIndex = i;
                    slotTransform = unitPositions[i];
                    return true;
                }
            }

            slotIndex = -1;
            slotTransform = null;
            return false;
        }

        /// <summary>
        /// 빈 슬롯을 찾아 즉시 그 유닛 앞으로 잡아 둡니다.
        /// 이동을 시작하기 전에 불러야 합니다 — TryGetFreeSlot만 쓰면 여러 명이 동시에 출발할 때
        /// 아무도 등록되지 않은 상태라 전원이 같은 슬롯을 받아 한 자리에 겹칩니다.
        /// </summary>
        public bool TryReserveSlot(AbstractCombatUnit unit, out int slotIndex, out Transform slotTransform)
        {
            if (!TryGetFreeSlot(out slotIndex, out slotTransform))
                return false;

            _mountedUnits[slotIndex] = unit;
            return true;
        }

        /// <summary>
        /// 그 유닛이 잡고 있던 슬롯만 비웁니다. 다른 유닛이 쓰고 있으면 아무것도 하지 않습니다.
        /// 탑승 전에 명령이 끊겨도 예약이 남지 않도록 하는 안전장치입니다.
        /// </summary>
        public void ReleaseSlot(int slotIndex, AbstractCombatUnit unit)
        {
            if (slotIndex < 0 || slotIndex >= _mountedUnits.Length)
                return;

            if (_mountedUnits[slotIndex] == unit)
                _mountedUnits[slotIndex] = null;
        }

        /// <summary>
        /// 지정 슬롯에 유닛을 등록합니다.
        /// </summary>
        public void RegisterUnit(int slotIndex, AbstractCombatUnit unit)
        {
            if (slotIndex < 0 || slotIndex >= _mountedUnits.Length)
                return;

            _mountedUnits[slotIndex] = unit;
        }

        /// <summary>
        /// 지정 슬롯의 유닛 등록을 해제합니다.
        /// </summary>
        public void UnregisterUnit(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _mountedUnits.Length)
                return;

            _mountedUnits[slotIndex] = null;
        }

        /// <summary>
        /// SO에 설정된 감지 범위 보너스를 반환합니다.
        /// </summary>
        public float GetRangeBonus()
            => _mannedSO != null ? _mannedSO.RangeBonus : 0f;

        /// <summary>
        /// 타워 본체의 정렬 순서입니다. 탑승 유닛이 이보다 앞에 그려져야 타워에 가리지 않습니다.
        /// </summary>
        public int SortingOrder
            => _sortingGroup != null ? _sortingGroup.sortingOrder : 0;
    }
}
