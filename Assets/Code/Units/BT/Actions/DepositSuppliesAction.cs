using System;
using Code.Animations;
using Code.Economics;
using Code.Units.Base;
using Code.Units.Util;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Code.Units.BT.Actions
{
    /// <summary>
    /// 일꾼이 귀환 후 보유 자원을 납품합니다.
    /// 대상이 가공 건물이면 입력 버퍼에, 그 외에는 UserSupplies에 입금합니다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Deposit supplies", story: "[Unit] deposit [Amount] of [SupplySO]", category: "Action", id: "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6")]
    public partial class DepositSuppliesAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractUnit> Unit;
        [SerializeReference] public BlackboardVariable<int> Amount;
        [SerializeReference] public BlackboardVariable<ResourceSO> SupplySO;
        [SerializeReference] public BlackboardVariable<ParameterSO> PawnIndexParameter;
        [SerializeReference] public BlackboardVariable<ParameterSO> TransportParameter;

        protected override Status OnStart()
        {
            if (Unit.Value == null || TransportParameter.Value == null || PawnIndexParameter.Value == null)
                return Status.Failure;

            // 빈손 귀환은 실패가 아니다. Failure를 내면 뒤의 Command 되돌리기가 끊겨 일꾼이 본성 앞에 굳는다
            if (SupplySO.Value == null || Amount.Value <= 0)
            {
                PawnCarryVisuals.Clear(Unit.Value.UnitAnimator, TransportParameter.Value, PawnIndexParameter.Value);
                Amount.Value = 0;
                return Status.Success;
            }

            GameObject target = ResolveDeliveryTarget();
            int leftover = ResourceDelivery.Deliver(Unit.Value.Owner, target, SupplySO.Value, Amount.Value);

            PawnCarryVisuals.Clear(Unit.Value.UnitAnimator, TransportParameter.Value, PawnIndexParameter.Value);
            Amount.Value = leftover;
            return Status.Success;
        }

        private GameObject ResolveDeliveryTarget()
        {
            return Unit.Value.GetVariable(BTVariables.HeadQuarter, out BlackboardVariable<GameObject> hqVar)
                ? hqVar.Value
                : null;
        }
    }
}
