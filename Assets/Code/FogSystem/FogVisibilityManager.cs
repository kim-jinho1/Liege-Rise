using System.Collections.Generic;
using Code.Core;
using Code.Core.Bus;
using Code.GameEvents.Building;
using Code.GameEvents.Enemy;
using Code.GameEvents.System;
using Code.GameEvents.Unit;
using UnityEngine;
using UnityEngine.Rendering;

namespace Code.FogSystem
{
    /// <summary>
    /// Fog of War 가시성 매니저입니다.
    /// 시야 판정 대상을 모아 두고, FogVisionReadback이 내려받은 시야로 매 프레임 조금씩 갱신합니다.
    /// 안개 머티리얼에 카메라 바운드와 미니맵 UV 변환을 넘기는 것도 여기서 합니다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FogVisibilityManager : MonoSingleton<FogVisibilityManager>
    {
        /// <summary>
        /// FogOverlay 머티리얼입니다. Inspector에서 할당합니다.
        /// </summary>
        [SerializeField] private Material fogOverlayMaterial;

        /// <summary>
        /// 미니맵 RawImage에 적용할 MinimapFog 머티리얼입니다. Inspector에서 할당합니다.
        /// </summary>
        [SerializeField] private Material minimapFogMaterial;

        /// <summary>
        /// 미니맵 카메라입니다. fog UV 변환 계산에 사용됩니다.
        /// </summary>
        [SerializeField] private Camera minimapCamera;

        /// <summary>
        /// 탐색 누적 셰이더 머티리얼입니다.
        /// </summary>
        [SerializeField] private Material accumulateMaterial;

        private static readonly int FogCameraBoundsID = Shader.PropertyToID("_FogCameraBounds");
        private static readonly int ExploredTexID = Shader.PropertyToID("_ExploredTex");
        private static readonly int VisionTexID = Shader.PropertyToID("_VisionTex");
        private static readonly int FogUVTransformID = Shader.PropertyToID("_FogUVTransform");

        private Camera _fogOfWarCamera;
        private FogVisionReadback _vision;

        private readonly HashSet<IHideable> _hideableSet = new(1000);
        private readonly List<IHideable> _iterBuffer = new(1000);
        private const int VisibilitySlices = 4; // 한 바퀴 도는 데 걸리는 프레임 수
        private bool _iterDirty = true;         // 등록/해제가 있었으면 다음 프레임에 명부를 다시 뜬다
        private int _cursor;

        protected override void Awake()
        {
            base.Awake();
            _fogOfWarCamera = GetComponent<Camera>();
            _vision = new FogVisionReadback(_fogOfWarCamera, accumulateMaterial);

            RenderTexture activeRT = _vision.Vision;
            if (accumulateMaterial != null)
                accumulateMaterial.SetTexture(ExploredTexID, _vision.Explored);

            if (fogOverlayMaterial != null)
            {
                fogOverlayMaterial.SetTexture(VisionTexID, activeRT);
                fogOverlayMaterial.SetTexture(ExploredTexID, _vision.Explored);
            }

            if (minimapFogMaterial != null)
            {
                minimapFogMaterial.SetTexture(VisionTexID, activeRT);
                minimapFogMaterial.SetTexture(ExploredTexID, _vision.Explored);
            }

            UpdateFogCameraBounds();

            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
            Bus<UnitSpawnEvent>.RegisterForAll(HandleUnitSpawn);
            Bus<UnitDeathEvent>.RegisterForAll(HandleUnitDeath);
            Bus<EnemyDeathEvent>.RegisterForAll(HandleEnemyDeath);
            Bus<BuildingSpawnEvent>.RegisterForAll(HandleBuildingSpawn);
            Bus<BuildingDeathEvent>.RegisterForAll(HandleBuildingDeath);
            Bus<PlaceHolderSpawnEvent>.RegisterForAll(HandlePlaceHolderSpawn);
            Bus<PlaceHolderDestroyEvent>.RegisterForAll(HandlePlaceHolderDestroy);
        }

        protected override void OnDestroy()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            Bus<UnitSpawnEvent>.UnRegisterForAll(HandleUnitSpawn);
            Bus<UnitDeathEvent>.UnRegisterForAll(HandleUnitDeath);
            Bus<EnemyDeathEvent>.UnRegisterForAll(HandleEnemyDeath);
            Bus<BuildingSpawnEvent>.UnRegisterForAll(HandleBuildingSpawn);
            Bus<BuildingDeathEvent>.UnRegisterForAll(HandleBuildingDeath);
            Bus<PlaceHolderSpawnEvent>.UnRegisterForAll(HandlePlaceHolderSpawn);
            Bus<PlaceHolderDestroyEvent>.UnRegisterForAll(HandlePlaceHolderDestroy);

            _vision?.Dispose();
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (cam != _fogOfWarCamera)
                return;

            _vision.Capture();
            UpdateFogCameraBounds();
        }

        /// <summary>
        /// 시야 판정을 네 프레임에 나눠 돕니다.
        /// 대상의 대부분은 움직이지 않는 자원 노드(Stage2 기준 276개)라 매 프레임 전부 볼 이유가 없습니다.
        /// 지연은 최대 66ms이고 SetVisible이 같은 값이면 즉시 빠지므로 깜빡이지 않습니다.
        /// </summary>
        private void LateUpdate()
        {
            if (!_vision.IsReady)
                return;

            if (_iterDirty)
            {
                _iterBuffer.Clear();
                _iterBuffer.AddRange(_hideableSet);
                _iterDirty = false;
                // 커서를 0으로 되돌리지 않는다 — 전투 중에는 등록/해제가 거의 매 프레임 일어나
                // 되돌리면 첫 조각만 계속 돌고 나머지 3/4는 시야 판정이 영영 갱신되지 않는다.
                // 아래 % count가 명부가 줄어든 경우도 받아 준다
            }

            int count = _iterBuffer.Count;
            if (count == 0)
                return;

            int take = Mathf.CeilToInt(count / (float)VisibilitySlices);
            for (int n = 0; n < take; n++)
            {
                IHideable hideable = _iterBuffer[(_cursor + n) % count];
                if (hideable?.Transform == null)
                    continue;

                hideable.SetVisible(_vision.IsVisible(hideable.Transform.position));
            }

            _cursor = (_cursor + take) % count;
        }

        /// <summary>
        /// 시야 판정 대상으로 등록합니다. 이벤트를 발행하지 않는 오브젝트(잔해·자원)가 쓰는 문입니다.
        /// </summary>
        public void Register(IHideable hideable)
        {
            if (hideable == null)
                return;

            _hideableSet.Add(hideable);
            _iterDirty = true;
        }

        /// <summary>
        /// 시야 판정 대상에서 제외합니다.
        /// </summary>
        public void Unregister(IHideable hideable)
        {
            if (hideable == null)
                return;

            _hideableSet.Remove(hideable);
            _iterDirty = true;
        }

        /// <summary>
        /// 플레이어 것이 아닐 때만 판정 대상에 올립니다. 아군은 늘 보이므로 셀 필요가 없습니다.
        /// </summary>
        private void RegisterHostile(Component candidate, Owner owner)
        {
            if (owner != Owner.Player && candidate is IHideable hideable)
                Register(hideable);
        }

        private void HandleUnitSpawn(UnitSpawnEvent evt)
            => RegisterHostile(evt.Unit, evt.Owner);

        private void HandleUnitDeath(UnitDeathEvent evt)
            => Unregister(evt.Unit);

        private void HandleEnemyDeath(EnemyDeathEvent evt)
            => Unregister(evt.Unit as IHideable);

        private void HandleBuildingSpawn(BuildingSpawnEvent evt)
            => RegisterHostile(evt.Building, evt.Owner);

        private void HandleBuildingDeath(BuildingDeathEvent evt)
            => Unregister(evt.Building);

        private void HandlePlaceHolderSpawn(PlaceHolderSpawnEvent evt)
            => Register(evt.Placeholder);

        private void HandlePlaceHolderDestroy(PlaceHolderDestroyEvent evt)
            => Unregister(evt.Placeholder);

        /// <summary>
        /// FogOverlay 및 미니맵 머티리얼에 fog 카메라 바운드와 UV 변환을 전달합니다.
        /// </summary>
        private void UpdateFogCameraBounds()
        {
            float fogHalfH = _fogOfWarCamera.orthographicSize;
            float fogHalfW = fogHalfH * _fogOfWarCamera.aspect;
            Vector3 fogPos = _fogOfWarCamera.transform.position;

            float fogMinX = fogPos.x - fogHalfW;
            float fogMinY = fogPos.y - fogHalfH;
            float fogMaxX = fogPos.x + fogHalfW;
            float fogMaxY = fogPos.y + fogHalfH;

            if (fogOverlayMaterial != null)
                fogOverlayMaterial.SetVector(FogCameraBoundsID, new Vector4(fogMinX, fogMinY, fogMaxX, fogMaxY));

            if (minimapFogMaterial == null || minimapCamera == null)
                return;

            float mmHalfH = minimapCamera.orthographicSize;
            float mmHalfW = mmHalfH * minimapCamera.aspect;
            Vector3 mmPos = minimapCamera.transform.position;

            float mmMinX = mmPos.x - mmHalfW;
            float mmMinY = mmPos.y - mmHalfH;
            float mmWidth = mmHalfW * 2f;
            float mmHeight = mmHalfH * 2f;
            float fogWidth = fogHalfW * 2f;
            float fogHeight = fogHalfH * 2f;

            float scaleX = mmWidth / fogWidth;
            float scaleY = mmHeight / fogHeight;
            float offsetX = (mmMinX - fogMinX) / fogWidth;
            float offsetY = (mmMinY - fogMinY) / fogHeight;

            minimapFogMaterial.SetVector(FogUVTransformID, new Vector4(offsetX, offsetY, scaleX, scaleY));
        }
    }
}
