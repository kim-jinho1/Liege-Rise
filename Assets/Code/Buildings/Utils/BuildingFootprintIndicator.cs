using System.Collections.Generic;
using Code.Buildings.Base;
using Code.Buildings.Construction;
using Code.Buildings.Data;
using Code.Commandable;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Buildings.Utils
{
    /// <summary>
    /// 건물이 차지하는 타일 영역을 외곽선으로 표시합니다.
    /// 그리는 자리는 BuildingFootprint.CellBounds가 정합니다 — transform.position을
    /// 그대로 중심으로 그리면 건물이 칸 중앙에 안 서 있을 때 실제 점유와 어긋납니다.
    /// 평소에는 선택했을 때만 보이고, 건물을 놓는 동안에는 ShowAll로 전부 켜집니다.
    /// </summary>
    public class BuildingFootprintIndicator : MonoBehaviour
    {
        private const int PixelsPerCell = 16;
        private const int BorderThickness = 2; // 픽셀. 크기 계산은 PixelsPerCell이 쥐고 있어 여기와 무관하다

        private static readonly Color FillColor = new(0.25f, 0.55f, 0.9f, 0.18f);
        private static readonly Color BorderColor = new(0.4f, 0.7f, 1f, 0.55f);
        private static readonly Dictionary<Vector2Int, Sprite> SpriteCache = new();

        // 살아 있는 표시 전부. 배치 중에 이미 놓인 건물들을 한꺼번에 켜기 위해 들고 있는다
        private static readonly List<BuildingFootprintIndicator> Live = new(64);
        private static bool _forceVisible;

        private SpriteRenderer _renderer;
        private AbstractCommandable _commandable;
        private Vector2Int _size;
        private bool _initialized;
        private bool _selected;

        /// <summary>
        /// 이미 놓인 건물들의 발자국을 전부 켜거나 끕니다. 새 건물을 놓는 동안 어디가 찼는지 보여 줍니다.
        /// </summary>
        public static void ShowAll(bool visible)
        {
            _forceVisible = visible;

            for (int i = Live.Count - 1; i >= 0; i--)
            {
                if (Live[i] == null)
                {
                    Live.RemoveAt(i);
                    continue;
                }

                Live[i].Refresh();
            }
        }

        private void Awake()
        {
            Live.Add(this);

            _commandable = GetComponent<AbstractCommandable>();
            if (_commandable != null)
                _commandable.OnSelectionChanged += HandleSelectionChanged;
        }

        private void Start()
        {
            if (_initialized)
                return;

            BuildingSO so = GetBuildingSO();
            if (so != null)
                Initialize(so.Size);
        }

        private void OnDestroy()
        {
            Live.Remove(this);

            if (_commandable != null)
                _commandable.OnSelectionChanged -= HandleSelectionChanged;
        }

        /// <summary>
        /// 보이는 동안에는 매 프레임 칸에 다시 맞춥니다 — 고스트는 마우스를 따라 움직입니다.
        /// </summary>
        private void LateUpdate()
        {
            if (_renderer != null && _renderer.gameObject.activeSelf)
                AlignToCells();
        }

        /// <summary>
        /// 외부에서 크기를 지정하여 초기화합니다. Ghost 배치 시 사용합니다.
        /// </summary>
        public void Initialize(Vector2Int size)
        {
            if (_initialized)
                return;

            _initialized = true;
            _size = size;
            CreateIndicator(size);
            Refresh();
        }

        private void HandleSelectionChanged(bool selected)
        {
            _selected = selected;
            Refresh();
        }

        private void Refresh()
        {
            if (_renderer == null)
                return;

            // 고스트에는 AbstractCommandable이 없다 — 그쪽은 늘 보인다
            bool visible = _forceVisible || _selected || _commandable == null;
            _renderer.gameObject.SetActive(visible);

            if (visible)
                AlignToCells();
        }

        /// <summary>
        /// 표시를 실제로 먹는 칸들 위에 얹습니다. 점유와 같은 계산을 쓰므로 그림과 실제가 같습니다.
        /// </summary>
        private void AlignToCells()
        {
            Tilemap map = BuildingFootprint.BaseMap;
            if (map == null)
                return;

            BoundsInt cells = BuildingFootprint.CellBounds(map, transform.position, _size);
            Vector3 center = BuildingFootprint.CellBoundsCenter(map, cells);
            center.z = transform.position.z;
            _renderer.transform.position = center;
        }

        private BuildingSO GetBuildingSO()
        {
            if (TryGetComponent(out AbstractBuilding building))
                return building.BuildingSO;
            if (TryGetComponent(out ConstructionDummy dummy))
                return dummy.BuildingSO;
            return null;
        }

        private void CreateIndicator(Vector2Int size)
        {
            Vector2 cellSize = BuildingFootprint.CellSize;

            GameObject indicatorObj = new("FootprintGrid") { layer = gameObject.layer };
            Transform indicatorTrm = indicatorObj.transform;
            indicatorTrm.SetParent(transform, false);
            indicatorTrm.localPosition = Vector3.zero;
            indicatorTrm.localScale = new Vector3(cellSize.x, cellSize.y, 1f);

            _renderer = indicatorObj.AddComponent<SpriteRenderer>();
            _renderer.sprite = GetOrCreateSprite(size);

            SpriteRenderer buildingRenderer = GetComponentInChildren<SpriteRenderer>();
            if (buildingRenderer != null && buildingRenderer != _renderer)
            {
                _renderer.sortingLayerID = buildingRenderer.sortingLayerID;
                _renderer.sortingOrder = buildingRenderer.sortingOrder + 1;
            }
        }

        private static Sprite GetOrCreateSprite(Vector2Int size)
        {
            if (SpriteCache.TryGetValue(size, out Sprite cached) && cached != null)
                return cached;

            Sprite sprite = CreateOutlineSprite(size);
            SpriteCache[size] = sprite;
            return sprite;
        }

        private static Sprite CreateOutlineSprite(Vector2Int size)
        {
            int w = size.x * PixelsPerCell;
            int h = size.y * PixelsPerCell;
            Texture2D tex = new(w, h)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // 칸마다 선을 긋지 않고 영역 외곽만 두른다 — 몇 칸인지가 아니라
                    // 어디까지 차지하는지가 배치할 때 보고 싶은 것이다.
                    bool isBorder = x < BorderThickness || y < BorderThickness
                                    || x >= w - BorderThickness || y >= h - BorderThickness;
                    pixels[y * w + x] = isBorder ? BorderColor : FillColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerCell);
        }
    }
}
