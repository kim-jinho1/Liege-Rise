using System;
using UnityEngine;

namespace Code.Military.Squads.Formations
{
    /// <summary>
    /// 분대가 보유한 대형 중 지금 쓰는 것을 고릅니다.
    /// 보유 목록에 없는 대형은 지정해도 무시됩니다.
    /// </summary>
    public class FormationSelector
    {
        private FormationSO[] _formations;
        private int _index;

        /// <summary>
        /// 분대가 쓸 수 있는 대형 목록을 넣고 첫 번째로 되돌립니다.
        /// </summary>
        public void SetUp(FormationSO[] formations)
        {
            _formations = formations;
            _index = 0;
        }

        /// <summary>
        /// 지금 쓰는 대형입니다. 목록이 비어 있으면 null입니다.
        /// </summary>
        public FormationSO Current
            => HasAny ? _formations[Mathf.Clamp(_index, 0, _formations.Length - 1)] : null;

        /// <summary>
        /// 인덱스로 대형을 지정합니다. 범위를 벗어나면 무시됩니다.
        /// </summary>
        public void Select(int index)
        {
            if (!HasAny || index < 0 || index >= _formations.Length)
                return;

            _index = index;
        }

        /// <summary>
        /// 대형 에셋으로 지정합니다. 보유하지 않은 대형이면 무시됩니다.
        /// </summary>
        public void Select(FormationSO formation)
            => Select(IndexOf(formation));

        /// <summary>
        /// 이 분대가 지정한 대형을 쓸 수 있는지 확인합니다.
        /// </summary>
        public bool CanUse(FormationSO formation)
            => IndexOf(formation) >= 0;

        private bool HasAny => _formations is { Length: > 0 };

        private int IndexOf(FormationSO formation)
            => formation == null || _formations == null ? -1 : Array.IndexOf(_formations, formation);
    }
}
