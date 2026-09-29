using System.Collections.Generic;
using Code.Core;
using Code.Economics;
using Code.Units.Pawns;
using UnityEngine;

namespace Code.Environments
{
    /// <summary>
    /// 맵에 배치된 모든 ResourceObject를 관리하는 싱글톤입니다.
    /// 일꾼 BT에서 가장 가까운 채집 가능 자원을 탐색하는 데 사용됩니다.
    /// </summary>
    public class ResourceObjectManager : MonoSingleton<ResourceObjectManager>
    {
        private readonly List<ResourceObject> _resources = new();

        // 예약 시도용 후보 버퍼. 호출마다 새로 할당하지 않도록 재사용한다.
        private readonly List<ResourceObject> _candidateBuffer = new();

        /// <summary>
        /// 등록된 모든 ResourceObject 읽기 전용 목록입니다.
        /// </summary>
        public IReadOnlyList<ResourceObject> Resources => _resources;

        /// <summary>
        /// ResourceObject를 등록합니다. ResourceObject.Start()에서 호출됩니다.
        /// </summary>
        public void Register(ResourceObject resource)
            => _resources.Add(resource);

        /// <summary>
        /// ResourceObject를 제거합니다. ResourceObject.OnDestroy()에서 호출됩니다.
        /// </summary>
        public void Unregister(ResourceObject resource)
        {
            int index = _resources.IndexOf(resource);
            if (index < 0)
                return;

            int last = _resources.Count - 1;
            _resources[index] = _resources[last];
            _resources.RemoveAt(last);
        }

        /// <summary>
        /// 채집 지점 예약까지 성공하는 가장 가까운 자원을 잡아 줍니다.
        /// 후보가 만석이면 다음으로 가까운 후보로 넘어갑니다.
        /// 가장 가까운 것 하나만 보고 예약 실패에 포기하면, 자원이 고갈돼 여러 일꾼이 같은 프레임에
        /// 재탐색할 때 전원이 같은 노드를 고르고 자리를 못 잡은 쪽이 Stop으로 굳어 버립니다.
        /// 탐색과 예약을 한 번에 처리해 그 틈을 없앱니다.
        /// </summary>
        public bool TryReserveNearest(Pawn pawn, Vector3 worldPos, float radius, ResourceSO filter,
            out ResourceObject result, out int pointIndex)
        {
            result = null;
            pointIndex = -1;

            if (pawn == null)
                return false;

            _candidateBuffer.Clear();
            float sqrRadius = radius * radius;

            foreach (ResourceObject resource in _resources)
            {
                if (resource == null || !resource.IsAvailable)
                    continue;

                if (filter != null && resource.ResourceSO != filter)
                    continue;

                if ((resource.SpawnPosition - worldPos).sqrMagnitude > sqrRadius)
                    continue;

                _candidateBuffer.Add(resource);
            }

            // 가까운 것부터 실제로 자리를 잡아 본다. 자원 수가 적어 선형 최소 탐색으로 충분하다.
            while (_candidateBuffer.Count > 0)
            {
                int best = 0;
                float bestSqr = float.MaxValue;

                for (int i = 0; i < _candidateBuffer.Count; i++)
                {
                    float sqr = (_candidateBuffer[i].SpawnPosition - worldPos).sqrMagnitude;
                    if (sqr >= bestSqr)
                        continue;

                    bestSqr = sqr;
                    best = i;
                }

                ResourceObject candidate = _candidateBuffer[best];
                _candidateBuffer.RemoveAt(best);

                int index = candidate.ReserveGatherPoint(pawn, worldPos);
                if (index < 0)
                    continue;

                result = candidate;
                pointIndex = index;
                return true;
            }

            return false;
        }
    }
}
