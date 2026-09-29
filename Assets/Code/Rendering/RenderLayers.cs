using UnityEngine;

namespace Code.Rendering
{
    /// <summary>
    /// 런타임에 만드는 월드 표시(진격 화살표·분대 깃발·목적지 깃발)가 쓰는 레이어를 모아 둡니다.
    /// </summary>
    public static class RenderLayers
    {
        /// <summary>
        /// 유닛·건물과 같은 정렬 레이어입니다. 위아래는 정렬 순서로 가릅니다 —
        /// 음수면 유닛 밑에, 큰 양수면 유닛 위에 깔립니다.
        /// </summary>
        public const string EntitySorting = "Entity";

        // 경로·명령 표시용 GameObject 레이어. 메인 카메라는 그리고 미니맵·안개 카메라는 그리지 않는다
        private const string MarkerLayerName = "PathLine";

        /// <summary>
        /// 새로 만든 표시 오브젝트를 카메라가 그리는 레이어에 올립니다.
        /// GameObject는 Default(0)로 태어나는데 메인 카메라 컬링 마스크에 Default가 없습니다.
        /// 정렬 레이어와 순서를 아무리 맞춰도 이 레이어가 틀리면 화면에 아무것도 나오지 않습니다.
        /// 눈에 안 보이는 실패라 세 번 반복해 밟았습니다 — 그래서 설명이 여기 한 번만 있습니다.
        /// </summary>
        public static void PlaceOnMarkerLayer(GameObject target, GameObject fallbackSource = null)
        {
            if (target == null)
                return;

            int layer = LayerMask.NameToLayer(MarkerLayerName);
            if (layer >= 0)
            {
                target.layer = layer;
                return;
            }

            // 프로젝트 설정에서 레이어가 사라졌다면 최소한 원본을 따라간다. Default에 두면 안 그려진다
            if (fallbackSource != null)
                target.layer = fallbackSource.layer;
        }
    }
}
