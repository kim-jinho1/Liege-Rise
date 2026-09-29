using UnityEngine;
using UnityEngine.Rendering;

namespace Code.Rendering
{
    /// <summary>
    /// Y 정렬을 잠시 끄고 정렬 순서를 고정했다가 원래대로 되돌립니다.
    /// 유닛이 건물 스프라이트에 가려서는 안 되는 상황(탑승·건설)에 씁니다.
    /// </summary>
    public struct SortingOverride
    {
        private SortingGroup _group;
        private YSortByPosition _ySort;
        private int _savedOrder;

        /// <summary>
        /// 대상의 정렬 순서를 지정한 값으로 고정합니다.
        /// </summary>
        public void Apply(GameObject target, int order)
        {
            if (target == null || _group != null)
                return;

            _group = target.GetComponent<SortingGroup>();
            if (_group == null)
                return;

            _ySort = target.GetComponent<YSortByPosition>();
            _savedOrder = _group.sortingOrder;

            if (_ySort != null)
                _ySort.enabled = false;

            _group.sortingOrder = order;
        }

        /// <summary>
        /// 고정 전 상태로 되돌립니다. Y 정렬은 다음 이동에서 스스로 갱신합니다.
        /// </summary>
        public void Restore()
        {
            if (_group == null)
                return;

            _group.sortingOrder = _savedOrder;
            if (_ySort != null)
                _ySort.enabled = true;

            _group = null;
            _ySort = null;
        }
    }
}
