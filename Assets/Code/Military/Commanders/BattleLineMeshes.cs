using Code.Military.Data;
using Code.Military.Squads;
using Code.Rendering;
using UnityEngine;

namespace Code.Military.Commanders
{
    /// <summary>
    /// 공세선 표시의 메시와 렌더러 묶음입니다 — 전선 막대와 곡선 화살표, 각각 본체와 테두리 한 겹씩입니다.
    /// 뷰는 "무엇을 그릴지"만 정하고 정점은 전부 여기서 찍습니다.
    /// </summary>
    public sealed class BattleLineMeshes
    {
        private const string BarName = "__BattleLineBar";
        private const string ArrowName = "__BattleLineArrow";
        private const string EdgeSuffix = "Edge";

        private static readonly int[] BarTriangles = { 0, 2, 1, 2, 3, 1 };

        public Transform Bar { get; }
        public Transform Arrow { get; }

        private readonly ArrowStripBuilder _arrowBuilder = new();
        private readonly Vector3[] _barVerts = new Vector3[4];
        private readonly Color[] _barColors = new Color[4];

        private readonly Mesh _barFill;
        private readonly Mesh _barEdge;
        private readonly Mesh _arrowFill;
        private readonly Mesh _arrowEdge;
        private readonly MeshRenderer[] _renderers = new MeshRenderer[4];

        public BattleLineMeshes(Transform parent, SquadMarkerStyleSO style)
        {
            int order = style.LineSortingOrder;

            Bar = NewPivot(parent, BarName);
            _barEdge = NewMesh(BarName + EdgeSuffix);
            _renderers[0] = AddSurface(Bar, BarName + EdgeSuffix, order - 1, _barEdge);
            _barFill = NewMesh(BarName);
            _renderers[1] = AddSurface(Bar, BarName, order, _barFill);

            // 화살표는 막대보다 한 겹 위. 곡선이 막대 위를 지나갈 때 묻히지 않는다
            Arrow = NewPivot(parent, ArrowName);
            _arrowEdge = NewMesh(ArrowName + EdgeSuffix);
            _renderers[2] = AddSurface(Arrow, ArrowName + EdgeSuffix, order + 1, _arrowEdge);
            _arrowFill = NewMesh(ArrowName);
            _renderers[3] = AddSurface(Arrow, ArrowName, order + 2, _arrowFill);
        }

        public void Dispose()
        {
            Object.Destroy(_barFill);
            Object.Destroy(_barEdge);
            Object.Destroy(_arrowFill);
            Object.Destroy(_arrowEdge);
        }

        public void SetBarVisible(bool visible)
        {
            _renderers[0].enabled = visible;
            _renderers[1].enabled = visible;
        }

        public void SetArrowVisible(bool visible)
        {
            _renderers[2].enabled = visible;
            _renderers[3].enabled = visible;
        }

        /// <summary>
        /// 막대는 자기 좌표계에서 X축을 따라 뻗습니다. 방향은 피벗 회전이 맡습니다.
        /// </summary>
        public void WriteBar(float length, float width, float outline, Color fill, Color edge)
        {
            WriteQuad(_barEdge, length * 0.5f + outline, width * 0.5f + outline, edge);
            WriteQuad(_barFill, length * 0.5f, width * 0.5f, fill);
        }

        /// <summary>
        /// 곡선 화살표입니다. 표본점은 피벗 기준 로컬 좌표입니다.
        /// </summary>
        public void WriteArrow(Vector2[] curve, int count, in ArrowDimensions dim, float outline, Color fill, Color edge)
        {
            _arrowBuilder.Write(_arrowEdge, curve, count, dim, outline, edge);
            _arrowBuilder.Write(_arrowFill, curve, count, dim, 0f, fill);
        }

        private void WriteQuad(Mesh mesh, float halfLength, float halfWidth, Color color)
        {
            _barVerts[0] = new Vector3(-halfLength, -halfWidth, 0f);
            _barVerts[1] = new Vector3(halfLength, -halfWidth, 0f);
            _barVerts[2] = new Vector3(-halfLength, halfWidth, 0f);
            _barVerts[3] = new Vector3(halfLength, halfWidth, 0f);

            for (int i = 0; i < 4; i++)
                _barColors[i] = color;

            mesh.Clear(false);
            mesh.SetVertices(_barVerts);
            mesh.SetColors(_barColors);
            mesh.SetTriangles(BarTriangles, 0, true);
        }

        private static Transform NewPivot(Transform parent, string pivotName)
        {
            GameObject go = new(pivotName);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Mesh NewMesh(string meshName)
            => new() { name = meshName, hideFlags = HideFlags.DontSave };

        private static MeshRenderer AddSurface(Transform parent, string childName, int order, Mesh mesh)
        {
            GameObject child = new(childName);
            child.transform.SetParent(parent, false);
            RenderLayers.PlaceOnMarkerLayer(child);

            child.AddComponent<MeshFilter>().sharedMesh = mesh;

            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = MarkerMaterial.Shared;
            renderer.sortingLayerName = RenderLayers.EntitySorting;
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }
    }
}
