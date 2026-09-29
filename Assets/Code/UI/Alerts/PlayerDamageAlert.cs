using Code.Commandable;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Combat;
using UnityEngine;

namespace Code.UI.Alerts
{
    /// <summary>
    /// 플레이어 소유 대상이 맞았을 때 비네트와 배너로 경고합니다.
    /// 화면 밖에서 벌어진 피격만 알립니다 — 보고 있는 전투에 경고를 겹치면 정보가 아니라 소음입니다.
    /// </summary>
    public class PlayerDamageAlert : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private AlertBannerUI banner;
        [SerializeField] private DamageVignetteUI vignette;

        [Header("Settings")]
        [SerializeField, Min(0f)] private float coalesceWindow = 0.5f; // 같은 프레임에 몰린 피격을 한 줄로 묶는 창
        [SerializeField, Min(0f)] private float bannerCooldown = 8f; // 배너를 다시 띄우기까지의 최소 간격. 계속 맞는 동안 같은 문구가 도배되지 않게 한다

        [Header("Message Format")]
        [SerializeField] private string messageFormat = "{0} 피격";
        [SerializeField] private string unknownVictimName = "아군"; // 이름을 못 읽었을 때 쓸 문구

        private readonly DelayedAction _coalesce = new();

        private BusSubscriptionScope _scope;
        private Camera _camera;

        private int _largestDamage;
        private string _victimName;
        private Vector3 _focusPosition;
        private float _nextBannerTime;

        private void Awake()
        {
            _scope = new BusSubscriptionScope();
            _scope.Subscribe<DamageTakenEvent>(Owner.Player, HandleDamage);
        }

        private void OnDestroy()
        {
            _scope?.Dispose();
            _coalesce.Dispose();
        }

        /// <summary>
        /// 같은 프레임에 몰린 피격을 한 줄로 묶습니다.
        /// 가장 크게 맞은 쪽을 대표로 삼아 배너 문구와 카메라 점프 지점이 그리로 갑니다.
        /// </summary>
        private void HandleDamage(DamageTakenEvent evt)
        {
            if (evt.Victim == null || !IsOffScreen(evt.Position))
                return;

            if (vignette != null)
                vignette.Pulse();

            // 가장 크게 맞은 쪽을 대표로 삼는다 — 배너 문구와 카메라 점프 지점이 그리로 간다
            if (evt.Damage > _largestDamage)
            {
                _largestDamage = evt.Damage;
                _victimName = ResolveName(evt.Victim);
                _focusPosition = evt.Position;
            }

            _coalesce.Restart(coalesceWindow, Flush);
        }

        /// <summary>
        /// 같은 프레임에 몰린 피격을 한 줄로 묶어 배너를 띄웁니다.
        /// </summary>
        private void Flush()
        {
            if (banner != null && _largestDamage > 0 && Time.time >= _nextBannerTime)
            {
                banner.Show(string.Format(messageFormat, _victimName), _focusPosition);
                _nextBannerTime = Time.time + bannerCooldown;
            }

            _largestDamage = 0;
            _victimName = null;
        }

        /// <summary>
        /// 카메라를 모르면 경고하지 않습니다 — 오탐보다 조용한 쪽이 낫습니다.
        /// </summary>
        private bool IsOffScreen(Vector3 worldPosition)
        {
            if (!EnsureCamera())
                return false;

            Vector3 viewport = _camera.WorldToViewportPoint(worldPosition);

            return viewport.z < 0f
                   || viewport.x < 0f || viewport.x > 1f
                   || viewport.y < 0f || viewport.y > 1f;
        }

        /// <summary>
        /// 카메라를 확보합니다. 스테이지 씬이 나중에 올라오므로 처음 몇 번은 없을 수 있습니다.
        /// </summary>
        private bool EnsureCamera()
        {
            // 스테이지 씬이 나중에 올라오므로 처음 몇 번은 없을 수 있다
            if (_camera == null)
                _camera = Camera.main;

            return _camera != null;
        }

        /// <summary>
        /// 피격당한 대상의 이름을 읽습니다. SO에 이름이 없으면 unknownVictimName을 씁니다.
        /// </summary>
        private string ResolveName(AbstractCommandable victim)
        {
            AbstractCommandableSO so = victim.AbstractCommandableSO;

            return so != null && !string.IsNullOrEmpty(so.Name) ? so.Name : unknownVictimName;
        }
    }
}
