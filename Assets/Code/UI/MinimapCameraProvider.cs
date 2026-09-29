using Code.Bootstraps;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.System;
using Code.Stages;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI
{
    /// <summary>
    /// 미니맵 카메라를 다른 씬의 UI가 런타임에 참조할 수 있도록 노출하는 마커 싱글톤입니다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MinimapCameraProvider : MonoSingleton<MinimapCameraProvider>, IGameStartListener
    {
        // 지도를 담는 칸들. 비율을 맵에 맞춰야 구역 밖 지형이 미니맵에 비치지 않는다.
        // 지도와 마커 칸이 따로라 둘 다 같은 값을 받아야 좌표가 어긋나지 않는다
        [SerializeField] private AspectRatioFitter[] windowFitters;

        /// <summary>
        /// 이 컴포넌트가 부착된 미니맵 카메라입니다.
        /// </summary>
        public Camera Camera { get; private set; }

        public int InitOrder => 0;

        protected override void Awake()
        {
            base.Awake();
            Camera = GetComponent<Camera>();
            Bus<MapRegionOpenedEvent>.OnEvent[Owner.Unowned] += HandleMapRegionOpened;
        }

        protected override void OnDestroy()
        {
            Bus<MapRegionOpenedEvent>.OnEvent[Owner.Unowned] -= HandleMapRegionOpened;
            base.OnDestroy();
        }

        private void HandleMapRegionOpened(MapRegionOpenedEvent evt)
            => FitToMap();

        /// <summary>
        /// 미니맵을 이번 맵에 맞춥니다. _Core는 상주 씬이라 맵마다 다른 값을 박아 둘 수 없습니다.
        /// 크기는 스테이지 정의에서, 색은 그 맵의 메인 카메라에서 가져옵니다 —
        /// 색은 씬 뷰에서 눈으로 맞추는 값이라 씬에 두는 편이 낫습니다.
        /// </summary>
        public void OnGameStart()
        {
            AdoptBackgroundColor();
            FitToMap();
        }

        private void AdoptBackgroundColor()
        {
            // Camera 프로퍼티가 타입 이름을 가리므로 전체 이름으로 부른다
            Camera stage = Camera.main;
            if (stage == null)
                return;

            // 미니맵은 렌더 텍스처로 그려서 알파가 그대로 남는다.
            // 메인 카메라 색은 알파가 0인데(화면에 직접 그릴 땐 무시된다) 그대로 쓰면 빈 곳이 비쳐 보인다
            Color color = stage.backgroundColor;
            color.a = 1f;
            Camera.backgroundColor = color;
        }

        /// <summary>
        /// 미니맵을 지금 열려 있는 범위에 맞춥니다. 구역이 열릴 때마다 다시 불립니다.
        /// </summary>
        public void FitToMap()
        {
            if (!TryResolveBounds(out Vector2 min, out Vector2 max))
                return;

            Vector2 center = (min + max) * 0.5f;
            Vector3 position = transform.position;
            transform.position = new Vector3(center.x, center.y, position.z);

            Vector2 size = max - min;
            if (windowFitters == null || windowFitters.Length == 0)
            {
                // 칸 모양을 못 바꾸면 좁은 축이 잘리지 않도록 큰 쪽에 맞춘다.
                // 이때 남는 자리에는 구역 밖 지형이 비치므로, 갈 수 없는 땅이 미니맵에 보인다
                float half = Mathf.Max(size.x / Mathf.Max(Camera.aspect, 0.0001f), size.y) * 0.5f;
                Camera.orthographicSize = Mathf.Max(half, 0.0001f);
                return;
            }

            // 칸 비율과 카메라 비율은 반드시 같이 세팅한다 — 하나만 바꾸면 미니맵이 늘어난다
            float ratio = size.x / Mathf.Max(size.y, 0.0001f);
            foreach (AspectRatioFitter fitter in windowFitters)
                if (fitter != null)
                    fitter.aspectRatio = ratio;

            Camera.aspect = ratio;
            Camera.orthographicSize = Mathf.Max(size.y * 0.5f, 0.0001f);
        }

        /// <summary>
        /// 지금 비출 범위를 읽습니다. 구역 매니저가 있으면 그쪽이, 없으면 스테이지 전체가 기준입니다.
        /// </summary>
        private bool TryResolveBounds(out Vector2 min, out Vector2 max)
        {
            if (MapRegionManager.TryGetCurrentBounds(out min, out max))
                return true;

            StageSO stage = StageDatabaseSO.Current;
            if (stage == null)
                    return false;

            min = stage.MapMin;
            max = stage.MapMax;
            return true;
        }
    }
}
