using UnityEngine;

namespace Code.Military.Squads
{
    /// <summary>
    /// 진격 화살표 한 겹의 크기입니다. 본체와 테두리가 같은 값에 부풀림만 달리해 쓰입니다.
    /// </summary>
    public readonly struct ArrowDimensions
    {
        public readonly float Tail;     // 꼬리 폭
        public readonly float Neck;     // 자루 끝(촉 바로 뒤) 폭
        public readonly float Head;     // 촉의 폭
        public readonly float NeckY;    // 촉이 시작하는 거리
        public readonly float FadeY;    // 꼬리 투명도가 다 차오르는 거리
        public readonly float Length;   // 전체 길이

        public ArrowDimensions(float tail, float neck, float head, float neckY, float fadeY, float length)
        {
            Tail = tail;
            Neck = neck;
            Head = head;
            NeckY = neckY;
            FadeY = fadeY;
            Length = length;
        }
    }

    /// <summary>
    /// 진격 화살표의 정점을 찍어 메시에 씁니다. 씬에 의존하지 않는 순수 계산입니다.
    /// 화살표는 자기 좌표계에서 +Y로 뻗습니다 — 방향은 부르는 쪽이 회전으로 준다는 전제입니다.
    /// </summary>
    public sealed class ArrowMeshBuilder
    {
        //  0,1 꼬리 · 2,3 페이드 끝 · 4,5 자루 끝 · 6,7 촉 밑동 · 8 끝점
        private static readonly int[] Triangles =
        {
            0, 2, 1, 2, 3, 1,
            2, 4, 3, 4, 5, 3,
            6, 8, 7
        };

        private const int VertexCount = 9;

        private readonly Vector3[] _verts = new Vector3[VertexCount];
        private readonly Color[] _colors = new Color[VertexCount];

        /// <summary>
        /// 화살표 한 겹을 메시에 씁니다.
        /// grow만큼 옆으로 부풀리면 테두리가 되고, 0이면 본체입니다.
        /// clearCount는 앞에서부터 투명하게 둘 정점 수입니다.
        /// </summary>
        public void Write(Mesh mesh, in ArrowDimensions dim, float grow, Color color, int clearCount)
        {
            FillVertices(dim, grow);
            FillColors(color, clearCount);

            mesh.SetVertices(_verts);
            mesh.SetColors(_colors);
            mesh.SetTriangles(Triangles, 0, true);
        }

        /// <summary>
        /// 꼬리는 뒤로 늘리지 않습니다 — 늘리면 시작 부분에 테두리 막대가 생깁니다.
        /// </summary>
        private void FillVertices(in ArrowDimensions dim, float grow)
        {
            float tailHalf = dim.Tail * 0.5f + grow;
            float neckHalf = dim.Neck * 0.5f + grow;
            float headHalf = dim.Head * 0.5f + grow;
            float fadeHalf = Mathf.Lerp(tailHalf, neckHalf,
                dim.NeckY <= 0f ? 1f : dim.FadeY / dim.NeckY);

            _verts[0] = new Vector3(-tailHalf, 0f, 0f);
            _verts[1] = new Vector3(tailHalf, 0f, 0f);
            _verts[2] = new Vector3(-fadeHalf, dim.FadeY, 0f);
            _verts[3] = new Vector3(fadeHalf, dim.FadeY, 0f);
            _verts[4] = new Vector3(-neckHalf, dim.NeckY, 0f);
            _verts[5] = new Vector3(neckHalf, dim.NeckY, 0f);
            _verts[6] = new Vector3(-headHalf, dim.NeckY, 0f);
            _verts[7] = new Vector3(headHalf, dim.NeckY, 0f);
            _verts[8] = new Vector3(0f, dim.Length + grow, 0f);
        }

        /// <summary>
        /// 앞쪽 clearCount개 정점을 투명하게 둡니다. 시작이 잘린 면으로 끝나면
        /// 분대 밑에서 툭 튀어나온 것처럼 보입니다.
        /// </summary>
        private void FillColors(Color color, int clearCount)
        {
            Color clear = color;
            clear.a = 0f;

            for (int i = 0; i < _colors.Length; i++)
                _colors[i] = i < clearCount ? clear : color;
        }
    }
}
