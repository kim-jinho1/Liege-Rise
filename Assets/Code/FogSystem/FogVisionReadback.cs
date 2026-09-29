using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Code.FogSystem
{
    /// <summary>
    /// FoW 카메라가 그린 시야를 GPU에서 누적하고, 판정용 픽셀만 비동기로 내려받습니다.
    /// 탐색 누적은 Blit으로, 픽셀 읽기는 AsyncGPUReadback으로 처리해 CPU-GPU 동기 stall을 없앱니다.
    /// </summary>
    public sealed class FogVisionReadback : IDisposable
    {
        private const int ReadbackResolution = 256;

        private readonly Camera _camera;
        private readonly Material _accumulateMaterial;
        private readonly RenderTexture _exploredRT;
        private readonly RenderTexture _readbackRT;
        private readonly byte[] _visionBytes;

        private bool _textureReady;
        private bool _readbackPending;

        /// <summary>탐색한 적 있는 영역이 누적된 텍스처입니다.</summary>
        public RenderTexture Explored => _exploredRT;

        /// <summary>지금 보이는 영역입니다. 카메라가 그리는 대상 그대로입니다.</summary>
        public RenderTexture Vision => _camera.targetTexture;

        /// <summary>한 번이라도 내려받았는지입니다. 그 전에는 판정할 데이터가 없습니다.</summary>
        public bool IsReady => _textureReady;

        public FogVisionReadback(Camera camera, Material accumulateMaterial)
        {
            _camera = camera;
            _accumulateMaterial = accumulateMaterial;

            // MSAA가 켜진 타깃은 Blit 원본으로 못 쓴다. 같은 서술자로 샘플만 1인 것을 다시 만든다
            RenderTexture srcRT = camera.targetTexture;
            if (srcRT.antiAliasing > 1)
            {
                RenderTextureDescriptor desc = srcRT.descriptor;
                desc.msaaSamples = 1;
                var fowRT = new RenderTexture(desc) { name = srcRT.name };
                fowRT.Create();
                camera.targetTexture = fowRT;
            }

            RenderTexture activeRT = camera.targetTexture;

            _exploredRT = new RenderTexture(activeRT.width, activeRT.height, 0, RenderTextureFormat.R8,
                RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Bilinear,
                name = "ExploredRT"
            };
            _exploredRT.Create();

            _readbackRT = new RenderTexture(ReadbackResolution, ReadbackResolution, 0, RenderTextureFormat.R8,
                RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Bilinear,
                name = "VisibilityReadbackRT"
            };
            _readbackRT.Create();

            _visionBytes = new byte[ReadbackResolution * ReadbackResolution];
        }

        /// <summary>
        /// 이번 프레임의 시야를 누적하고 판정용 읽기를 겁니다. 카메라 렌더가 끝난 뒤에 부릅니다.
        /// </summary>
        public void Capture()
        {
            AccumulateExplored();
            RequestReadback();
        }

        /// <summary>
        /// 월드 좌표가 플레이어 시야 내에 있는지 판정합니다.
        /// </summary>
        public bool IsVisible(Vector3 position)
        {
            Vector3 screenPoint = _camera.WorldToScreenPoint(position);
            RenderTexture fowRT = _camera.targetTexture;

            int x = Mathf.Clamp((int)(screenPoint.x / fowRT.width * ReadbackResolution), 0, ReadbackResolution - 1);
            int y = Mathf.Clamp((int)(screenPoint.y / fowRT.height * ReadbackResolution), 0, ReadbackResolution - 1);

            return _visionBytes[y * ReadbackResolution + x] > 230; // 0.9f threshold (byte: 230/255)
        }

        public void Dispose()
        {
            if (_exploredRT != null)
                _exploredRT.Release();

            if (_readbackRT != null)
                _readbackRT.Release();
        }

        /// <summary>
        /// 탐색 누적을 GPU Blit으로 처리합니다. max(vision, explored)를 GPU에서 계산입니다.
        /// </summary>
        private void AccumulateExplored()
        {
            RenderTexture temp = RenderTexture.GetTemporary(_exploredRT.descriptor);
            Graphics.Blit(_camera.targetTexture, temp, _accumulateMaterial);
            Graphics.Blit(temp, _exploredRT);
            RenderTexture.ReleaseTemporary(temp);
        }

        /// <summary>
        /// 256×256으로 다운샘플 후 AsyncGPUReadback으로 비동기 읽기 요청합니다.
        /// GPU stall 없이 1프레임 딜레이로 CPU 데이터를 갱신합니다.
        /// </summary>
        private void RequestReadback()
        {
            if (_readbackPending)
                return;

            Graphics.Blit(_camera.targetTexture, _readbackRT);
            _readbackPending = true;
            AsyncGPUReadback.Request(_readbackRT, 0, TextureFormat.R8, OnReadbackComplete);
        }

        private void OnReadbackComplete(AsyncGPUReadbackRequest request)
        {
            _readbackPending = false;
            if (request.hasError)
                return;

            // NativeArray는 콜백 범위 내에서만 유효 — 즉시 복사
            request.GetData<byte>().CopyTo(_visionBytes);
            _textureReady = true;
        }
    }
}
