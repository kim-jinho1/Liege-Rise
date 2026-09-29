using System;
using Code.Commandable;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Code.Units.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "UnitHeal", story: "Heal [Target] by [Value]", category: "Action/Units", id: "b92d06059f229e4985fcb1ff0d6c44ac")]
    public partial class UnitHealAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<int> Value;
        [SerializeReference] public BlackboardVariable<float> HealDelay = new(2f);

        private float _lastHealTime;
        private AbstractCommandable _targetCommandable;

        protected override Status OnStart()
        {
            if (Target.Value == null || !Target.Value.TryGetComponent(out _targetCommandable) ||
                _targetCommandable.CurrentHealth >= _targetCommandable.MaxHealth)
                return Status.Failure;

            if (Time.time <= _lastHealTime + HealDelay.Value)
                return Status.Running;

            _targetCommandable.Heal(Value.Value);
            _lastHealTime = Time.time;
            return Status.Success;
        }

        protected override Status OnUpdate()
        {
            if (_targetCommandable == null || _targetCommandable.CurrentHealth >= _targetCommandable.MaxHealth)
                return Status.Failure;

            if (Time.time > _lastHealTime + HealDelay.Value)
            {
                _targetCommandable.Heal(Value.Value);
                _lastHealTime = Time.time;
                return Status.Success;
            }

            return Status.Running;
        }
    }
}
