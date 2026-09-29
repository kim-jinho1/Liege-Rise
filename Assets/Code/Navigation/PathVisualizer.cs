using System.Collections.Generic;
using Code.Commandable.Interfaces;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Unit;
using Code.Units.Combat;
using UnityEngine;

namespace Code.Navigation
{
    /// <summary>
    /// 선택된 유닛들의 이동 목적지를 표시하는 글로벌 마커 매니저입니다.
    /// 선택된 유닛들의 목적지 중앙에 하나의 마커만 표시합니다.
    /// </summary>
    public class PathVisualizer : MonoSingleton<PathVisualizer>
    {
        private const float RestageThresholdSqr = 0.25f; // 0.5 유닛. 이보다 덜 움직이면 다시 꽂지 않는다

        // 깃발 스프라이트. 오브젝트는 런타임에 만든다 — 씬에 빈 마커를 두고 배선을 놓치면 조용히 죽는다
        [SerializeField] private Sprite markerSprite;

        // 낙하 높이·시간과 밑동 보정. 눈으로 맞추는 값이라 인스펙터가 쥔다
        [SerializeField] private DestinationMarkerMotion markerMotion = new();

        private readonly HashSet<ISelectable> _selectedUnits = new();
        private readonly Dictionary<ISelectable, Vector3> _unitDestinations = new();
        private readonly HashSet<ISelectable> _arrivedUnits = new();

        private DestinationMarker _marker;
        private Vector3 _lastCenter = Vector3.positiveInfinity;

        protected override void Awake()
        {
            base.Awake();
            _marker = DestinationMarker.Create(markerSprite, markerMotion);
            Bus<UnitSelectEvent>.RegisterForAll(HandleUnitSelect);
            Bus<UnitDeselectEvent>.RegisterForAll(HandleUnitDeselect);
        }

        protected override void OnDestroy()
        {
            Bus<UnitSelectEvent>.UnRegisterForAll(HandleUnitSelect);
            Bus<UnitDeselectEvent>.UnRegisterForAll(HandleUnitDeselect);
            base.OnDestroy();
        }

        private void HandleUnitSelect(UnitSelectEvent evt)
        {
            _selectedUnits.Add(evt.Unit);
            // 선택된 유닛이 이미 목적지를 가지고 있다면 마커를 표시
            if (_unitDestinations.ContainsKey(evt.Unit))
                UpdateMarkerPosition();
        }

        private void HandleUnitDeselect(UnitDeselectEvent evt)
        {
            _selectedUnits.Remove(evt.Unit);
            UpdateMarkerPosition();
        }

        /// <summary>
        /// 유닛의 목적지를 등록하고 마커 위치를 업데이트합니다.
        /// </summary>
        public void SetUnitDestination(ISelectable unit, Vector3 destination)
        {
            ISelectable key = Fold(unit);
            if (!_selectedUnits.Contains(key))
                return;

            _unitDestinations[key] = destination;
            UpdateMarkerPosition();
        }

        /// <summary>
        /// 분대원은 자기 이름으로 목적지를 알려 오지만 선택 목록에는 분대가 들어 있습니다.
        /// 접어 두지 않으면 분대를 고르고 우클릭했을 때 마커가 한 번도 뜨지 않습니다.
        /// </summary>
        private static ISelectable Fold(ISelectable unit)
        {
            if (unit is not AbstractCombatUnit combat)
                return unit;

            return combat.SelectionProxy ?? unit;
        }

        /// <summary>
        /// 유닛이 목적지에 도착했음을 표시합니다.
        /// 모든 유닛이 도착할 때까지 마커를 유지합니다.
        /// </summary>
        public void MarkUnitArrived(ISelectable unit)
        {
            ISelectable key = Fold(unit);
            if (!_unitDestinations.ContainsKey(key))
                return;

            _arrivedUnits.Add(key);
            UpdateMarkerPosition();
        }

        /// <summary>
        /// 유닛의 목적지 등록을 해제하고 마커 위치를 업데이트합니다.
        /// </summary>
        public void ClearUnitDestination(ISelectable unit)
        {
            ISelectable key = Fold(unit);
            _unitDestinations.Remove(key);
            _arrivedUnits.Remove(key);
            UpdateMarkerPosition();
        }

        /// <summary>
        /// 선택된 유닛들의 목적지 중앙을 계산해서 마커를 표시합니다.
        /// 도착한 유닛의 목적지는 계산에서 제외합니다.
        /// </summary>
        private void UpdateMarkerPosition()
        {
            if (_marker == null)
                return;

            Vector3 center = Vector3.zero;
            int count = 0;
            foreach (KeyValuePair<ISelectable, Vector3> kvp in _unitDestinations)
            {
                if (!_selectedUnits.Contains(kvp.Key) || _arrivedUnits.Contains(kvp.Key))
                    continue;

                center += kvp.Value;
                count++;
            }

            if (count == 0)
            {
                _lastCenter = Vector3.positiveInfinity;
                _marker.Hide();
                return;
            }

            center /= count;

            // 재탐색마다 같은 자리로 다시 꽂으면 낙하 연출이 매번 처음부터 돈다
            if ((center - _lastCenter).sqrMagnitude < RestageThresholdSqr)
                return;

            _lastCenter = center;
            _marker.Show(center);
        }
    }
}
