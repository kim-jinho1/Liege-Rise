using Code.Commandable;
using Code.Core.Bus;
using Code.GameEvents.Unit;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 지정한 종류의 아군을 클릭해 골랐을 때 완료됩니다("일꾼을 눌러 보세요" 스텝). 비워 두면 아무 아군이나 됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_UnitSelected", menuName = "Objectives/Conditions/UnitSelected")]
    public class UnitSelectedConditionSO : ObjectiveConditionSO
    {
        [field: SerializeField] public AbstractCommandableSO TargetUnit { get; private set; }

        public override ConditionTracker CreateTracker()
            => new UnitSelectedTracker(TargetUnit);
    }

    public class UnitSelectedTracker : ConditionTracker
    {
        private readonly AbstractCommandableSO _target;

        public UnitSelectedTracker(AbstractCommandableSO target)
            => _target = target;

        public override void Subscribe()
            => Bus<UnitSelectEvent>.OnEvent[Owner.Player] += OnSelected;

        public override void Unsubscribe()
            => Bus<UnitSelectEvent>.OnEvent[Owner.Player] -= OnSelected;

        private void OnSelected(UnitSelectEvent e)
        {
            if (e.Unit is not AbstractCommandable commandable)
                return;

            if (_target != null && !SoMatch.Same(commandable.AbstractCommandableSO, _target))
                return;

            Complete();
        }
    }
}
