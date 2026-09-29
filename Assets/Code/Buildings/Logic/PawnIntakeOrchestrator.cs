using System;
using System.Collections.Generic;
using Code.Buildings.Base;
using Code.Buildings.Logic.Shared;
using Code.Units.Pawns;
using UnityEngine;

namespace Code.Buildings.Logic
{
    /// <summary>
    /// 폰을 소모하는 건물의 폰 선발·이동 추적·소모·예약 해제를 담당합니다.
    /// 선발 시 받은 주문 SO를 보관했다가 도착 시 함께 돌려줍니다.
    /// </summary>
    public class PawnIntakeOrchestrator
    {
        /// <summary>
        /// 선발한 폰이 도착했을 때 주문과 함께 발생합니다. 예약은 이미 해제된 상태입니다.
        /// </summary>
        public event Action<Pawn, ScriptableObject> PawnArrived;

        /// <summary>
        /// 건물로 걸어오는 중인 폰 수입니다.
        /// </summary>
        public int WalkingCount
            => _walking.Count;

        /// <summary>
        /// 도착해 소모 대기 중인 폰 수입니다.
        /// </summary>
        public int ArrivedCount
            => _arrived.Count;

        /// <summary>
        /// 이동 중 + 도착 대기 중인 전체 폰 수입니다. 정원 계산용입니다.
        /// </summary>
        public int PendingCount
            => _walking.Count + _arrived.Count;

        private readonly AbstractBuilding _building;
        private readonly PawnStationHost _host;
        private readonly Dictionary<Pawn, ScriptableObject> _walking = new();
        private readonly Dictionary<Pawn, ScriptableObject> _arrived = new();
        private readonly List<Pawn> _staleBuffer = new();

        /// <summary>
        /// 소모 대상 건물과 도착 감지용 상주 호스트를 주입받습니다.
        /// </summary>
        public PawnIntakeOrchestrator(AbstractBuilding building, PawnStationHost host)
        {
            _building = building;
            _host = host;
        }

        /// <summary>
        /// 정원을 지정하고 도착 이벤트 구독을 시작합니다. 건물 초기화 시 1회 호출합니다.
        /// </summary>
        public void Attach(int capacity)
        {
            _host.Initialize(_building, capacity);
            _host.PawnStationed += HandlePawnStationed;
        }

        /// <summary>
        /// 구독을 해제하고 이동 중 폰의 예약을 풉니다. 상주 중인 폰은 호스트가 내보냅니다.
        /// </summary>
        public void Detach()
        {
            _host.PawnStationed -= HandlePawnStationed;

            foreach (Pawn pawn in _walking.Keys)
                PawnConscription.Release(pawn);

            _walking.Clear();
            _arrived.Clear();
        }

        /// <summary>
        /// 상주 정원을 갱신합니다. 필요 인원이 바뀔 때 사용합니다.
        /// </summary>
        public void SetCapacity(int capacity)
            => _host.Initialize(_building, capacity);

        /// <summary>
        /// 폰 1기를 선발해 예약하고 건물로 걸어오게 합니다. 실패 시 null을 반환합니다.
        /// </summary>
        public Pawn TrySummon(ScriptableObject order)
        {
            Pawn pawn = PawnConscription.FindConsumable(_building.Owner, _building.transform.position);
            if (pawn == null)
                return null;

            PawnConscription.Reserve(pawn);
            _walking[pawn] = order;
            pawn.StationAt(_building);
            return pawn;
        }

        /// <summary>
        /// 죽었거나 이탈한 폰을 이동·도착 목록에서 정리합니다. 주문 정보도 함께 폐기됩니다.
        /// </summary>
        public void PruneStale()
        {
            _staleBuffer.Clear();
            foreach (Pawn pawn in _walking.Keys)
                if (pawn == null || !PawnConscription.IsEnRouteToStation(pawn))
                    _staleBuffer.Add(pawn);

            foreach (Pawn pawn in _staleBuffer)
            {
                _walking.Remove(pawn);
                PawnConscription.Release(pawn);
            }

            _staleBuffer.Clear();
            foreach (Pawn pawn in _arrived.Keys)
                if (pawn == null || pawn.StationedBuilding != _building)
                    _staleBuffer.Add(pawn);

            foreach (Pawn pawn in _staleBuffer)
                _arrived.Remove(pawn);
        }

        /// <summary>
        /// 도착 대기 중인 폰을 조용히 소모합니다.
        /// </summary>
        public void ConsumeArrived(Pawn pawn)
        {
            if (_arrived.Remove(pawn) && pawn != null)
                pawn.DespawnSilently();
        }

        /// <summary>
        /// 도착 대기 중인 폰을 전부 조용히 소모합니다.
        /// </summary>
        public void ConsumeAllArrived()
        {
            foreach (Pawn pawn in _arrived.Keys)
                if (pawn != null)
                    pawn.DespawnSilently();

            _arrived.Clear();
        }

        /// <summary>
        /// 도착 대기 중인 폰을 소모하지 않고 내보냅니다.
        /// </summary>
        public void DismissArrived(Pawn pawn)
        {
            if (_arrived.Remove(pawn) && pawn != null)
                pawn.Stop();
        }

        /// <summary>
        /// 이동·도착 폰 전원을 소모하지 않고 되돌립니다. 주문 취소용입니다.
        /// </summary>
        public void DismissAll()
        {
            foreach (Pawn pawn in _walking.Keys)
            {
                PawnConscription.Release(pawn);
                if (pawn != null)
                    pawn.Stop();
            }

            _walking.Clear();
            foreach (Pawn pawn in _arrived.Keys)
                if (pawn != null)
                    pawn.Stop();

            _arrived.Clear();
        }

        /// <summary>
        /// 선발한 폰이면 주문과 함께 도착 목록에 올리고, 무관한 폰이면 내보냅니다.
        /// </summary>
        private void HandlePawnStationed(Pawn pawn)
        {
            if (!_walking.Remove(pawn, out ScriptableObject order))
            {
                pawn.Stop();
                return;
            }

            PawnConscription.Release(pawn);
            _arrived[pawn] = order;
            PawnArrived?.Invoke(pawn, order);
        }
    }
}
