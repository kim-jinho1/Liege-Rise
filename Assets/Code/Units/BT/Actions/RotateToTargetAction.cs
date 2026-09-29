using System;
using Code.Units.Base;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Code.Units.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Rotate to target", story: "[Unit] rotate to [Target] [Visual] ", category: "Action/Units", id: "f4150259bb008f030a655e66e0110eda")]
    public partial class RotateToTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractUnit> Unit;
        [SerializeReference] public BlackboardVariable<SpriteRenderer> Visual;
        [SerializeReference] public BlackboardVariable<Transform> Target;

        private bool _warnedMissingVisual;

        protected override Status OnStart()
        {
            if (Unit == null || Unit.Value == null || Target.Value == null)
                return Status.Failure;

            Vector3 dir = (Target.Value.position - Unit.Value.transform.position).normalized;
            Unit.Value.transform.rotation = Quaternion.identity;

            // Visual은 연출용 참조 — 비어 있어도 공격 시퀀스는 계속 진행한다 (프리팹 배선 누락 시 유닛이 전투 불능이 되는 것 방지)
            if (Visual.Value == null)
            {
                if (!_warnedMissingVisual)
                {
                    _warnedMissingVisual = true;
                    Debug.LogWarning($"RotateToTargetAction: {Unit.Value.name}의 Visual(SpriteRenderer) 블랙보드 변수가 비어 있습니다. 스프라이트 반전 없이 진행합니다.", Unit.Value);
                }

                return Status.Success;
            }

            if (Mathf.Abs(dir.x) > 0.01f)
                Visual.Value.flipX = dir.x < 0f;

            return Status.Success;
        }
    }
}
