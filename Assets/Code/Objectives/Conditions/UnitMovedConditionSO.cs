using Code.Commandable;
using Code.Commands.Types.Movement;
using Code.Core.Bus;
using Code.GameEvents.Unit;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 지정한 종류의 아군에게 우클릭 이동 명령이 실제로 나갔을 때 완료됩니다. 비워 두면 아무 아군이나 됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_UnitMoved", menuName = "Objectives/Conditions/UnitMoved")]
    public class UnitMovedConditionSO : ObjectiveConditionSO
    {
        [field: SerializeField] public AbstractCommandableSO TargetUnit { get; private set; }

        public override ConditionTracker CreateTracker()
            => new UnitMovedTracker(TargetUnit);
    }

    public class UnitMovedTracker : ConditionTracker
    {
        private readonly AbstractCommandableSO _target;

        public UnitMovedTracker(AbstractCommandableSO target)
            => _target = target;

        public override void Subscribe()
            => Bus<CommandIssuedEvent>.OnEvent[Owner.Player] += OnIssued;

        public override void Unsubscribe()
            => Bus<CommandIssuedEvent>.OnEvent[Owner.Player] -= OnIssued;

        private void OnIssued(CommandIssuedEvent e)
        {
            if (e.Command is not (MoveCommandSO or SquadMoveCommandSO))
                return;

            if (_target != null && (e.Unit == null || !SoMatch.Same(e.Unit.AbstractCommandableSO, _target)))
                return;

            Complete();
        }
    }
}
