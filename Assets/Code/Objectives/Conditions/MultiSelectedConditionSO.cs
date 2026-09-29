using System.Collections.Generic;
using Code.Commandable.Interfaces;
using Code.Core.Bus;
using Code.GameEvents.Unit;
using UnityEngine;

namespace Code.Objectives.Conditions
{
    /// <summary>
    /// 아군을 지정한 수 이상 한꺼번에 골랐을 때 완료됩니다("끌어서 여럿 고르기" 스텝).
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_MultiSelected", menuName = "Objectives/Conditions/MultiSelected")]
    public class MultiSelectedConditionSO : ObjectiveConditionSO
    {
        [field: SerializeField, Min(2)] public int Count { get; private set; } = 2;

        public override ConditionTracker CreateTracker()
            => new MultiSelectedTracker(Count);
    }

    public class MultiSelectedTracker : ConditionTracker
    {
        public override int Current => _selected.Count;

        public override int Target => _count;

        private readonly int _count;
        private readonly HashSet<ISelectable> _selected = new();

        public MultiSelectedTracker(int count)
            => _count = count;

        public override void Subscribe()
        {
            Bus<UnitSelectEvent>.OnEvent[Owner.Player] += OnSelected;
            Bus<UnitDeselectEvent>.OnEvent[Owner.Player] += OnDeselected;
        }

        public override void Unsubscribe()
        {
            Bus<UnitSelectEvent>.OnEvent[Owner.Player] -= OnSelected;
            Bus<UnitDeselectEvent>.OnEvent[Owner.Player] -= OnDeselected;
        }

        private void OnSelected(UnitSelectEvent e)
        {
            if (e.Unit == null || !_selected.Add(e.Unit))
                return;

            Report();
            if (_selected.Count >= _count)
                Complete();
        }

        private void OnDeselected(UnitDeselectEvent e)
        {
            if (e.Unit != null && _selected.Remove(e.Unit))
                Report();
        }
    }
}
