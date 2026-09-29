using Ami.BroAudio;
using Code.Buildings.Interfaces;
using Code.Commandable;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.System;
using Code.GameEvents.Unit;
using UnityEngine;

namespace Code.Audio
{
    /// <summary>
    /// 플레이어의 조작에 소리로 답합니다.
    /// 부스에서 관람객이 "내 클릭이 먹혔나"를 화면만 보고 판단하지 않게 하는 것이 목적입니다.
    /// </summary>
    public class CommandAudio : MonoSingleton<CommandAudio>
    {
        [Header("선택")]
        [SerializeField] private SoundID selectUnit;
        [SerializeField] private SoundID selectBuilding;

        [Header("명령")]
        // 개별 커맨드가 자기 소리를 들고 있으면 그것이 이깁니다(BaseCommandSO.OrderSound).
        // 여기 있는 것은 소리를 지정하지 않은 커맨드가 쓰는 기본값입니다
        [SerializeField] private SoundID orderFallback;
        [SerializeField] private SoundID commandButton;   // 커맨드 패널 버튼을 눌렀을 때
        [SerializeField] private SoundID orderRefused;    // 명령이 거절돼 경고가 뜰 때

        [Header("간격")]
        // 드래그로 열 기를 고르면 선택 이벤트가 열 번 온다. 한 프레임에 한 번만 낸다
        [SerializeField, Min(0f)] private float selectInterval = 0.08f;
        [SerializeField, Min(0f)] private float uiInterval = 0.05f;

        private readonly SoundThrottle _throttle = new();

        private BusSubscriptionScope _scope;

        private void OnEnable()
        {
            _scope = new BusSubscriptionScope();
            _scope.Subscribe<UnitSelectEvent>(Owner.Player, OnUnitSelect);
            _scope.Subscribe<CommandSelectEvent>(Owner.Player, OnCommandSelect);
            _scope.Subscribe<NoticeEvent>(Owner.Player, OnNotice);
        }

        private void OnDisable()
            => _scope?.Dispose();

        /// <summary>
        /// 커맨드가 자기 소리를 들고 있으면 그것을, 없으면 기본 명령음을 냅니다.
        /// CommandExecutor가 명령을 실제로 실행한 뒤 부릅니다.
        /// </summary>
        public void PlayOrder(SoundID commandSound, Vector3 position)
        {
            SoundID id = commandSound.IsValid() ? commandSound : orderFallback;
            _throttle.Play(id, position, uiInterval);
        }

        /// <summary>
        /// 건물과 유닛의 선택음을 가릅니다. 무엇을 골랐는지가 소리로 구분돼야 합니다.
        /// </summary>
        private void OnUnitSelect(UnitSelectEvent evt)
        {
            if (evt.Unit is not AbstractCommandable commandable)
                return;

            bool building = commandable is IBuilding;
            _throttle.Play(building ? selectBuilding : selectUnit, selectInterval);
        }

        private void OnCommandSelect(CommandSelectEvent evt)
            => _throttle.Play(commandButton, uiInterval);

        // 경고 배너와 같은 순간에 난다. 배너 쪽에도 재발행 쿨다운이 있어 연타로 겹치지 않는다
        private void OnNotice(NoticeEvent evt)
            => _throttle.Play(orderRefused, uiInterval);
    }
}
