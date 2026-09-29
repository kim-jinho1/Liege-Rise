using System.Collections.Generic;
using Code.Commandable.Interfaces;
using Code.Effects;
using Code.Units.Combat;
using UnityEngine;

namespace Code.Commandable
{
    /// <summary>
    /// 명령 대상의 겉모습을 맡습니다 — 렌더러 수집, 선택·호버 외곽선, 피격 번쩍임, 부식, 시야 범위 표시입니다.
    /// AbstractCommandable이 소유하고 필요할 때 시킵니다.
    /// </summary>
    public sealed class CommandableVisuals
    {
        private readonly GameObject _owner;
        private readonly SpriteRenderer[] _renderers;
        private readonly SpriteRenderer[] _bodyRenderers;
        private readonly SelectionOutlineEffect _selection;
        private readonly Transform _visionTrm;

        private HitFlashEffect _hitFlash;
        private CorrosionEffect _corrosion;

        /// <summary>
        /// 렌더러를 훑어 갈래를 나누고 외곽선 효과를 붙입니다.
        /// </summary>
        public CommandableVisuals(GameObject owner, SpriteRenderer minimapIcon, Transform visionTrm,
            Color outlineColor, float hoverAlphaScale)
        {
            _owner = owner;
            _visionTrm = visionTrm;
            _renderers = owner.GetComponentsInChildren<SpriteRenderer>();
            _bodyRenderers = BuildBodyRenderers(minimapIcon, visionTrm);

            _hitFlash = owner.GetComponent<HitFlashEffect>();
            _corrosion = owner.GetComponent<CorrosionEffect>();

            _selection = owner.GetComponent<SelectionOutlineEffect>();
            if (_selection == null)
                _selection = owner.AddComponent<SelectionOutlineEffect>();

            _selection.Initialize(_bodyRenderers, outlineColor, hoverAlphaScale);
        }

        /// <summary>
        /// 선택 외곽선을 켜고 끕니다.
        /// </summary>
        public void SetSelected(bool selected)
            => _selection.SetSelected(selected);

        /// <summary>
        /// 커서가 올라와 있는 동안의 외곽선을 켜고 끕니다.
        /// </summary>
        public void SetHovered(bool hovered)
            => _selection.SetHovered(hovered);

        /// <summary>
        /// 시퀀스 안내 외곽선을 켜고 끕니다.
        /// </summary>
        public void SetFlagged(bool flagged)
            => _selection.SetFlagged(flagged);

        /// <summary>
        /// 시야 범위 표시를 세웁니다. 플레이어 소유물만 켜고, 지름은 시야 반경의 두 배입니다.
        /// 시야 설정이 없으면 음수가 들어오고, 그때는 프리팹 크기를 그대로 둡니다.
        /// </summary>
        public void InitializeVision(bool isPlayer, float sightRadius)
        {
            if (_visionTrm == null)
                return;

            _visionTrm.gameObject.SetActive(isPlayer);

            if (isPlayer && sightRadius >= 0f)
                _visionTrm.localScale = Vector3.one * (sightRadius * 2);
        }

        /// <summary>
        /// 모든 스프라이트 렌더러를 켜고 끕니다. 사망 연출 종료와 안개 진입·이탈에 씁니다.
        /// </summary>
        public void SetRenderersEnabled(bool enabled)
        {
            foreach (SpriteRenderer spriteRenderer in _renderers)
                if (spriteRenderer != null)
                    spriteRenderer.enabled = enabled;
        }

        /// <summary>
        /// 피격 번쩍임을 재생합니다. 컴포넌트가 없으면 그때 붙입니다.
        /// </summary>
        public void Flash()
        {
            if (_bodyRenderers.Length == 0)
                return;

            if (_hitFlash == null)
                _hitFlash = _owner.AddComponent<HitFlashEffect>();

            _hitFlash.Flash(_bodyRenderers);
        }

        /// <summary>
        /// 지속 피해를 겁니다. 컴포넌트가 없으면 그때 붙입니다.
        /// </summary>
        public void ApplyCorrosion(IDamageable target, int damagePerTick, float tickInterval, float duration)
        {
            if (_corrosion == null)
                _corrosion = _owner.AddComponent<CorrosionEffect>();

            _corrosion.Apply(target, damagePerTick, tickInterval, duration);
        }

        /// <summary>
        /// 피격 번쩍임 대상이 되는 본체 렌더러를 고릅니다.
        /// 미니맵 아이콘·시야 비주얼은 색이 변하면 안 되므로 제외합니다.
        /// </summary>
        private SpriteRenderer[] BuildBodyRenderers(SpriteRenderer minimapIcon, Transform visionTrm)
        {
            List<SpriteRenderer> bodyRenderers = new(_renderers.Length);
            foreach (SpriteRenderer spriteRenderer in _renderers)
            {
                if (spriteRenderer == minimapIcon)
                    continue;

                if (visionTrm != null && spriteRenderer.transform.IsChildOf(visionTrm))
                    continue;

                bodyRenderers.Add(spriteRenderer);
            }

            return bodyRenderers.ToArray();
        }
    }
}
