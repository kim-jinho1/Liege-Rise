using System;
using Code.Buildings.Base;
using Code.Buildings.Data;
using Code.Buildings.Logic.Shared;
using Code.Core.Bus;
using Code.GameEvents.Economy;
using UnityEngine;

namespace Code.Buildings.Logic.EconomyBuilding
{
    /// <summary>
    /// 양을 길러 식량으로 바꾸는 목장입니다.
    /// 양은 시간이 지나면 자라고, 상주 폰이 그 양을 잡을 때만 식량이 들어옵니다 —
    /// 화면의 마릿수가 곧 재고이므로, 양이 없으면 생산이 멈추는 것이 눈에 보입니다.
    /// </summary>
    public class FarmBuilding : AbstractBuilding
    {
        /// <summary>
        /// 남은 양의 수가 바뀐 직후 새 마릿수와 함께 발생합니다.
        /// </summary>
        public event Action<int> SheepCountChanged;

        /// <summary>
        /// 양 한 마리를 잡아 식량이 들어온 순간 발생합니다.
        /// </summary>
        public event Action Slaughtered;

        /// <summary>
        /// 울타리 안에 남은 양의 수입니다.
        /// </summary>
        public int SheepCount { get; private set; }

        private FarmBuildingSO FarmSO
            => BuildingSO as FarmBuildingSO;

        private PawnStationHost _host;
        private float _accumulated;
        private float _growTimer;

        /// <summary>
        /// 폰 상주 호스트를 준비하고 정원을 SO 값으로 초기화합니다.
        /// 지어진 목장은 양을 데리고 시작합니다 — 빈 울타리부터 기다리게 하면 첫 식량이 너무 늦습니다.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            _host = GetComponent<PawnStationHost>();
            if (_host == null)
                _host = gameObject.AddComponent<PawnStationHost>();

            _host.Initialize(this, FarmSO != null ? FarmSO.MaxPawns : 0);

            SetSheepCount(FarmSO != null ? FarmSO.MaxSheep : 0);
        }

        private void Update()
        {
            if (FarmSO == null || FarmSO.FoodResource == null || _host == null)
                return;

            GrowSheep();
            Slaughter();
        }

        /// <summary>
        /// 상한에 못 미치면 일정 시간마다 양을 한 마리 늘립니다.
        /// </summary>
        private void GrowSheep()
        {
            if (SheepCount >= FarmSO.MaxSheep)
            {
                _growTimer = 0f;
                return;
            }

            _growTimer += Time.deltaTime;
            if (_growTimer < FarmSO.SheepGrowInterval)
                return;

            _growTimer -= FarmSO.SheepGrowInterval;
            SetSheepCount(SheepCount + 1);
        }

        /// <summary>
        /// 상주 폰이 도축을 진행하고, 한 마리분이 차면 양을 소비해 식량을 냅니다.
        /// </summary>
        private void Slaughter()
        {
            int perSheep = Mathf.Max(1, FarmSO.FoodPerSheep);

            if (_host.Count == 0)
                return;

            // 양이 없으면 일꾼은 기다린다. 진행도를 무한히 쌓아 두면 양이 나는 족족 한꺼번에 사라진다
            if (SheepCount == 0)
            {
                _accumulated = Mathf.Min(_accumulated, perSheep);
                return;
            }

            _accumulated += _host.Count * FarmSO.FoodPerPawnPerSecond * Time.deltaTime;
            if (_accumulated < perSheep)
                return;

            _accumulated -= perSheep;
            SetSheepCount(SheepCount - 1);
            Slaughtered?.Invoke();

            Bus<SupplyEvent>.Raise(Owner, new SupplyEvent(Owner, perSheep, FarmSO.FoodResource));
        }

        private void SetSheepCount(int count)
        {
            SheepCount = count;
            SheepCountChanged?.Invoke(count);
        }
    }
}
