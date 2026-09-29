using Code.Buildings.Base;
using Code.Navigation.Baking;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Code.Navigation.Editor
{
    /// <summary>
    /// 씬에 손으로 놓은 건물·자원을 런타임과 같은 자리로 스냅하고,
    /// 선택한 것의 셀 좌표와 건설 가능 여부를 씬 뷰에 표시합니다.
    /// </summary>
    [InitializeOnLoad]
    public static class GridSnapTool
    {
        private const string MenuPath = "Tools/Map/그리드에 스냅";

        // 메뉴 단축키는 Create Empty Parent(Ctrl+Shift+G)와, Alt+G는 브라우저 전역 핫키와 겹쳤다.
        // 컨텍스트를 걸면 씬 뷰에 포커스가 있을 때만 먹어, 하이어라키에서 고르고 누르면 무반응이다
        private const string ShortcutId = "Map/Grid Snap";

        private static readonly Color BuildableFace = new(0.2f, 0.8f, 1f, 0.12f);
        private static readonly Color BuildableLine = new(0.2f, 0.8f, 1f, 0.9f);
        private static readonly Color BlockedFace = new(1f, 0.25f, 0.2f, 0.18f);
        private static readonly Color BlockedLine = new(1f, 0.25f, 0.2f, 0.95f);

        private static PathBaker _baker;
        private static GUIStyle _labelStyle;

        static GridSnapTool() => SceneView.duringSceneGui += DrawOverlay;

        // 단축키가 안 먹으면 Edit ▸ Shortcuts에서 "Grid Snap"을 찾아 직접 지정하면 된다
        [Shortcut(ShortcutId, KeyCode.G, ShortcutModifiers.Shift | ShortcutModifiers.Alt)]
        private static void SnapShortcut() => Snap();

        [MenuItem(MenuPath, true)]
        private static bool CanSnap() => Selection.transforms.Length > 0;

        [MenuItem(MenuPath)]
        private static void Snap()
        {
            PathBaker baker = ResolveBaker();
            if (baker == null)
            {
                Debug.LogWarning("[GridSnapTool] 씬에 PathBaker가 없어 기준으로 삼을 그리드가 없습니다.");
                return;
            }

            Transform[] targets = Selection.transforms;
            Undo.RecordObjects(targets, "Snap To Grid");

            foreach (Transform target in targets)
                target.position = baker.SnapToGrid(target.position, FootprintOf(target));

            SceneView.RepaintAll();
        }

        private static void DrawOverlay(SceneView view)
        {
            Transform[] targets = Selection.transforms;
            if (targets.Length == 0)
                return;

            PathBaker baker = ResolveBaker();
            if (baker == null || baker.BaseTilemap == null)
                return;

            Vector3 cellSize = baker.BaseTilemap.cellSize;

            foreach (Transform target in targets)
            {
                Vector2Int size = FootprintOf(target);

                // 스냅 전에도 "놓이게 될 자리"를 그린다 — 눈으로 맞춘 위치가 아니라 이쪽이 실제 결과다
                Vector3 snapped = baker.SnapToGrid(target.position, size);
                bool buildable = baker.CanBuildFootprint(snapped, size);

                DrawFootprint(snapped, size, cellSize, buildable);
                DrawLabel(baker, snapped, size, cellSize, buildable, target.position != snapped);
            }
        }

        private static void DrawFootprint(Vector3 center, Vector2Int size, Vector3 cellSize, bool buildable)
        {
            float halfWidth = size.x * cellSize.x * 0.5f;
            float halfHeight = size.y * cellSize.y * 0.5f;

            Vector3[] corners =
            {
                new(center.x - halfWidth, center.y - halfHeight, 0f),
                new(center.x - halfWidth, center.y + halfHeight, 0f),
                new(center.x + halfWidth, center.y + halfHeight, 0f),
                new(center.x + halfWidth, center.y - halfHeight, 0f),
            };

            Handles.DrawSolidRectangleWithOutline(corners,
                buildable ? BuildableFace : BlockedFace,
                buildable ? BuildableLine : BlockedLine);
        }

        private static void DrawLabel(PathBaker baker, Vector3 center, Vector2Int size, Vector3 cellSize,
            bool buildable, bool needsSnap)
        {
            // CanBuildFootprint와 같은 방식으로 좌하단 셀을 구한다. 짝수 크기는 중심이 셀 경계에 있어 그대로 읽으면 어긋난다
            Vector3 origin = center - new Vector3(
                (size.x - 1) * 0.5f * cellSize.x,
                (size.y - 1) * 0.5f * cellSize.y, 0f);
            Vector3Int cell = baker.BaseTilemap.WorldToCell(origin);

            string text = $"{cell.x}, {cell.y}";

            if (size != Vector2Int.one)
                text += $"  {size.x}×{size.y}";

            if (!buildable)
                text += "  · 건설 불가";

            if (needsSnap)
                text += "  · 스냅 필요";

            Vector3 labelPosition = center + new Vector3(0f, size.y * cellSize.y * 0.5f + 0.35f, 0f);
            Handles.Label(labelPosition, text, LabelStyle(buildable));
        }

        private static GUIStyle LabelStyle(bool buildable)
        {
            _labelStyle ??= new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
            _labelStyle.normal.textColor = buildable ? Color.white : new Color(1f, 0.4f, 0.35f);
            return _labelStyle;
        }

        private static Vector2Int FootprintOf(Transform target)
        {
            AbstractBuilding building = target.GetComponent<AbstractBuilding>();
            return building != null && building.BuildingSO != null ? building.BuildingSO.Size : Vector2Int.one;
        }

        private static PathBaker ResolveBaker()
        {
            if (_baker == null)
                _baker = Object.FindAnyObjectByType<PathBaker>();

            return _baker;
        }
    }
}
