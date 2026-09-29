using System;
using UnityEngine;

namespace Code.Units.Combat
{
    /// <summary>
    /// 피격 시 본체 위에 흰 실루엣을 덮어 번쩍이게 합니다.
    /// SpriteRenderer.color는 곱연산이라 밝게 만들 수 없어 실루엣을 겹치는 방식을 씁니다.
    /// </summary>
    public class HitFlashEffect : MonoBehaviour
    {
        private const string ShaderName = "Custom/SelectionOutlineShader";
        private const string MaterialResourcePath = "SelectionOutline";
        private const string OverlayName = "__HitFlash";
        private const float FlashDuration = 0.1f;

        private static readonly Color FlashColor = Color.white;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        // 실루엣은 색만 다를 뿐 동일하므로 모든 복제본이 한 머티리얼을 공유합니다.
        private static Material _sharedMaterial;

        private SpriteRenderer[] _renderers = Array.Empty<SpriteRenderer>();
        private SpriteRenderer[] _overlays = Array.Empty<SpriteRenderer>();
        private MaterialPropertyBlock _propertyBlock;
        private float _endTime;
        private bool _isRunning;

        /// <summary>
        /// 피격 플래시를 재생합니다. 이미 재생 중이면 종료 시각만 연장됩니다.
        /// </summary>
        public void Flash(SpriteRenderer[] renderers)
        {
            _endTime = Time.time + FlashDuration;
            if (_isRunning)
                return;

            _renderers = renderers ?? Array.Empty<SpriteRenderer>();
            BuildOverlays();
            RunAsync();
        }

        private async void RunAsync()
        {
            _isRunning = true;
            try
            {
                while (Time.time < _endTime)
                {
                    SyncOverlays();
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
            }
            catch (OperationCanceledException)
            {

            }
            finally
            {
                DisableOverlays();
                _isRunning = false;
            }
        }

        private void BuildOverlays()
        {
            if (_overlays.Length == _renderers.Length)
                return;

            EnsureSharedMaterial();
            _propertyBlock ??= new MaterialPropertyBlock();
            _overlays = new SpriteRenderer[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
            {
                SpriteRenderer body = _renderers[i];
                if (body == null)
                    continue;

                GameObject overlayObject = new GameObject(OverlayName);
                overlayObject.transform.SetParent(body.transform, false);

                SpriteRenderer overlay = overlayObject.AddComponent<SpriteRenderer>();
                overlay.sharedMaterial = _sharedMaterial;
                overlay.enabled = false;
                _overlays[i] = overlay;
            }
        }

        /// <summary>
        /// 실루엣 머티리얼을 1회 확보합니다. Resources에 두어 빌드 시 셰이더 스트리핑을 방지합니다.
        /// </summary>
        private void EnsureSharedMaterial()
        {
            if (_sharedMaterial != null)
                return;

            _sharedMaterial = Resources.Load<Material>(MaterialResourcePath);
            if (_sharedMaterial == null)
                _sharedMaterial = new Material(Shader.Find(ShaderName)) { name = "HitFlash (Fallback)" };
        }

        private void SyncOverlays()
        {
            for (int i = 0; i < _overlays.Length; i++)
            {
                SpriteRenderer overlay = _overlays[i];
                SpriteRenderer body = _renderers[i];
                if (overlay == null || body == null)
                    continue;

                Sprite sprite = body.sprite;
                bool visible = body.enabled && body.gameObject.activeInHierarchy && sprite != null;

                overlay.enabled = visible;
                if (!visible)
                    continue;

                overlay.sprite = sprite;
                overlay.flipX = body.flipX;
                overlay.flipY = body.flipY;

                // 본체를 덮어야 하므로 한 칸 위에 그린다.
                overlay.sortingLayerID = body.sortingLayerID;
                overlay.sortingOrder = body.sortingOrder + 1;
                overlay.gameObject.layer = body.gameObject.layer;

                overlay.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(ColorId, FlashColor);
                _propertyBlock.SetTexture(MainTexId, sprite.texture);
                overlay.SetPropertyBlock(_propertyBlock);
            }
        }

        private void DisableOverlays()
        {
            foreach (SpriteRenderer overlay in _overlays)
                if (overlay != null)
                    overlay.enabled = false;
        }
    }
}
