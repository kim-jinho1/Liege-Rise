using System;
using UnityEngine;

namespace Code.Effects
{
    /// <summary>
    /// 선택/호버 시 본체 스프라이트 바깥에 단색 외곽선을 그리는 연출 컴포넌트입니다.
    /// </summary>
    public class SelectionOutlineEffect : MonoBehaviour
    {
        private const string ShaderName = "Custom/SelectionOutlineShader";
        private const string MaterialResourcePath = "SelectionOutline";
        private const string OverlayName = "__SelectionOutline";
        private const float OutlinePixels = 2f;
        private static readonly Color DefaultOutlineColor = Color.yellow;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        // 외곽선 두께를 균일하게 하기 위한 8방향(대각선은 정규화해 같은 거리로 사용).
        private static readonly Vector2[] Directions =
        {
            new(1f, 0f), new(-1f, 0f), new(0f, 1f), new(0f, -1f),
            new(1f, 1f), new(1f, -1f), new(-1f, 1f), new(-1f, -1f),
        };

        // 외곽선 실루엣은 색만 다를 뿐 동일하므로 모든 복제본이 한 머티리얼을 공유합니다.
        private static Material _sharedMaterial;

        private SpriteRenderer[] _bodyRenderers = Array.Empty<SpriteRenderer>();
        private SpriteRenderer[][] _overlays = Array.Empty<SpriteRenderer[]>();
        private MaterialPropertyBlock _propertyBlock;
        private Color _selectedColor = DefaultOutlineColor;
        private Color _outlineColor = DefaultOutlineColor;
        private float _hoverAlphaScale = 0.55f; // 진영 팔레트가 준다. 못 받아도 그림이 죽지 않게 기본값을 둔다
        private bool _isSelected;
        private bool _isHovered;
        private bool _isFlagged;
        private bool _isShown;
        private bool _overlaysBuilt;

        private Sprite[] _syncedSprites;
        private bool[] _syncedFlipX;
        private bool[] _syncedFlipY;
        private bool[] _syncedVisible;
        private int[] _syncedOrder;
        private bool[] _hasBaseline;

        // 아무것도 안 보여 주는 상태로 태어난다. Refresh가 켤 때까지 LateUpdate를 돌 이유가 없다
        private void Awake()
            => enabled = false;

        /// <summary>
        /// 효과 대상이 되는 본체 렌더러와 외곽선 색, 호버 시 알파 배율을 등록합니다.
        /// </summary>
        public void Initialize(SpriteRenderer[] bodyRenderers, Color outlineColor, float hoverAlphaScale)
        {
            _bodyRenderers = bodyRenderers ?? Array.Empty<SpriteRenderer>();
            _selectedColor = outlineColor;
            _outlineColor = outlineColor;
            _hoverAlphaScale = Mathf.Clamp01(hoverAlphaScale);
        }

        /// <summary>
        /// 외곽선 표시 상태를 토글합니다.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_isSelected == selected)
                return;

            _isSelected = selected;
            Refresh();
        }

        /// <summary>
        /// 커서가 올라와 있는 동안의 외곽선을 토글합니다. 선택과 별개로 기억합니다.
        /// </summary>
        public void SetHovered(bool hovered)
        {
            if (_isHovered == hovered)
                return;

            _isHovered = hovered;
            Refresh();
        }

        /// <summary>
        /// 시퀀스가 잠깐 짚어 주는 외곽선을 토글합니다.
        /// 선택·호버와 따로 기억해야 안내가 끝났다고 진짜 선택까지 풀리지 않습니다.
        /// </summary>
        public void SetFlagged(bool flagged)
        {
            if (_isFlagged == flagged)
                return;

            _isFlagged = flagged;
            Refresh();
        }

        /// <summary>
        /// 선택·호버·안내 세 상태를 합쳐 외곽선을 켜고 색을 정합니다.
        /// </summary>
        private void Refresh()
        {
            // 선택이 호버를 이긴다 — 고른 것을 다시 가리켰다고 흐려지면 안 된다
            Color wanted = _isSelected || _isFlagged
                ? _selectedColor
                : new Color(_selectedColor.r, _selectedColor.g, _selectedColor.b,
                    _selectedColor.a * _hoverAlphaScale);

            bool colorChanged = wanted != _outlineColor;
            _outlineColor = wanted;

            bool show = _isSelected || _isHovered || _isFlagged;
            if (!show)
            {
                if (!_isShown)
                    return;

                _isShown = false;
                DisableOverlays();

                // 안 보이는 동안은 컴포넌트를 끈다. 이 효과는 유닛·건물·자원 노드 전부에 붙어
                // 400개 남짓이 매 프레임 "보여줄 게 없다"만 확인하고 빠져나가고 있었다
                enabled = false;
                return;
            }

            if (!_overlaysBuilt)
                BuildOverlays();

            // 색만 바뀌면 SyncOverlays가 기준 비교에서 걸러내므로 기준을 무효화한다
            if (colorChanged && _hasBaseline != null)
                Array.Clear(_hasBaseline, 0, _hasBaseline.Length);

            _isShown = true;
            enabled = true; // Refresh는 컴포넌트가 꺼져 있어도 불리므로 여기가 다시 켜는 유일한 문이다
            SyncOverlays();
        }

        private void LateUpdate()
        {
            if (_isShown)
                SyncOverlays();
        }

        /// <summary>
        /// 본체 렌더러마다 8방향 외곽선 복제본을 1회 생성합니다.
        /// </summary>
        private void BuildOverlays()
        {
            _overlaysBuilt = true;
            _propertyBlock ??= new MaterialPropertyBlock();
            EnsureSharedMaterial();

            int bodyCount = _bodyRenderers.Length;
            _overlays = new SpriteRenderer[bodyCount][];
            _syncedSprites = new Sprite[bodyCount];
            _syncedFlipX = new bool[bodyCount];
            _syncedFlipY = new bool[bodyCount];
            _syncedVisible = new bool[bodyCount];
            _syncedOrder = new int[bodyCount];
            _hasBaseline = new bool[bodyCount];

            for (int i = 0; i < _bodyRenderers.Length; i++)
            {
                SpriteRenderer body = _bodyRenderers[i];
                if (body == null)
                    continue;

                SpriteRenderer[] copies = new SpriteRenderer[Directions.Length];
                for (int d = 0; d < Directions.Length; d++)
                {
                    GameObject overlayObject = new GameObject(OverlayName);
                    overlayObject.transform.SetParent(body.transform, false);

                    SpriteRenderer overlay = overlayObject.AddComponent<SpriteRenderer>();
                    overlay.sharedMaterial = _sharedMaterial;
                    overlay.enabled = false;
                    copies[d] = overlay;
                }

                _overlays[i] = copies;
            }
        }

        /// <summary>
        /// 외곽선 머티리얼을 1회 확보합니다. Resources에 두어 빌드 시 셰이더 스트리핑을 방지합니다.
        /// </summary>
        private void EnsureSharedMaterial()
        {
            if (_sharedMaterial != null)
                return;

            _sharedMaterial = Resources.Load<Material>(MaterialResourcePath);
            if (_sharedMaterial == null)
                _sharedMaterial = new Material(Shader.Find(ShaderName)) { name = "SelectionOutline (Fallback)" };
        }

        /// <summary>
        /// 외곽선 복제본이 본체의 현재 스프라이트·반전·정렬·가시성을 따라가도록 갱신합니다.
        /// </summary>
        private void SyncOverlays()
        {
            for (int i = 0; i < _overlays.Length; i++)
            {
                SpriteRenderer[] copies = _overlays[i];
                SpriteRenderer body = _bodyRenderers[i];
                if (copies == null || body == null)
                    continue;

                Sprite sprite = body.sprite;
                bool visible = body.enabled && body.gameObject.activeInHierarchy && sprite != null;

                // 스프라이트·반전·가시성·정렬이 지난 동기화와 동일하면 이 본체의 오버레이 갱신을 건너뜁니다.
                // 정렬 순서를 빼면 안 된다 — 자기 sortingOrder를 매 프레임 다시 매기는 본체(SheepView)에서
                // 외곽선만 옛 순서에 굳어, 본체가 그 위로 올라오는 프레임마다 실루엣이 앞을 덮어 깜박인다
                if (_hasBaseline[i] && _syncedVisible[i] == visible &&
                    ReferenceEquals(_syncedSprites[i], sprite) &&
                    _syncedFlipX[i] == body.flipX && _syncedFlipY[i] == body.flipY &&
                    _syncedOrder[i] == body.sortingOrder)
                    continue;

                _hasBaseline[i] = true;
                _syncedSprites[i] = sprite;
                _syncedFlipX[i] = body.flipX;
                _syncedFlipY[i] = body.flipY;
                _syncedVisible[i] = visible;
                _syncedOrder[i] = body.sortingOrder;

                float worldOffset = visible ? OutlinePixels / sprite.pixelsPerUnit : 0f;

                for (int d = 0; d < copies.Length; d++)
                {
                    SpriteRenderer overlay = copies[d];
                    if (overlay == null)
                        continue;

                    overlay.enabled = visible;
                    if (!visible)
                        continue;

                    overlay.sprite = sprite;
                    overlay.flipX = body.flipX;
                    overlay.flipY = body.flipY;

                    overlay.sortingLayerID = body.sortingLayerID;
                    overlay.sortingOrder = body.sortingOrder - 1;
                    overlay.gameObject.layer = body.gameObject.layer;

                    overlay.GetPropertyBlock(_propertyBlock);
                    _propertyBlock.SetColor(ColorId, _outlineColor);
                    _propertyBlock.SetTexture(MainTexId, sprite.texture);
                    overlay.SetPropertyBlock(_propertyBlock);

                    Vector2 offset = Directions[d].normalized * worldOffset;
                    overlay.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
                }
            }
        }

        private void DisableOverlays()
        {
            foreach (SpriteRenderer[] copies in _overlays)
            {
                if (copies == null)
                    continue;

                foreach (SpriteRenderer overlay in copies)
                    if (overlay != null)
                        overlay.enabled = false;
            }

            // 다음 선택 시 오버레이를 다시 켜야 하므로 기준 상태를 무효화합니다.
            if (_hasBaseline != null)
                Array.Clear(_hasBaseline, 0, _hasBaseline.Length);
        }
    }
}
