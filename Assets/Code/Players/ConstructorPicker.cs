using Code.Buildings.Interfaces;
using Code.Commandable.Interfaces;
using Code.Units.Base;
using UnityEngine;

namespace Code.Players
{
    /// <summary>
    /// 건물을 세울 일꾼 하나를 고릅니다. 고르고 나면 나머지는 건설 커맨드가 맡습니다.
    /// </summary>
    public sealed class ConstructorPicker
    {
        /// <summary>
        /// 이 자리를 지을 일꾼입니다. 골라 둔 일꾼을 먼저 보고, 없으면 유휴 일꾼을 끌어옵니다.
        /// </summary>
        public AbstractUnit Pick(SelectionController selection, Vector3 position)
        {
            AbstractUnit selected = NearestSelected(selection, position);

            // UnityEngine.Object에는 ??를 쓰면 안 된다 — 파괴된 객체를 살아 있다고 본다
            return selected != null ? selected : NearestIdle(selection, position);
        }

        /// <summary>
        /// 골라 둔 것 중 지금 건설에 나설 수 있는 가장 가까운 일꾼입니다.
        /// </summary>
        private static AbstractUnit NearestSelected(SelectionController selection, Vector3 position)
        {
            AbstractUnit nearest = null;
            float nearestSqrDist = float.MaxValue;

            foreach (ISelectable selectable in selection.SelectedUnits)
            {
                if (selectable is not AbstractUnit unit || unit.Owner != selection.ControlOwner)
                    continue;

                if (unit is not IBuildingConstructor ctor || ctor.IsBuilding)
                    continue;

                float sqrDist = (unit.transform.position - position).sqrMagnitude;
                if (sqrDist >= nearestSqrDist)
                    continue;

                nearestSqrDist = sqrDist;
                nearest = unit;
            }

            return nearest;
        }

        /// <summary>
        /// 아무것도 고르지 않았을 때 대신 나설 가장 가까운 유휴 일꾼입니다.
        /// 채집·운반·상주 중인 폰은 끌어오지 않습니다.
        /// </summary>
        private static AbstractUnit NearestIdle(SelectionController selection, Vector3 position)
        {
            AbstractUnit nearest = null;
            float nearestSqrDist = float.MaxValue;

            foreach (AbstractUnit unit in selection.AliveUnits)
            {
                if (unit.Owner != selection.ControlOwner)
                    continue;

                if (unit is not IBuildingConstructor { IsAvailable: true })
                    continue;

                float sqrDist = (unit.transform.position - position).sqrMagnitude;
                if (sqrDist >= nearestSqrDist)
                    continue;

                nearestSqrDist = sqrDist;
                nearest = unit;
            }

            return nearest;
        }
    }
}
