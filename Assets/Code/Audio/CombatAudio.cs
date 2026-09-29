using Ami.BroAudio;
using Code.Core.Bus;
using Code.GameEvents.Combat;
using Code.GameEvents.Unit;
using Code.Military.Squads;
using UnityEngine;

namespace Code.Audio
{
    /// <summary>
    /// 전투 소리를 버스 이벤트에 붙입니다. 게임플레이 코드는 건드리지 않습니다 —
    /// 때린 곳도 발사한 곳도 이벤트가 이미 좌표까지 실어 나릅니다.
    /// </summary>
    public class CombatAudio : MonoBehaviour
    {
        [Header("피격")]
        [SerializeField] private SoundID hitEnemy;   // 적을 때렸을 때
        [SerializeField] private SoundID hitFriend;  // 아군이 맞았을 때. 더 눈에 띄어야 한다
        [SerializeField, Min(0f)] private float hitInterval = 0.06f;

        [Header("투사체")]
        [SerializeField] private SoundID projectileLaunch;
        [SerializeField, Min(0f)] private float projectileInterval = 0.05f;

        [Header("사망")]
        [SerializeField] private SoundID friendlyUnitDeath;
        [SerializeField] private SoundID squadWiped;   // 분대 전멸. 한 판에 몇 번 없어 간격이 필요 없다
        [SerializeField, Min(0f)] private float deathInterval = 0.12f;

        private readonly SoundThrottle _throttle = new();

        private BusSubscriptionScope _scope;

        private void OnEnable()
        {
            _scope = new BusSubscriptionScope();
            _scope.SubscribeAll<DamageTakenEvent>(OnDamageTaken);
            _scope.SubscribeAll<ProjectileLaunchEvent>(OnProjectileLaunch);
            _scope.SubscribeAll<UnitDeathEvent>(OnUnitDeath);
            _scope.SubscribeAll<SquadDeathEvent>(OnSquadDeath);
        }

        private void OnDisable()
            => _scope?.Dispose();

        /// <summary>
        /// 맞은 쪽이 누구냐로 소리를 가릅니다. 내 병력이 맞는 소리는 적을 때리는 소리와 달라야
        /// 화면을 안 보고도 "지금 내가 맞고 있다"를 압니다.
        /// </summary>
        private void OnDamageTaken(DamageTakenEvent evt)
        {
            if (evt.Victim == null)
                return;

            bool friendly = evt.Victim.Owner == Owner.Player;
            _throttle.Play(friendly ? hitFriend : hitEnemy, evt.Position, hitInterval);
        }

        private void OnProjectileLaunch(ProjectileLaunchEvent evt)
            => _throttle.Play(projectileLaunch, evt.StartPosition, projectileInterval);

        /// <summary>
        /// 아군 유닛의 죽음만 알립니다. 적 유닛 사망은 GameplayAudio가 이미 맡고 있습니다.
        /// </summary>
        private void OnUnitDeath(UnitDeathEvent evt)
        {
            if (evt.Unit == null || evt.Unit.Owner != Owner.Player)
                return;

            _throttle.Play(friendlyUnitDeath, evt.Unit.transform.position, deathInterval);
        }

        // ISquad에는 좌표가 없다 — 분대 루트를 통해 자리를 얻는다
        private void OnSquadDeath(SquadDeathEvent evt)
        {
            if (evt.Owner != Owner.Player || evt.Squad is not Squad squad || squad == null)
                return;

            _throttle.Play(squadWiped, squad.transform.position, deathInterval);
        }
    }
}
