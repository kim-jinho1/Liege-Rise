using Code.Buildings.Logic.DefenseBuilding;
using Code.Military.Combat.FSM;

namespace Code.Military.Squads
{
    /// <summary>
    /// 분대의 탑승형 타워 탑승 상태를 맡습니다.
    /// 탑승 중에는 상태 기계를 잠가 이동·교전이 자리를 흔들지 않게 합니다.
    /// </summary>
    public class SquadTowerMount
    {
        private readonly SquadRoster _roster;
        private readonly SquadStateMachine _machine;

        public SquadTowerMount(SquadRoster roster, SquadStateMachine machine)
        {
            _roster = roster;
            _machine = machine;
        }

        /// <summary>
        /// 현재 탑승 중인 타워입니다. 타워에 없으면 null입니다.
        /// </summary>
        public MannedTowerBuilding Current { get; private set; }

        /// <summary>
        /// 분대원을 타워에 올립니다. 슬롯보다 인원이 많으면 남은 인원은 타워 밑에 남습니다.
        /// </summary>
        public void Mount(MannedTowerBuilding tower)
        {
            if (tower == null || Current != null)
                return;

            Current = tower;

            // 상태를 먼저 바꾼다 — 나중에 바꾸면 이전 상태의 Exit이 방금 건 탑승 명령을 덮어쓴다
            _machine.SetMounted(true);
            _roster.AssignMount(tower);
        }

        /// <summary>
        /// 타워에서 내려 다시 명령을 받는 상태로 돌아갑니다.
        /// 멤버를 멈추면 BT의 MountTower 분기가 끝나면서 하차 처리가 따라옵니다.
        /// </summary>
        public void Dismount()
        {
            if (Current == null)
                return;

            Current = null;
            _roster.HaltAll();
            _machine.SetMounted(false);
        }
    }
}
