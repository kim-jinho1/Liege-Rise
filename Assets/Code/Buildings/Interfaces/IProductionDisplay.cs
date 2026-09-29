using System;
using System.Collections.Generic;
using Code.Commandable;

namespace Code.Buildings.Interfaces
{
    /// <summary>
    /// 생산 현황을 UI에 표시하기 위한 공통 창구입니다.
    /// 시간 기반 생산(유닛)과 폰 도착 기반 생산(분대)의 차이를 여기서 숨깁니다.
    /// 생산 로직 자체는 통합하지 않고 표시용 값만 맞춥니다.
    /// </summary>
    public interface IProductionDisplay
    {
        /// <summary>
        /// 생산 상태가 바뀔 때 발생합니다. UI 갱신용입니다.
        /// </summary>
        event Action ProductionChanged;

        /// <summary>
        /// 현재 생산 중인 대상입니다. 생산 중이 아니면 null.
        /// </summary>
        UnlockableSO CurrentItem { get; }

        /// <summary>
        /// 현재 생산의 진행도입니다 (0~1).
        /// </summary>
        float Progress { get; }

        /// <summary>
        /// 대기 중인 주문 목록입니다.
        /// </summary>
        IReadOnlyList<UnlockableSO> QueuedItems { get; }

        /// <summary>
        /// 대기 목록의 주문 하나를 취소합니다.
        /// </summary>
        void CancelQueued(int index);

        /// <summary>지금 만드는 것을 취소합니다. 큰 아이콘을 눌렀을 때입니다.</summary>
        void CancelCurrent();
    }
}
