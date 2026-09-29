using Code.Buildings.Base;
using Code.Buildings.Interfaces;
using Code.Buildings.Logic.Shared;
using UnityEngine;

namespace Code.Buildings.Logic.CoreBuilding
{
    /// <summary>
    /// 인구를 공급하는 건물입니다. 실제 스폰·재생성은 PawnSupplySource가 담당합니다.
    /// </summary>
    public class PopulationBuilding : AbstractBuilding
    {
        private PawnSupplySource _supply;

        /// <summary>
        /// 현재 이 건물이 유지 중인 폰 수입니다.
        /// </summary>
        // 폰 수는 UserPopulation이 센다. 집은 상한만 올리고 시작 폰은 InitialPawns만큼 세운다

        /// <summary>
        /// 완공 시점에 폰 공급을 시작합니다.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            if (BuildingSO is not IPawnSupplier supplier)
            {
                Debug.LogWarning($"{name}: BuildingSO가 IPawnSupplier가 아닙니다.", this);
                return;
            }

            _supply = PawnSupplySource.GetOrAdd(this);
            _supply.Initialize(this, supplier);
        }
    }
}
