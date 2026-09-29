using System.Collections;
using System.Collections.Generic;
using Code.Commandable;

namespace Code.Buildings.Base
{
    /// <summary>
    /// 생산 큐에서 "지금 만드는 것"을 뺀 대기 목록입니다. 큐는 진행 중인 항목을 0번에 그대로 두는데,
    /// UI는 그것을 큰 아이콘으로 따로 그리므로 대기 칸에 한 번 더 나오면 하나를 두 개로 봅니다.
    /// 할당 없이 원본을 그대로 비춥니다.
    /// </summary>
    public sealed class WaitingQueueView : IReadOnlyList<UnlockableSO>
    {
        private readonly UnitProductionQueue _queue;

        public WaitingQueueView(UnitProductionQueue queue)
            => _queue = queue;

        private int Skip => _queue.SOBeingBuilt != null ? 1 : 0;

        public int Count => System.Math.Max(0, _queue.Count - Skip);

        public UnlockableSO this[int index] => _queue.Items[index + Skip];

        public IEnumerator<UnlockableSO> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
