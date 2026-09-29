using UnityEngine;

namespace Code.Military.Squads
{
    /// <summary>
    /// 곡선을 따라가는 화살표의 정점을 찍습니다. 직선 화살표(ArrowMeshBuilder)와 같은 문법입니다 —
    /// 꼬리가 두껍고 자루로 갈수록 좁아지다 촉이 벌어졌다 뾰족하게 모이고, 꼬리는 투명하게 빠집니다.
    /// 입력은 꼬리에서 끝점까지의 표본점이고, 촉은 마지막 headLength 구간을 차지합니다.
    /// </summary>
    public sealed class ArrowStripBuilder
    {
        public const int MaxSamples = 32;

        private readonly Vector3[] _verts = new Vector3[MaxSamples * 2 + 5];
        private readonly Color[] _colors = new Color[MaxSamples * 2 + 5];
        private readonly int[] _tris = new int[MaxSamples * 6 + 9];
        private readonly float[] _cumulative = new float[MaxSamples];

        /// <summary>
        /// 화살표 한 겹을 메시에 씁니다. grow만큼 옆으로 부풀리면 테두리가 되고, 0이면 본체입니다.
        /// 테두리는 페이드 구간을 아예 그리지 않습니다 — 반투명한 본체 밑에 깔리면 꼬리가 탁해집니다.
        /// </summary>
        public void Write(Mesh mesh, Vector2[] curve, int count, in ArrowDimensions dim, float grow, Color color)
        {
            count = Mathf.Clamp(count, 2, Mathf.Min(MaxSamples, curve.Length));

            float total = 0f;
            _cumulative[0] = 0f;
            for (int i = 1; i < count; i++)
            {
                total += Vector2.Distance(curve[i - 1], curve[i]);
                _cumulative[i] = total;
            }

            float headLength = Mathf.Clamp(dim.Length - dim.NeckY, 0f, total * 0.6f);
            float neckAt = total - headLength;

            int v = 0;
            int lastSample = 0;

            for (int i = 0; i < count && _cumulative[i] <= neckAt; i++)
            {
                float d = _cumulative[i];
                float width = Mathf.Lerp(dim.Tail, dim.Neck, neckAt <= 0f ? 1f : d / neckAt) * 0.5f + grow;
                Vector2 dir = DirectionAt(curve, count, i);
                Vector2 side = new(-dir.y, dir.x);

                _verts[v] = curve[i] - side * width;
                _verts[v + 1] = curve[i] + side * width;
                Fill(v, d, dim.FadeY, grow > 0f, color);
                v += 2;
                lastSample = i;
            }

            // 자루 끝은 표본 사이에 떨어진다 — 보간해 정확히 그 자리에 세운다
            int next = Mathf.Min(lastSample + 1, count - 1);
            float span = _cumulative[next] - _cumulative[lastSample];
            float t = span <= 0.0001f ? 0f : (neckAt - _cumulative[lastSample]) / span;
            Vector2 neckPoint = Vector2.Lerp(curve[lastSample], curve[next], t);
            Vector2 neckDir = (curve[next] - curve[lastSample]).normalized;
            if (neckDir.sqrMagnitude < 0.5f)
                neckDir = DirectionAt(curve, count, lastSample);
            Vector2 neckSide = new(-neckDir.y, neckDir.x);

            float neckHalf = dim.Neck * 0.5f + grow;
            _verts[v] = neckPoint - neckSide * neckHalf;
            _verts[v + 1] = neckPoint + neckSide * neckHalf;
            Fill(v, neckAt, dim.FadeY, grow > 0f, color);
            v += 2;

            float headHalf = dim.Head * 0.5f + grow;
            int headLeft = v;
            _verts[v] = neckPoint - neckSide * headHalf;
            _verts[v + 1] = neckPoint + neckSide * headHalf;
            Fill(v, neckAt, dim.FadeY, grow > 0f, color);
            v += 2;

            Vector2 tipDir = (curve[count - 1] - neckPoint).normalized;
            if (tipDir.sqrMagnitude < 0.5f)
                tipDir = neckDir;

            int tip = v;
            _verts[v] = curve[count - 1] + tipDir * grow;
            Fill(v, total, dim.FadeY, grow > 0f, color);
            v += 1;

            int tri = 0;
            int stripPairs = (headLeft / 2) - 1; // 자루 구간의 정점 쌍 사이 개수
            for (int p = 0; p < stripPairs; p++)
            {
                int a = p * 2;
                _tris[tri++] = a;     _tris[tri++] = a + 2; _tris[tri++] = a + 1;
                _tris[tri++] = a + 2; _tris[tri++] = a + 3; _tris[tri++] = a + 1;
            }

            _tris[tri++] = headLeft;
            _tris[tri++] = tip;
            _tris[tri++] = headLeft + 1;

            mesh.Clear(false);
            mesh.SetVertices(_verts, 0, v);
            mesh.SetColors(_colors, 0, v);
            mesh.SetTriangles(_tris, 0, tri, 0);
        }

        private void Fill(int index, float distance, float fadeLength, bool isEdge, Color color)
        {
            Color c = color;
            if (fadeLength > 0f && distance < fadeLength)
                c.a = isEdge ? 0f : color.a * Mathf.Clamp01(distance / fadeLength);

            _colors[index] = c;
            _colors[index + 1] = c;
        }

        private static Vector2 DirectionAt(Vector2[] curve, int count, int i)
        {
            int prev = Mathf.Max(0, i - 1);
            int next = Mathf.Min(count - 1, i + 1);
            Vector2 dir = curve[next] - curve[prev];
            return dir.sqrMagnitude < 0.0001f ? Vector2.up : dir.normalized;
        }
    }
}
