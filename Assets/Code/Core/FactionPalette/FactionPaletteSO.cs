using System.Collections.Generic;
using Code.Core.Bus;
using UnityEngine;

namespace Code.Core.FactionPalette
{
    /// <summary>
    /// 진영 팔레트
    /// </summary>
    [CreateAssetMenu(fileName = "Faction Palette", menuName = "Commandable/Faction Palette", order = 0)]
    public class FactionPaletteSO : ScriptableObject
    {
        [SerializeField] private FactionProfile[] profiles;
        [SerializeField] private Color fallbackSelectionColor = Color.red;
        [SerializeField] private Color fallbackMinimapColor = Color.red;

        // 커서를 올렸을 때의 외곽선 진하기. 선택보다 옅어야 둘을 눈으로 구분한다
        [SerializeField, Range(0.1f, 1f)] private float hoverAlphaScale = 0.55f;

        /// <summary>
        /// 호버 외곽선에 곱하는 알파 배율입니다. 선택은 원래 진하기를 그대로 씁니다.
        /// </summary>
        public float HoverAlphaScale => hoverAlphaScale;

        private Dictionary<Owner, FactionProfile> _lookup;

        /// <summary>
        /// 선택 외곽선 색상을 반환합니다. 미등록 진영은 대체 색을 씁니다.
        /// </summary>
        public Color GetSelectionColor(Owner owner)
            => TryGet(owner, out FactionProfile factionProfile)
                ? factionProfile.SelectionColor
                : fallbackSelectionColor;

        /// <summary>
        /// 미니맵 아이콘 색상을 반환합니다. 미등록 진영은 대체 색을 씁니다.
        /// </summary>
        public Color GetMinimapColor(Owner owner)
            => TryGet(owner, out FactionProfile factionProfile)
                ? factionProfile.MinimapColor
                : fallbackMinimapColor;

        private bool TryGet(Owner owner, out FactionProfile factionProfile)
        {
            if (_lookup != null)
                return _lookup.TryGetValue(owner, out factionProfile);

            _lookup = new Dictionary<Owner, FactionProfile>(profiles.Length);
            foreach (FactionProfile item in profiles)
                _lookup[item.Owner] = item;

            return _lookup.TryGetValue(owner, out factionProfile);
        }
    }
}
