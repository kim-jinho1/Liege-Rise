using System.Collections.Generic;
using Code.Core.Bus;
using Code.Military.Combat;
using Code.Military.Data;
using Code.Rendering;
using UnityEngine;

namespace Code.Military.Squads
{
    /// <summary>
    /// 분대 발밑에 정면·측면·후면 구간을 부채꼴로 깝니다.
    /// 각도는 SquadSO의 값을 그대로 그리므로 화면과 피해 판정이 갈라지지 않습니다.
    /// 맞은 구간은 잠깐 밝아져, 글자 없이 어디를 내줬는지 알려 줍니다.
    /// </summary>
    public class SquadArcView : MonoBehaviour
    {
        private const string PivotName = "__SquadArc";
        private const string SurfaceName = "__SquadArcFill";
        private const string ShaderName = "Sprites/Default";
        private const float DegreesPerSlice = 6f;

        private static Material _sharedMaterial;

        private Squad _squad;
        private SquadSO _squadSO;
        private SquadFacing _facing;
        private SquadMarkerStyleSO _style;

        private Transform _pivot;
        private Mesh _mesh;
        private MeshRenderer _renderer;

        // 정점마다 어느 구간의 어느 층인지. 색만 갈아 끼우면 되도록 기하와 따로 들고 있는다
        private ArmorZone[] _vertexZone;
        private bool[] _vertexIsEdge;
        private Color[] _colors;

        private ArmorZone _flashZone;
        private float _flashUntil = float.NegativeInfinity;

        /// <summary>
        /// 분대에 방향 피해 부채꼴을 붙입니다.
        /// </summary>
        public static void Attach(Squad squad, SquadSO squadSO, SquadFacing facing)
        {
            if (squad == null || squadSO == null || facing == null)
                return;

            SquadArcView view = squad.gameObject.AddComponent<SquadArcView>();
            view._squad = squad;
            view._squadSO = squadSO;
            view._facing = facing;
            view._style = SquadMarkerStyleSO.Load();
            view.Build();
        }

        private void OnDestroy()
        {
            if (_squad != null)
                _squad.ZoneHit -= HandleZoneHit;

            if (_mesh != null)
                Destroy(_mesh);
        }

        private void LateUpdate()
        {
            bool visible = ShouldShow();
            _renderer.enabled = visible;
            if (!visible)
                return;

            _pivot.localRotation = Quaternion.Euler(0f, 0f, _facing.CurrentAngle);

            // 번쩍임이 끝나는 프레임에만 색을 되돌린다
            if (_flashUntil > float.NegativeInfinity && Time.time >= _flashUntil)
            {
                _flashUntil = float.NegativeInfinity;
                ApplyColors();
            }
        }

        /// <summary>
        /// 적 분대는 시야에 든 동안 늘 보여 줍니다 — 돌아 들어갈 이유가 화면에 없으면 기믹이 없는 것과 같습니다.
        /// 아군도 같은 이유로 늘 보여 주되, 화면이 덮인다 싶으면 스타일에서 끄고 선택 시에만 띄웁니다.
        /// </summary>
        // 적도 선택했을 때만 — 늘 띄우면 교전 중 화면이 부채꼴로 덮인다. 방향은 이제 공세선 화살표가 말한다
        private bool ShouldShow()
            => _squad.Owner == Owner.Player
                ? _style.ArcAlwaysOnAllies || _squad.IsSelected
                : _squad.IsSelected && SquadVisibility.IsVisibleToPlayer(_squad);

        private void HandleZoneHit(ArmorZone zone)
        {
            _flashZone = zone;
            _flashUntil = Time.time + Mathf.Max(0f, _style.ArcFlashSeconds);
            ApplyColors();
        }

        private void Build()
        {
            GameObject pivot = new(PivotName);
            pivot.transform.SetParent(transform, false);
            RenderLayers.PlaceOnMarkerLayer(pivot, gameObject);
            _pivot = pivot.transform;

            _mesh = new Mesh { name = SurfaceName, hideFlags = HideFlags.DontSave };
            BuildMesh();

            GameObject surface = new(SurfaceName);
            surface.transform.SetParent(pivot.transform, false);
            RenderLayers.PlaceOnMarkerLayer(surface, gameObject);
            surface.AddComponent<MeshFilter>().sharedMesh = _mesh;

            _renderer = surface.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = SharedMaterial;
            _renderer.sortingLayerName = RenderLayers.EntitySorting;
            _renderer.sortingOrder = _style.ArcSortingOrder;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.enabled = false;

            _squad.ZoneHit += HandleZoneHit;
        }

        /// <summary>
        /// 네 조각(정면 · 측면 둘 · 후면)을 한 메시에 담습니다. 색은 정점이 쥐므로
        /// 조각마다 렌더러를 두지 않아도 되고, 번쩍임은 색만 갈아 끼우면 끝납니다.
        /// </summary>
        private void BuildMesh()
        {
            float half = Mathf.Clamp(_squadSO.FrontConeAngle, 0f, 360f) * 0.5f;
            float rearHalf = Mathf.Clamp(_squadSO.RearConeAngle, 0f, 360f) * 0.5f;

            List<Vector3> vertices = new(256);
            List<int> triangles = new(768);
            List<ArmorZone> zones = new(256);
            List<bool> edges = new(256);

            AddSpan(-half, half, ArmorZone.Front, vertices, triangles, zones, edges);
            AddSpan(half, 180f - rearHalf, ArmorZone.Side, vertices, triangles, zones, edges);
            AddSpan(180f - rearHalf, 180f + rearHalf, ArmorZone.Rear, vertices, triangles, zones, edges);
            AddSpan(180f + rearHalf, 360f - half, ArmorZone.Side, vertices, triangles, zones, edges);

            _vertexZone = zones.ToArray();
            _vertexIsEdge = edges.ToArray();
            _colors = new Color[_vertexZone.Length];

            _mesh.Clear();
            _mesh.SetVertices(vertices);
            _mesh.SetTriangles(triangles, 0);
            ApplyColors();
            _mesh.RecalculateBounds();
        }

        /// <summary>
        /// 한 구간을 채움 부채꼴과 바깥 테두리 띠로 나눠 담습니다.
        /// </summary>
        private void AddSpan(float fromDeg, float toDeg, ArmorZone zone, List<Vector3> vertices,
            List<int> triangles, List<ArmorZone> zones, List<bool> edges)
        {
            float sweep = toDeg - fromDeg;
            if (sweep <= 0.5f)
                return;

            int slices = Mathf.Max(1, Mathf.CeilToInt(sweep / DegreesPerSlice));
            float radius = Mathf.Max(0.01f, _style.ArcRadius);
            float inner = Mathf.Max(0f, radius - Mathf.Max(0.01f, _style.ArcEdgeWidth));

            int center = vertices.Count;
            vertices.Add(Vector3.zero);
            zones.Add(zone);
            edges.Add(false);

            for (int i = 0; i <= slices; i++)
            {
                float deg = Mathf.Lerp(fromDeg, toDeg, i / (float)slices);
                Vector3 dir = Direction(deg);

                vertices.Add(dir * inner);
                zones.Add(zone);
                edges.Add(false);

                vertices.Add(dir * radius);
                zones.Add(zone);
                edges.Add(true);
            }

            for (int i = 0; i < slices; i++)
            {
                int a = center + 1 + i * 2;      // 안쪽 현재
                int b = a + 2;                   // 안쪽 다음
                int aEdge = a + 1;               // 바깥 현재
                int bEdge = b + 1;               // 바깥 다음

                triangles.Add(center); triangles.Add(b); triangles.Add(a);
                triangles.Add(a); triangles.Add(b); triangles.Add(bEdge);
                triangles.Add(a); triangles.Add(bEdge); triangles.Add(aEdge);
            }
        }

        /// <summary>
        /// 분대 정면(+Y)을 0°로 두고 시계 반대 방향으로 도는 로컬 방향입니다.
        /// </summary>
        private static Vector3 Direction(float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector3(-Mathf.Sin(rad), Mathf.Cos(rad), 0f);
        }

        private void ApplyColors()
        {
            bool flashing = Time.time < _flashUntil;

            for (int i = 0; i < _colors.Length; i++)
            {
                ArmorZone zone = _vertexZone[i];
                Color color = ColorOf(zone);

                float alpha = _vertexIsEdge[i] ? _style.ArcEdgeAlpha : _style.ArcFillAlpha;
                if (zone != ArmorZone.Front)
                    alpha *= _style.ArcMutedScale;

                if (flashing && zone == _flashZone)
                    alpha = _style.ArcFlashAlpha;

                color.a = alpha;
                _colors[i] = color;
            }

            _mesh.SetColors(_colors);
        }

        private Color ColorOf(ArmorZone zone)
            => zone switch
            {
                ArmorZone.Rear => _style.ArcRearColor,
                ArmorZone.Side => _style.ArcSideColor,
                _ => _style.ArcFrontColor
            };

        /// <summary>
        /// 모든 분대가 한 머티리얼을 나눠 씁니다. 색은 정점이 쥐고 있어 공유해도 섞이지 않습니다 —
        /// 분대마다 새로 만들면 죽을 때마다 머티리얼이 남습니다.
        /// </summary>
        private static Material SharedMaterial
        {
            get
            {
                if (_sharedMaterial != null)
                    return _sharedMaterial;

                _sharedMaterial = new Material(Shader.Find(ShaderName)) { name = "SquadArc" };
                return _sharedMaterial;
            }
        }
    }
}
