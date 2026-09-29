using Code.Commandable;
using Code.Commands.Base;
using Code.Units.Base;
using UnityEngine;

namespace Code.Commands.Types.Movement
{
    [CreateAssetMenu(fileName = "MoveAction", menuName = "Units/Commands/Move", order = 100)]
    public class MoveCommandSO : BaseCommandSO
    {
        [SerializeField] private float radiusMultiplier = 2.5f; //링의 크기를 조절하는 보정치

        private int _unitsOnOrbit;          //궤도에 있는 유닛에 수
        private int _maxUnitsOnOrbit = 1;   //링에 있을 수 있는 유닛의 수
        private float _circleRadius;        //궤도의 크기
        private float _radialOffset;        //링외곽에서 떨어진 정도.

        public override bool CanHandle(CommandContext context)
        {
            return context.Commandable is AbstractUnit;
        }

        public override void Handle(CommandContext context)
        {
            if(context.Commandable is not AbstractUnit unit)
                return;

            if (context.Hit.collider != null
                && context.Hit.collider.TryGetComponent(out AbstractCommandable commandable))
            {
                unit.MoveTo(commandable.transform);
                return;
            }

            if (context.UnitIndex == 0)
            {
                _unitsOnOrbit = 0;
                _maxUnitsOnOrbit = 1;
                _circleRadius = 0;
                _radialOffset = 0;
            }

            Vector3 targetPosition = new Vector3(
                context.Hit.point.x + _circleRadius * Mathf.Cos(_radialOffset * _unitsOnOrbit),
                context.Hit.point.y + _circleRadius * Mathf.Sin(_radialOffset * _unitsOnOrbit),
                0f);

            unit.MoveTo(targetPosition);
            _unitsOnOrbit++;

            if (_unitsOnOrbit >= _maxUnitsOnOrbit)
            {
                _unitsOnOrbit = 0; //다음 궤도로 넘어가기 위해 초기화
                _circleRadius += unit.AgentRadius * radiusMultiplier; //보정치를 곱해준다.
                _maxUnitsOnOrbit = Mathf.FloorToInt(2 * Mathf.PI * _circleRadius / (unit.AgentRadius * 2));
                //원의 둘레를 유닛의 지름으로 나누면 들어갈 수 있는 갯수가 나온다.
                _radialOffset = 2 * Mathf.PI / _maxUnitsOnOrbit; //360도를 갯수로 나눈다.
            }
        }

        public override bool IsLocked(CommandContext context)
            => false;
    }
}
