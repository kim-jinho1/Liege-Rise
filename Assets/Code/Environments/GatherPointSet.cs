using Code.Navigation.Baking;
using Code.Units.Pawns;
using UnityEngine;

namespace Code.Environments
{
    /// <summary>
    /// 자원 하나가 가진 채집 지점들의 예약을 관리합니다.
    /// 지점 좌표는 자원이 들고 있고, 여기서는 누가 어느 자리를 잡았는지만 셉니다.
    /// </summary>
    public class GatherPointSet
    {
        private readonly Transform[] _points;
        private readonly Pawn[] _holders;
        private readonly Transform _fallback;

        /// <summary>
        /// 채집 지점 배열과, 지점이 없을 때 대신 쓸 좌표를 받습니다.
        /// </summary>
        public GatherPointSet(Transform[] points, Transform fallback)
        {
            _points = points;
            _fallback = fallback;
            _holders = new Pawn[Mathf.Max(1, points?.Length ?? 0)];
        }

        /// <summary>
        /// 동시에 붙을 수 있는 일꾼 수입니다.
        /// </summary>
        public int Capacity => _holders.Length;

        /// <summary>
        /// 빈자리가 하나라도 있으면 true입니다.
        /// </summary>
        public bool HasFreePoint()
        {
            foreach (Pawn holder in _holders)
                if (holder == null)
                    return true;

            return false;
        }

        /// <summary>
        /// 해당 일꾼이 자리를 잡고 있으면 true입니다.
        /// </summary>
        public bool Holds(Pawn pawn)
        {
            if (pawn == null)
                return false;

            foreach (Pawn holder in _holders)
                if (holder == pawn)
                    return true;

            return false;
        }

        /// <summary>
        /// 인덱스에 해당하는 채집 지점의 월드 좌표입니다.
        /// </summary>
        public Vector3 PositionOf(int index)
            => _points != null && index >= 0 && index < _points.Length
                ? _points[index].position
                : _fallback.position;

        /// <summary>
        /// fromPosition에서 가장 가까운 빈 지점을 예약하고 인덱스를 반환합니다.
        /// 통행 가능한 지점을 우선하며, 빈자리가 없으면 -1입니다.
        /// </summary>
        public int Reserve(Pawn pawn, Vector3 fromPosition)
        {
            if (pawn == null)
                return -1;

            int bestAny = -1, bestWalkable = -1;
            float minAnySqr = float.MaxValue, minWalkableSqr = float.MaxValue;

            for (int i = 0; i < _holders.Length; i++)
            {
                Pawn holder = _holders[i];
                if (holder != null && holder != pawn)
                    continue; // 비었거나 이미 자기 자리인 지점만 후보로 본다

                Vector3 point = PositionOf(i);
                float sqr = (point - fromPosition).sqrMagnitude;

                if (sqr < minAnySqr)
                {
                    minAnySqr = sqr;
                    bestAny = i;
                }

                if (sqr < minWalkableSqr && IsWalkable(point))
                {
                    minWalkableSqr = sqr;
                    bestWalkable = i;
                }
            }

            // 통행 가능한 자리가 하나도 없으면 예약하지 않는다. 아무 자리나 주면 건물 발자국 위 자리가 걸려
            // 폰이 건물 벽 앞 엉뚱한 곳에서 캔다. 베이크 데이터가 없을 때만 IsWalkable이 전부 참이라 bestAny가 쓰인다
            int chosen = bestWalkable >= 0 ? bestWalkable : (PathBaker.Instance == null ? bestAny : -1);
            if (chosen < 0)
                return -1;

            Release(pawn); // 같은 자원 안의 이전 예약을 정리
            _holders[chosen] = pawn;
            return chosen;
        }

        /// <summary>
        /// 해당 일꾼의 예약을 해제합니다. 예약이 없으면 아무 일도 하지 않습니다.
        /// </summary>
        public void Release(Pawn pawn)
        {
            if (pawn == null)
                return;

            for (int i = 0; i < _holders.Length; i++)
                if (_holders[i] == pawn)
                    _holders[i] = null;
        }

        /// <summary>
        /// 해당 좌표의 셀이 경로 그래프에 통행 가능한 노드로 있는지 봅니다.
        /// PathBaker가 없어 검증할 수 없으면 기존 동작 유지를 위해 true입니다.
        /// </summary>
        private static bool IsWalkable(Vector3 worldPos)
        {
            PathBaker baker = PathBaker.Instance;
            if (baker == null || baker.BakedData == null || baker.BaseTilemap == null)
                return true;

            Vector3Int cell = baker.BaseTilemap.WorldToCell(worldPos);
            return baker.BakedData.HasNode(cell);
        }
    }
}
