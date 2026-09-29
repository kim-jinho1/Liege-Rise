using System;
using UnityEngine;

namespace Code.Navigation
{
    /// <summary>
    /// 깔린 경로를 따라 실제로 걷습니다. 경로 버퍼와 세대 번호를 소유하고,
    /// 진행 상황은 주인인 PathAgent에 되돌려 씁니다.
    /// 경로를 채우는 것(Fill)과 출발시키는 것(Begin)이 따로인 이유는,
    /// Begin이 첫 프레임 몫을 그 자리에서 걸어 버리기 때문입니다 — 에이전트가 남은 거리와
    /// 목적지 표시를 먼저 세워 두지 않으면 그 값들이 한 프레임 뒤처진 채로 덮입니다.
    /// </summary>
    public class PathFollower
    {
        private readonly PathAgent _agent;
        private readonly Vector3[] _path;

        private int _count;
        private int _generation;

        public PathFollower(PathAgent agent, int capacity)
        {
            _agent = agent;
            _path = new Vector3[Mathf.Max(2, capacity)];
        }

        /// <summary>경로 계산이 직접 채워 넣는 버퍼입니다.</summary>
        public Vector3[] Buffer => _path;

        public int Count => _count;

        /// <summary>지금 향하는 마지막 지점입니다. 경로가 없으면 에이전트 자신의 자리입니다.</summary>
        public Vector3 LastPoint => _count > 0 ? _path[_count - 1] : _agent.transform.position;

        /// <summary>
        /// 도는 이동을 버립니다. 세대만 올리면 루프가 다음 프레임에 스스로 빠져나갑니다 —
        /// 취소 토큰을 따로 들고 있지 않아도 되는 이유입니다.
        /// </summary>
        public void Cancel()
        {
            _generation++;
            _count = 0;
        }

        /// <summary>Buffer에 채워 둔 경로를 이 길이로 확정합니다.</summary>
        public void Fill(int count)
            => _count = count;

        /// <summary>두 점을 잇는 직선 경로를 깝니다.</summary>
        public void FillStraight(Vector3 from, Vector3 to)
        {
            _path[0] = from; // 이동 루프는 1번부터 걷는다 — 0번은 출발점 자리다
            _path[1] = to;
            _count = 2;
        }

        /// <summary>깔아 둔 경로를 걷기 시작합니다. 앞선 이동은 여기서 버려집니다.</summary>
        public void Begin()
            => RunAsync(++_generation);

        private async void RunAsync(int generation)
        {
            try
            {
                await WalkAsync(generation);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogError($"PathAgent 이동 오류: {e}");
            }
        }

        private async Awaitable WalkAsync(int generation)
        {
            Transform transform = _agent.transform;

            for (int i = 1; i < _count; i++)
            {
                if (generation != _generation)
                    return;

                Vector3 target = _path[i];
                bool isFinalWaypoint = i == _count - 1;
                float arrivalThreshold =
                    isFinalWaypoint && !_agent.IgnoreStoppingDistance ? _agent.StoppingDistance : 0.1f;

                while (!_agent.IsStopped && generation == _generation)
                {
                    Vector3 current = transform.position;
                    float dist = Vector3.Distance(current, target);

                    if (_count <= 0)
                        return;

                    _agent.RemainingDistance = Vector3.Distance(current, _path[_count - 1]);
                    if (dist <= arrivalThreshold)
                        break;

                    Vector3 dir = (target - current).normalized;
                    _agent.Velocity = dir * _agent.Speed;
                    transform.position = Vector3.MoveTowards(current, target, _agent.Speed * Time.deltaTime);

                    if (_agent.UpdateRotation && dir != Vector3.zero)
                        _agent.FaceDirection(dir);

                    await Awaitable.NextFrameAsync(_agent.destroyCancellationToken);
                }

                if (_agent.IsStopped || generation != _generation)
                    return;
            }

            if (generation == _generation)
                _agent.NotifyArrived();
        }

#if UNITY_EDITOR

        public void DrawGizmos()
        {
            if (_count <= 0)
                return;

            for (int i = 0; i < _count; i++)
            {
                Gizmos.color = i == _count - 1 ? Color.red : Color.blue;
                Gizmos.DrawSphere(_path[i], 0.15f);

                if (i < _count - 1)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(_path[i], _path[i + 1]);
                }
            }
        }

#endif
    }
}
