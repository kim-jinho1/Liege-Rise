using UnityEngine;

namespace Code.Military.Squads
{
    /// <summary>
    /// 런타임 표시(진격 화살표·공세선)가 나눠 쓰는 머티리얼입니다.
    /// 색은 머티리얼이 아니라 정점이 쥐고 있어 공유해도 섞이지 않습니다 — 표시마다 새로 만들면 죽을 때마다 남습니다.
    /// </summary>
    public static class MarkerMaterial
    {
        private const string ShaderName = "Sprites/Default";
        private static readonly int RendererColorId = Shader.PropertyToID("_RendererColor");
        private static Material _shared;

        public static Material Shared
        {
            get
            {
                if (_shared != null)
                    return _shared;

                _shared = new Material(Shader.Find(ShaderName)) { name = "WorldMarker" };
                _shared.color = Color.white;
                _shared.SetColor(RendererColorId, Color.white);
                return _shared;
            }
        }
    }
}
