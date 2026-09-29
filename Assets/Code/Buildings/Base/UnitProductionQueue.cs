using System;
using System.Collections.Generic;
using System.Threading;
using Code.Commandable;
using UnityEngine;

namespace Code.Buildings.Base
{
    /// <summary>
    /// 건물에서 생산할 유닛의 생산 대기열을 관리합니다.
    /// 최대 5개까지 대기열에 추가할 수 있으며, 생산 완료 시 스폰 콜백을 호출합니다.
    /// </summary>
    public sealed class UnitProductionQueue
    {
        public const int MaxQueueSize = 5;

        private readonly List<UnlockableSO> _items = new(MaxQueueSize);
        private readonly Action<UnlockableSO> _spawn;
        private CancellationTokenSource _cts;

        /// <summary>
        /// 현재 생산 중인 항목입니다. 대기 중이면 null.
        /// </summary>
        public UnlockableSO SOBeingBuilt { get; private set; }

        /// <summary>
        /// 현재 항목의 생산 시작 시각(Time.time). UI 진행바 계산에 사용합니다.
        /// </summary>
        public float CurrentQueueStartTime { get; private set; }

        /// <summary>
        /// 현재 큐에 있는 항목 수입니다.
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// 큐 항목의 읽기 전용 목록입니다.
        /// </summary>
        public IReadOnlyList<UnlockableSO> Items => _items;

        /// <summary>
        /// 큐가 가득 차 더 추가할 수 없는지 여부입니다.
        /// </summary>
        public bool IsFull
            => _items.Count >= MaxQueueSize;

        /// <summary>
        /// 큐 상태가 변경될 때(추가/취소/생산 완료) 발생합니다.
        /// </summary>
        public event Action<IReadOnlyList<UnlockableSO>> Updated;

        /// <summary>
        /// 생산 완료 시 호출할 스폰 콜백을 받아 큐를 생성합니다.
        /// </summary>
        public UnitProductionQueue(Action<UnlockableSO> spawn)
        {
            _spawn = spawn;
        }

        /// <summary>
        /// 항목을 큐에 추가합니다. 큐가 비어 있었다면 생산 루프를 시작합니다.
        /// </summary>
        public void Enqueue(UnlockableSO unlockable)
        {
            _items.Add(unlockable);
            if (_items.Count == 1)
                StartLoop();
            else
                Updated?.Invoke(_items);
        }

        /// <summary>
        /// 지정 인덱스 항목을 취소합니다. 현재 생산 중(0번)이면 다음 항목으로 넘어가거나 루프를 정지합니다.
        /// </summary>
        public void Cancel(int index)
        {
            if (index < 0 || index >= _items.Count)
                return;

            _items.RemoveAt(index);
            if (index == 0)
            {
                if (_items.Count > 0)
                {
                    StartLoop();
                }
                else
                {
                    StopLoop();
                    SOBeingBuilt = null;
                    Updated?.Invoke(_items);
                }
            }
            else
            {
                Updated?.Invoke(_items);
            }
        }

        /// <summary>
        /// 진행 중인 생산 루프를 취소하고 리소스를 정리합니다. 건물 파괴 시 호출합니다.
        /// </summary>
        public void Dispose()
        {
            StopLoop();
        }

        private void StartLoop()
        {
            StopLoop();
            _cts = new CancellationTokenSource();
            RunAsync(_cts.Token);
        }

        private void StopLoop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async void RunAsync(CancellationToken token)
        {
            while (_items.Count > 0)
            {
                SOBeingBuilt = _items[0];
                CurrentQueueStartTime = Time.time;
                Updated?.Invoke(_items);

                try
                {
                    await Awaitable.WaitForSecondsAsync(SOBeingBuilt.BuildTime, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                _spawn(SOBeingBuilt);
                _items.RemoveAt(0);
            }

            SOBeingBuilt = null;
            Updated?.Invoke(_items);
        }
    }
}
