using Code.Buildings.Interfaces;
using Code.Commandable;
using Code.Core.Bus;
using Code.FogSystem;
using UnityEngine;

namespace Code.Effects
{
    /// <summary>
    /// 보충 거점의 사각 범위를 월드에 그립니다. 평소에는 옅게, 선택하면 진하게 보입니다.
    /// </summary>
    public class ReinforceRangeView : MonoBehaviour
    {
        private const string ShaderName = "Sprites/Default";
        private const string LineName = "__ReinforceRange";
        private const float LineWidth = 0.08f;
        private const float IdleAlpha = 0.18f;
        private const float SelectedAlpha = 0.7f;

        private static readonly Color PlayerColor = new(0.35f, 0.66f, 1f);
        private static readonly Color EnemyColor = new(1f, 0.4f, 0.34f);

        private AbstractCommandable _building;
        private IReinforcePoint _point;
        private LineRenderer _line;
        private float _shownSize = -1f;

        /// <summary>
        /// 건물이 보충 거점이면 범위 표시를 붙입니다. 아니면 아무것도 하지 않습니다.
        /// </summary>
        public static void Attach(AbstractCommandable building)
        {
            if (building is not IReinforcePoint point || point.ReinforceSize <= 0f)
                return;

            ReinforceRangeView view = building.gameObject.AddComponent<ReinforceRangeView>();
            view._building = building;
            view._point = point;
        }

        private void Start()
        {
            BuildLine();
            _building.OnSelectionChanged += HandleSelectionChanged;
            _building.OnVisibilityChanged += HandleVisibilityChanged;

            // 스프라이트만 끄면 사각형이 남아 정찰하지 않은 적 거점 위치를 흘린다
            _line.enabled = _building.IsVisible;
            ApplyColor(IdleAlpha);
        }

        private void OnDestroy()
        {
            if (_building == null)
                return;

            _building.OnSelectionChanged -= HandleSelectionChanged;
            _building.OnVisibilityChanged -= HandleVisibilityChanged;
        }

        // 건설 완료나 데이터 교체로 범위가 바뀔 수 있어 크기가 달라졌을 때만 다시 그린다
        private void LateUpdate()
        {
            if (!Mathf.Approximately(_shownSize, _point.ReinforceSize))
                Reshape();
        }

        private void HandleSelectionChanged(bool selected)
            => ApplyColor(selected ? SelectedAlpha : IdleAlpha);

        // FogVisibilityManager는 플레이어 소유가 아닌 것만 등록한다 — 아군 거점은 계속 true다
        private void HandleVisibilityChanged(IHideable hideable, bool isVisible)
            => _line.enabled = isVisible;

        private void BuildLine()
        {
            GameObject go = new(LineName);
            go.transform.SetParent(transform, false);

            _line = go.AddComponent<LineRenderer>();
            _line.useWorldSpace = false;
            _line.loop = true;
            _line.positionCount = 4;
            _line.widthMultiplier = LineWidth;
            _line.numCapVertices = 0;
            _line.textureMode = LineTextureMode.Tile;
            _line.material = new Material(Shader.Find(ShaderName));

            ApplySorting();
            Reshape();
        }

        // 레이어를 안 정하면 Default로 잡혀 지형 타일맵(Water·Ground·Deco) 밑에 묻힌다.
        // BuildingFootprintIndicator·SelectionOutlineEffect와 같이 건물 스프라이트에서 물려받는다
        private void ApplySorting()
        {
            SpriteRenderer body = _building.GetComponentInChildren<SpriteRenderer>();
            if (body == null)
                return;

            _line.sortingLayerID = body.sortingLayerID;
            _line.sortingOrder = body.sortingOrder - 1; // 건물 밑에 깐다
        }

        private void Reshape()
        {
            _shownSize = _point.ReinforceSize;
            float half = _shownSize * 0.5f;

            _line.SetPosition(0, new Vector3(-half, -half, 0f));
            _line.SetPosition(1, new Vector3(half, -half, 0f));
            _line.SetPosition(2, new Vector3(half, half, 0f));
            _line.SetPosition(3, new Vector3(-half, half, 0f));
        }

        private void ApplyColor(float alpha)
        {
            Color color = _building.Owner == Owner.Player ? PlayerColor : EnemyColor;
            color.a = alpha;

            _line.startColor = color;
            _line.endColor = color;
        }
    }
}
