using System;
using System.Collections.Generic;
using Code.Buildings.Base;
using Code.Units.Pawns;
using UnityEngine;

namespace Code.Buildings.Logic.Shared
{
    /// <summary>
    /// 폰이 상주할 수 있는 건물에 부착되는 컴포넌트입니다.
    /// </summary>
    public class PawnStationHost : MonoBehaviour
    {
        /// <summary>
        /// 상주 폰 수가 변할 때 발생합니다.
        /// </summary>
        public event Action CountChanged;

        /// <summary>
        /// 폰이 도착해 상주 등록된 직후 해당 폰과 함께 발생합니다.
        /// </summary>
        public event Action<Pawn> PawnStationed;

        /// <summary>
        /// 현재 상주 중인 폰 수입니다.
        /// </summary>
        public int Count => _stationed.Count;

        /// <summary>
        /// 가장 먼저 상주한 폰입니다. 상주 중이 아니면 null입니다.
        /// </summary>
        public Pawn Primary
            => _stationed.Count > 0 ? _stationed[0] : null;

        /// <summary>
        /// 배치 가능한 최대 폰 수입니다.
        /// </summary>
        public int Max { get; private set; }

        /// <summary>
        /// 추가 배치 여유가 있는지 여부입니다.
        /// </summary>
        public bool HasSpace
            => Count < Max;

        private readonly List<Pawn> _stationed = new();
        private AbstractBuilding _building;

        /// <summary>
        /// 건물에 부착된 상주 호스트를 가져오고, 없으면 새로 추가해 반환합니다.
        /// </summary>
        public static PawnStationHost GetOrAdd(AbstractBuilding building)
        {
            PawnStationHost host = building.GetComponent<PawnStationHost>();
            return host != null ? host : building.gameObject.AddComponent<PawnStationHost>();
        }

        /// <summary>
        /// 상주 호스트를 초기화합니다. 반드시 건물과 최대 폰 수를 지정해야 합니다.
        /// </summary>
        public void Initialize(AbstractBuilding building, int maxPawns)
        {
            _building = building;
            Max = maxPawns;
        }

        /// <summary>
        /// 건물 소속 폰 중 가장 가까운 폰을 상주시키도록 요청합니다. 성공 시 true를 반환합니다.
        /// </summary>
        public bool RequestPawn()
        {
            if (!HasSpace || _building == null)
                return false;

            Pawn best = null;
            float bestSqrDist = float.MaxValue;
            foreach (Pawn pawn in FindObjectsByType<Pawn>())
            {
                if (pawn.Owner != _building.Owner || !pawn.IsAvailable)
                    continue;

                float sqrDist = (pawn.transform.position - transform.position).sqrMagnitude;
                if (!(sqrDist < bestSqrDist))
                    continue;

                bestSqrDist = sqrDist;
                best = pawn;
            }

            if (best == null)
                return false;

            best.StationAt(_building);
            return true;
        }

        /// <summary>
        /// 상주 중인 폰 중 가장 최근에 상주 등록된 폰을 해제합니다. 성공 시 true를 반환합니다.
        /// </summary>
        public bool ReleaseOne()
        {
            if (_stationed.Count == 0)
                return false;

            _stationed[^1].Stop();
            return true;
        }

        /// <summary>
        /// 폰을 상주 등록합니다. 이미 상주 중이거나 공간이 없으면 false를 반환합니다.
        /// </summary>
        public bool TryStation(Pawn pawn)
        {
            if (!HasSpace || _stationed.Contains(pawn))
                return false;

            _stationed.Add(pawn);
            CountChanged?.Invoke();
            PawnStationed?.Invoke(pawn);
            return true;
        }

        /// <summary>
        /// 상주 명부에서 폰의 자리 번호를 돌려줍니다. 상주 중이 아니면 -1입니다.
        /// </summary>
        public int IndexOf(Pawn pawn)
            => _stationed.IndexOf(pawn);

        /// <summary>
        /// 폰을 상주 해제합니다. 상주 중이 아니면 아무 일도 일어나지 않습니다.
        /// </summary>
        public void Unstation(Pawn pawn)
        {
            if (_stationed.Remove(pawn))
                CountChanged?.Invoke();
        }

        /// <summary>
        /// 컴포넌트가 파괴될 때 상주 중인 모든 폰을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            Pawn[] snapshot = _stationed.ToArray();
            _stationed.Clear();

            foreach (Pawn pawn in snapshot)
                if (pawn != null)
                    pawn.Stop();
        }
    }
}
