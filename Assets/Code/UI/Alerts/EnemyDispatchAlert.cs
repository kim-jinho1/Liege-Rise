using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Enemy;
using UnityEngine;

namespace Code.UI.Alerts
{
    /// <summary>
    /// 적 출격을 배너 한 줄로 알립니다.
    /// 사이클이 오르면 조건을 채운 주둔지가 같은 프레임에 전부 출격하므로,
    /// 짧은 창 동안 모아 합계로 한 번만 띄웁니다.
    /// </summary>
    public class EnemyDispatchAlert : MonoBehaviour
    {
        [SerializeField] private AlertBannerUI banner;

        // 같은 프레임에 몰린 출격을 한 줄로 묶는 창
        [SerializeField, Min(0f)] private float coalesceWindow = 0.4f;

        [SerializeField] private string messageFormat = "적 병력 {0}기 출격";

        private readonly DelayedAction _coalesce = new();

        private BusSubscriptionScope _scope;
        private int _pendingCount;
        private int _largestGroup;
        private Vector3 _focusPosition;

        private void Awake()
        {
            _scope = new BusSubscriptionScope();
            _scope.Subscribe<EnemyDispatchEvent>(Owner.Enemy, HandleDispatch);
        }

        private void OnDestroy()
        {
            _scope?.Dispose();
            _coalesce.Dispose();
        }

        /// <summary>
        /// 같은 프레임에 몰린 출격을 한 줄로 묶습니다. 가장 큰 무리를 대표로 삼아 배너 클릭 시 그리로 이동합니다.
        /// </summary>
        private void HandleDispatch(EnemyDispatchEvent evt)
        {
            _pendingCount += evt.UnitCount;

            // 가장 큰 무리를 대표로 삼는다 — 배너를 클릭하면 그리로 간다
            if (evt.UnitCount > _largestGroup)
            {
                _largestGroup = evt.UnitCount;
                _focusPosition = evt.Origin;
            }

            _coalesce.Restart(coalesceWindow, Flush);
        }

        /// <summary>
        /// 같은 프레임에 몰린 출격을 한 줄로 묶어 배너를 띄웁니다.
        /// </summary>
        private void Flush()
        {
            if (banner != null && _pendingCount > 0)
                banner.Show(string.Format(messageFormat, _pendingCount), _focusPosition);

            _pendingCount = 0;
            _largestGroup = 0;
        }
    }
}
