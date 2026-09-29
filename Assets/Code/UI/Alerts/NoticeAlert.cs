using Code.Core.Bus;
using Code.GameEvents.System;
using UnityEngine;

namespace Code.UI.Alerts
{
    /// <summary>
    /// NoticeEvent를 배너 한 줄로 옮깁니다.
    /// </summary>
    public class NoticeAlert : MonoBehaviour
    {
        [SerializeField] private AlertBannerUI banner;

        // 같은 문구가 다시 올라오기까지 쉬는 시간. 배너를 셋이 나눠 쓰므로 연타를 여기서 막는다
        [SerializeField, Min(0f)] private float repeatCooldown = 2f;

        private BusSubscriptionScope _scope;
        private string _lastMessage;
        private float _lastShownTime = float.NegativeInfinity;

        private void Awake()
        {
            _scope = new BusSubscriptionScope();
            _scope.Subscribe<NoticeEvent>(Owner.Player, HandleNotice);
        }

        private void OnDestroy()
            => _scope?.Dispose();

        private void HandleNotice(NoticeEvent evt)
        {
            if (banner == null || string.IsNullOrEmpty(evt.Message))
                return;

            if (evt.Message == _lastMessage && Time.unscaledTime - _lastShownTime < repeatCooldown)
                return;

            _lastMessage = evt.Message;
            _lastShownTime = Time.unscaledTime;

            if (evt.HasFocus)
                banner.Show(evt.Message, evt.FocusPosition);
            else
                banner.Show(evt.Message);
        }
    }
}
