using Code.Navigation.Baking;
using Code.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace Code.Navigation
{
    /// <summary>
    /// 목적지 깃발의 등장·퇴장 값입니다. 눈으로 맞추는 값이라 소유자인
    /// PathVisualizer의 인스펙터가 쥡니다.
    /// </summary>
    [System.Serializable]
    public class DestinationMarkerMotion
    {
        // 깃발 밑동을 목표점에 맞추는 세로 보정. 0이면 스프라이트 가운데가 목표점에 온다
        [SerializeField] private float groundOffset;
        [SerializeField, Min(0f)] private float dropHeight = 1.2f;
        [SerializeField, Min(0.01f)] private float dropDuration = 0.22f;
        [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.18f;

        public float GroundOffset => groundOffset;
        public float DropHeight => dropHeight;
        public float DropDuration => dropDuration;
        public float FadeOutDuration => fadeOutDuration;
    }

    /// <summary>
    /// 이동 명령이 떨어진 자리에 꽂히는 깃발입니다.
    /// 위에서 떨어지며 나타나고 사라질 때는 흐려집니다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DestinationMarker : MonoBehaviour
    {
        private const string GameObjectName = "DestinationMarker";

        private SpriteRenderer _renderer;
        private DestinationMarkerMotion _motion;
        private Vector3 _groundPoint;
        private float _elapsed;
        private bool _fadingOut;

        /// <summary>
        /// 깃발 오브젝트를 만들어 돌려줍니다. 스프라이트가 없으면 만들지 않습니다.
        /// </summary>
        public static DestinationMarker Create(Sprite sprite, DestinationMarkerMotion motion)
        {
            if (sprite == null)
                return null;

            GameObject markerObject = new GameObject(GameObjectName);
            RenderLayers.PlaceOnMarkerLayer(markerObject);

            SpriteRenderer renderer = markerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            // 유닛과 같은 레이어에서 Y로 겹침을 가린다 — 앞에 선 유닛이 깃발을 가려야 자연스럽다
            SortingGroup group = markerObject.AddComponent<SortingGroup>();
            group.sortingLayerName = RenderLayers.EntitySorting;
            markerObject.AddComponent<YSortByPosition>();

            DestinationMarker marker = markerObject.AddComponent<DestinationMarker>();
            marker._motion = motion ?? new DestinationMarkerMotion();
            markerObject.SetActive(false);
            return marker;
        }

        /// <summary>
        /// 지정한 지점에 깃발을 꽂습니다. 이미 떠 있으면 자리만 옮기고 낙하를 다시 재생합니다.
        /// </summary>
        public void Show(Vector3 point)
        {
            EnsureRenderer();

            Vector3 center = SnapToCellCenter(point);

            // 깃발 밑동을 목표점에 맞추는 보정. 0이면 스프라이트 가운데가 목표점에 온다
            _groundPoint = new Vector3(center.x, center.y + _motion.GroundOffset, point.z);
            _elapsed = 0f;
            _fadingOut = false;

            gameObject.SetActive(true);
            Apply(0f);
        }

        /// <summary>
        /// 찍은 자리를 그 점이 속한 타일의 중앙으로 옮깁니다.
        /// 칸 경계에 걸쳐 서면 어느 칸을 찍은 것인지 눈으로 알 수 없고, 같은 칸을 두 번 찍어도
        /// 깃발이 조금씩 다른 자리에 섭니다. 타일맵을 못 찾으면 찍은 자리를 그대로 씁니다.
        /// </summary>
        private static Vector3 SnapToCellCenter(Vector3 point)
        {
            Tilemap map = PathBaker.Instance != null ? PathBaker.Instance.BaseTilemap : null;
            if (map == null)
                return point;

            return map.GetCellCenterWorld(map.WorldToCell(point));
        }

        /// <summary>
        /// 깃발을 흐려 지웁니다. 이미 사라지는 중이면 그대로 둡니다.
        /// </summary>
        public void Hide()
        {
            if (!gameObject.activeSelf || _fadingOut)
                return;

            _fadingOut = true;
            _elapsed = 0f;
        }

        private void Awake()
            => EnsureRenderer();

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (_fadingOut)
            {
                float t = Mathf.Clamp01(_elapsed / _motion.FadeOutDuration);
                SetAlpha(1f - t);
                if (t >= 1f)
                    gameObject.SetActive(false);

                return;
            }

            Apply(Mathf.Clamp01(_elapsed / _motion.DropDuration));
        }

        /// <summary>
        /// 낙하 진행도 t(0~1)에 맞춰 높이와 알파를 적용합니다.
        /// </summary>
        private void Apply(float t)
        {
            // 뒤로 갈수록 느려지는 대신 앞이 빠르다 — 떨어져 꽂히는 느낌은 이쪽이다
            float eased = 1f - (1f - t) * (1f - t);
            transform.position = _groundPoint + Vector3.up * (_motion.DropHeight * (1f - eased));
            SetAlpha(t);
        }

        private void SetAlpha(float alpha)
        {
            Color color = _renderer.color;
            color.a = Mathf.Clamp01(alpha);
            _renderer.color = color;
        }

        private void EnsureRenderer()
        {
            if (_renderer != null)
                return;

            _renderer = GetComponent<SpriteRenderer>();
        }
    }
}
