using System.Collections.Generic;
using Code.EnemyAI;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Alerts
{
    /// <summary>
    /// 진행 중인 공세 부대와 그 목표를 미니맵에 표시합니다.
    /// 위치를 AssaultForce에서 직접 읽으므로 안개에 가려져도 계속 보입니다.
    /// </summary>
    public class AssaultMinimapUI : MonoBehaviour
    {
        [Header("Minimap")]
        [SerializeField] private RectTransform markerRoot; // 미니맵 이미지와 같은 영역을 덮는 컨테이너. 좌표 계산의 기준이다

        [Header("Markers")]
        [SerializeField] private RectTransform forceMarkerPrefab;
        [SerializeField] private RectTransform targetMarkerPrefab;

        [Header("Force Marker Size")]
        [SerializeField] private float minScale = 1f;
        [SerializeField] private float maxScale = 1.8f;
        [SerializeField, Min(1)] private int strengthForMaxScale = 20; // 이 병력 수에서 마커가 최대 크기가 된다

        private readonly List<RectTransform> _forceMarkers = new(8);
        private readonly List<RectTransform> _targetMarkers = new(8);

        private Camera _minimapCamera;

        private void LateUpdate()
        {
            if (markerRoot == null || forceMarkerPrefab == null || targetMarkerPrefab == null)
                return;

            if (!EnsureCamera())
                return;

            int forceUsed = 0;
            int targetUsed = 0;

            IReadOnlyList<AssaultForce> forces = AssaultRegistry.Forces;
            for (int i = 0; i < forces.Count; i++)
            {
                AssaultForce force = forces[i];
                if (force == null)
                    continue;

                if (TryToMarkerLocal(force.Center, out Vector2 forcePosition))
                {
                    RectTransform marker = GetMarker(_forceMarkers, forceMarkerPrefab, forceUsed++);
                    marker.anchoredPosition = forcePosition;
                    marker.localScale = Vector3.one * ScaleFor(force.Strength);
                }

                // 목표를 잃은 부대는 목적지 마커를 그리지 않는다 — 남은 기지가 없다는 뜻이다
                if (force.Target == null)
                    continue;

                if (TryToMarkerLocal(force.Target.transform.position, out Vector2 targetPosition))
                    GetMarker(_targetMarkers, targetMarkerPrefab, targetUsed++)
                        .anchoredPosition = targetPosition;
            }

            HideFrom(_forceMarkers, forceUsed);
            HideFrom(_targetMarkers, targetUsed);
        }

        /// <summary>
        /// 월드 좌표를 마커 컨테이너 안의 로컬 좌표로 바꿉니다. 맵 밖이면 false입니다.
        /// </summary>
        private bool TryToMarkerLocal(Vector3 world, out Vector2 local)
        {
            local = default;
            Vector3 viewport = _minimapCamera.WorldToViewportPoint(world);

            // 맵 밖은 그리지 않는다 — 마스크가 잘라 주지만 마커가 가장자리에 눌어붙는다
            if (viewport.x is < 0f or > 1f || viewport.y is < 0f or > 1f)
                return false;

            Rect rect = markerRoot.rect;
            local = new Vector2(
                (viewport.x - 0.5f) * rect.width,
                (viewport.y - 0.5f) * rect.height);

            return true;
        }

        /// <summary>
        /// 미니맵 카메라를 확보합니다. _Core에 있지만 이 컴포넌트보다 늦게 설 수 있습니다.
        /// </summary>
        private bool EnsureCamera()
        {
            if (_minimapCamera != null)
                return true;

            if (MinimapCameraProvider.Instance != null)
                _minimapCamera = MinimapCameraProvider.Instance.Camera;

            return _minimapCamera != null;
        }

        /// <summary>
        /// 마커를 가져옵니다.
        /// </summary>
        private RectTransform GetMarker(List<RectTransform> markers, RectTransform prefab, int index)
        {
            while (markers.Count <= index)
                markers.Add(CreateMarker(prefab));

            RectTransform marker = markers[index];
            if (!marker.gameObject.activeSelf)
                marker.gameObject.SetActive(true);

            return marker;
        }

        /// <summary>
        /// 마커를 새로 생성합니다.
        /// </summary>
        private RectTransform CreateMarker(RectTransform prefab)
        {
            RectTransform marker = Instantiate(prefab, markerRoot);

            // 로컬 좌표 계산이 중앙 기준이므로 앵커도 중앙으로 고정한다
            marker.anchorMin = marker.anchorMax = marker.pivot = new Vector2(0.5f, 0.5f);

            // 마커가 커서를 먹으면 미니맵 클릭으로 카메라를 옮길 수 없다
            foreach (Graphic graphic in marker.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;

            return marker;
        }

        /// <summary>
        /// 병력 수에 따라 마커 크기를 조정합니다.
        /// strengthForMaxScale 이상이면 maxScale, 0이면 minScale입니다.
        /// </summary>
        private float ScaleFor(int strength)
        {
            float t = Mathf.Clamp01((float)strength / strengthForMaxScale);
            return Mathf.Lerp(minScale, maxScale, t);
        }

        /// <summary>
        /// 마커 리스트에서 index 이후의 마커를 모두 숨깁니다.
        /// </summary>
        private static void HideFrom(List<RectTransform> markers, int index)
        {
            for (int i = index; i < markers.Count; i++)
                if (markers[i] != null && markers[i].gameObject.activeSelf)
                    markers[i].gameObject.SetActive(false);
        }
    }
}
