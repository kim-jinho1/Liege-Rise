using System;
using System.Collections.Generic;
using Code.Buildings.Base;
using Code.Buildings.Logic.CoreBuilding;
using Code.Buildings.Utils;
using Code.Units.Base;
using Code.Units.Pawns;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Code.Units.BT.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Find closest HeadQuarter", story: "[Unit] finds closest [HeadQuarter]", category: "Action", id: "1c386210cfbd4892f9bd93720e46a6c5")]
    public partial class FindClosestHeadQuarterAction : Action
    {
        [SerializeReference] public BlackboardVariable<AbstractUnit> Unit;
        [SerializeReference] public BlackboardVariable<GameObject> HeadQuarter;
        [SerializeReference] public BlackboardVariable<float> SearchRadius = new(10);

        // SearchRadius가 100이라 콜라이더가 쉽게 20개를 넘는다. 넘치면 본성이 밀려 일꾼이 굳는다
        private readonly Collider2D[] _results = new Collider2D[64];
        private ContactFilter2D _filter;

        protected override Status OnStart()
        {
            if (Unit.Value is Pawn pawn && pawn.ManualHeadQuarter != null)
            {
                HeadQuarter.Value = pawn.ManualHeadQuarter;
                return Status.Success;
            }

            //LayerMask는 현재 BlackboardVariable에 직렬화지원이 안된다.(BT 1.0.12 기준)
            _filter.SetLayerMask(LayerMask.GetMask("Building"));
            _filter.useTriggers = true;
            int cnt = Physics2D.OverlapCircle(Unit.Value.transform.position, SearchRadius.Value, _filter, _results);

            List<AbstractBuilding> nearByHq = new List<AbstractBuilding>();

            //근거리에 있는 것중 HeadquartersBuilding 컴포넌트를 가진 것들만 찾아서 빌딩을 리스트에 넣는다.
            for (int i = 0; i < cnt; i++)
                if (_results[i].TryGetComponent(out HeadquartersBuilding building)
                    && building.Owner == Unit.Value.Owner)
                    nearByHq.Add(building);

            if (nearByHq.Count == 0)
                return Status.Failure;

            nearByHq.Sort(new ClosestBaseBuildingComparer(Unit.Value.transform.position));
            // 건물 루트를 넘기면 ClosestPoint가 통행 불가인 콜라이더 표면을 목적지로 잡는다
            AbstractBuilding closest = nearByHq[0];
            HeadQuarter.Value = closest.ApproachPoint != null
                ? closest.ApproachPoint.gameObject
                : closest.gameObject;

            return Status.Success;
        }
    }
}
