using Code.Core.Bus;
using Code.GameEvents.System;
using Code.Stages;
using Unity.Cinemachine;
using UnityEngine;

namespace Code.Players
{
    /// <summary>
    /// 카메라 화면이 맵 경계 안에 머물도록 지킵니다. 구역제를 쓰는 스테이지에서는 지금 열린 구역이 경계입니다.
    /// </summary>
    public class CameraBoundsClamper
    {
        private readonly CinemachineCamera _cinemachineCamera;
        private readonly UnityEngine.Object _context;

        private Camera _outputCamera;
        private Vector2 _boundsMin;
        private Vector2 _boundsMax;
        private bool _boundsResolved;

        public CameraBoundsClamper(CinemachineCamera cinemachineCamera, UnityEngine.Object context)
        {
            _cinemachineCamera = cinemachineCamera;
            _context = context;

            Bus<MapRegionOpenedEvent>.OnEvent[Owner.Unowned] += HandleMapRegionOpened;
        }

        public void Dispose()
            => Bus<MapRegionOpenedEvent>.OnEvent[Owner.Unowned] -= HandleMapRegionOpened;

        /// <summary>
        /// 실제로 화면을 그리는 카메라입니다. _Core와 스테이지가 따로 올라오므로 지연 해석합니다.
        /// </summary>
        public Camera OutputCamera
            => _outputCamera != null ? _outputCamera : _outputCamera = Camera.main;

        /// <summary>
        /// 카메라 화면 전체가 경계 안에 머물도록 추적 좌표를 제한합니다.
        /// 경계를 화면 절반 크기만큼 안쪽으로 좁혀 클램프하므로 경계 바깥이 보이지 않습니다.
        /// </summary>
        public Vector2 Clamp(Vector2 position)
        {
            ResolveBounds();

            Vector2 extents = ViewExtents(_cinemachineCamera, OutputCamera);
            Vector2 min = _boundsMin + extents;
            Vector2 max = _boundsMax - extents;

            position.x = ClampAxis(position.x, min.x, max.x, _boundsMin.x, _boundsMax.x);
            position.y = ClampAxis(position.y, min.y, max.y, _boundsMin.y, _boundsMax.y);
            return position;
        }

        /// <summary>
        /// 카메라가 허용 범위 밖에 있으면 댐핑을 기다리지 않고 즉시 안으로 끌어옵니다.
        /// 줌 아웃이나 창 크기 변경으로 허용 범위가 갑자기 좁아졌을 때만 동작하며,
        /// 평상시 추적은 Cinemachine의 댐핑에 그대로 맡깁니다.
        /// </summary>
        public void SnapIfOutOfBounds()
        {
            if (_cinemachineCamera == null)
                return;

            Vector3 current = _cinemachineCamera.transform.position;
            Vector2 clamped = Clamp(current);

            if (Mathf.Approximately(clamped.x, current.x) && Mathf.Approximately(clamped.y, current.y))
                return;

            _cinemachineCamera.ForceCameraPosition(
                new Vector3(clamped.x, clamped.y, current.z), _cinemachineCamera.transform.rotation);
        }

        /// <summary>
        /// 지금 줌과 화면 비율에서 카메라가 비추는 영역의 절반 크기입니다.
        /// 실제로 렌더링하는 카메라의 값을 우선 읽습니다 — 뷰포트 비율까지 반영되기 때문입니다.
        /// 기즈모가 플레이 밖에서도 부를 수 있도록 정적입니다.
        /// </summary>
        public static Vector2 ViewExtents(CinemachineCamera cinemachineCamera, Camera output)
        {
            if (output != null)
                return new Vector2(output.orthographicSize * output.aspect, output.orthographicSize);

            if (cinemachineCamera == null)
                return Vector2.zero;

            float halfHeight = cinemachineCamera.Lens.OrthographicSize;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;
            return new Vector2(halfHeight * aspect, halfHeight);
        }

        /// <summary>
        /// 구역이 열리면 다음 클램프에서 경계를 다시 읽게 합니다.
        /// </summary>
        private void HandleMapRegionOpened(MapRegionOpenedEvent evt)
            => _boundsResolved = false;

        /// <summary>
        /// 경계를 한 번만 읽어 둡니다. 매니저가 없으면 스테이지 정의로, 그것도 없으면 경계를 걸지 않습니다 —
        /// 카메라가 한 점에 묶여 아무것도 못 보는 것보다 낫습니다.
        /// </summary>
        private void ResolveBounds()
        {
            if (_boundsResolved)
                return;

            _boundsResolved = true;

            // 구역제를 쓰는 스테이지는 지금 열린 만큼만 본다
            if (MapRegionManager.TryGetCurrentBounds(out Vector2 regionMin, out Vector2 regionMax))
            {
                _boundsMin = regionMin;
                _boundsMax = regionMax;
                return;
            }

            StageSO stage = StageDatabaseSO.Current;
            if (stage == null)
            {
                _boundsMin = new Vector2(float.MinValue * 0.5f, float.MinValue * 0.5f);
                _boundsMax = new Vector2(float.MaxValue * 0.5f, float.MaxValue * 0.5f);
                Debug.LogWarning("이번 씬의 StageSO를 찾지 못해 카메라 경계를 걸지 않습니다. " +
                                 "Resources/StageDatabase에 이 씬이 등록돼 있는지 확인하세요.", _context);
                return;
            }

            _boundsMin = stage.MapMin;
            _boundsMax = stage.MapMax;
        }

        /// <summary>
        /// 한 축을 좁혀진 범위로 클램프합니다.
        /// 경계가 화면보다 좁아 범위가 뒤집히면 어느 쪽도 만족할 수 없으므로 경계 중앙에 고정합니다.
        /// </summary>
        private static float ClampAxis(float value, float min, float max, float boundsMin, float boundsMax)
            => min <= max ? Mathf.Clamp(value, min, max) : (boundsMin + boundsMax) * 0.5f;
    }
}
