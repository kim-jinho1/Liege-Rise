using Code.Core.Bus;
using Code.FogSystem;
using Code.GameEvents.System;
using Code.Rendering;
using UnityEngine;

namespace Code.Buildings.Base
{
    /// <summary>
    /// 파괴된 건물이 남기는 잔해입니다. 서서히 투명해진 뒤 사라집니다.
    /// 시야를 잃으면 건물과 같은 방식으로 잔상을 남기므로, 안개 속에서 잔해가 사라지는 장면은 보이지 않습니다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BuildingRuins : MonoBehaviour, IHideable
    {
        public Transform Transform
            => this == null ? null : transform;

        public bool IsVisible { get; private set; } = true;

        public event IHideable.VisibilityChangedEvent OnVisibilityChanged;

        private const string GameObjectName = "BuildingRuins";
        private SpriteRenderer _renderer;
        private Color _baseColor;
        private Owner _owner;
        private float _lifetime;
        private float _elapsed;
        private bool _registered;

        /// <summary>
        /// 파괴된 건물 자리에 잔해를 만듭니다. 원본의 정렬 레이어를 물려받고 순서는 스스로 Y 정렬합니다.
        /// </summary>
        public static void Spawn(Sprite sprite, SpriteRenderer source, Vector3 position, float lifetime, Owner owner)
        {
            if (sprite == null || lifetime <= 0f)
                return;

            GameObject renderObject = new GameObject(GameObjectName)
            {
                transform =
                {
                    position = position
                }
            };

            SpriteRenderer renderer = renderObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            if (source != null)
            {
                renderObject.transform.localScale = source.transform.lossyScale;
                renderer.flipX = source.flipX;
            }

            // 정렬 부품은 스프라이트와 스케일이 정해진 뒤에 붙인다 — YSort가 Awake에서 바운드를 읽는다.
            SortingCopier.MakeSelfSorting(renderObject, source);

            renderObject.AddComponent<BuildingRuins>().Initialize(lifetime, owner);
        }

        /// <summary>
        /// 잔해의 지속 시간을 설정합니다. 0 이하의 값은 0.01초로 보정됩니다.
        /// </summary>
        public void Initialize(float lifetime, Owner owner)
        {
            _lifetime = Mathf.Max(0.01f, lifetime);
            _elapsed = 0f;
            _owner = owner;
        }

        /// <summary>
        /// FogVisibilityManager가 호출합니다. 건물과 같은 경로로 잔상을 남기고 걷습니다.
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            if (isVisible == IsVisible)
                return;

            IsVisible = isVisible;
            _renderer.enabled = isVisible;

            // 이 이벤트가 PlaceHolderManager를 깨워 마지막으로 본 모습을 맵에 남긴다.
            // 잔상이 없으면 안개 속에서 잔해가 옅어지다 사라지는 장면이 그대로 보인다.
            Bus<VisibleStateChangeEvent>.Raise(_owner,
                new VisibleStateChangeEvent(_owner, transform, _renderer, isVisible));

            OnVisibilityChanged?.Invoke(this, isVisible);
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _baseColor = _renderer.color;
        }

        private void Start()
        {
            // 안개가 없는 씬에서는 등록할 곳이 없다. 그때는 계속 보이는 것이 맞다.
            FogVisibilityManager fog = FogVisibilityManager.Instance;
            if (fog == null)
                return;

            fog.Register(this);
            _registered = true;
        }

        private void OnDestroy()
        {
            if (_registered && FogVisibilityManager.Instance != null)
                FogVisibilityManager.Instance.Unregister(this);
        }

        private void Update()
        {
            // 시야와 무관하게 흐른다. 안개 밖에서 멈춰 두면 되돌아가지 않는 잔해가 영원히 남는다.
            // 안 보이는 동안 사라져도 잔상이 마지막 모습을 대신 들고 있다.
            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float remaining = 1f - _elapsed / _lifetime;
            _renderer.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b,
                _baseColor.a * remaining * remaining);
        }
    }
}
