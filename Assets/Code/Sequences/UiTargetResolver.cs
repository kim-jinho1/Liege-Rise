using System;
using Code.Buildings.Data;
using Code.Commands.Base;
using Code.Commands.Types.Production;
using Code.Commands.Types.System;
using Code.Military.Commands;
using Code.UI;
using Code.UI.Selection;
using Code.UI.Sequence;
using Code.Units.Pawns;
using UnityEngine;

namespace Code.Sequences
{
    /// <summary>
    /// UI 힌트 종류를 지금 화면에 있는 RectTransform으로 풉니다. 태그 대신 컴포넌트와 칸에 꽂힌 커맨드로 찾습니다.
    /// 패널이 바뀔 때마다 답이 달라지므로 매 프레임 불러도 되게 가볍게 둡니다.
    /// </summary>
    public sealed class UiTargetResolver
    {
        private static readonly Func<BaseCommandSO, bool> IsBuildMenu = c => c is OverrideCommandsCommand;
        private static readonly Func<BaseCommandSO, bool> IsSquadProduce = c => c is BuildSquadCommandSO;
        private static readonly Func<BaseCommandSO, bool> IsAppoint = c => c is AppointCommanderCommandSO;
        private static readonly Func<BaseCommandSO, bool> IsCommanderProduce = c => c is BuildCommanderCommandSO;
        private static readonly Func<BaseCommandSO, bool> IsPawnProduce =
            c => c is BuildUnitCommandSO build && build.Unit is PawnSO;
        private static readonly Func<BaseCommandSO, bool> IsFarmBuild = BuildOf<FarmBuildingSO>;
        private static readonly Func<BaseCommandSO, bool> IsCottageBuild = BuildOf<PopulationBuildingSO>;
        private static readonly Func<BaseCommandSO, bool> IsBarracksBuild = BuildOf<SquadRecruitmentBuildingSO>;

        private CommandPanel _panel;
        private ResourceUI _resource;
        private PopulationUI _population;
        private MinimapClickHandler _minimap;
        private ObjectiveHUD _objective;
        private CommanderPortraitStrip _portraits;

        /// <summary>
        /// allowFallback이 켜져 있으면 건설 칸이 없을 때 Q 칸으로 내려갑니다. 강조는 켜고, 안내 손은 끕니다 —
        /// 손은 "Q → 농장"을 두 제스처로 따로 적으므로 내려가면 Q를 두 번 누르는 것으로 보입니다.
        /// </summary>
        public bool TryResolve(UIElementHighlightType key, out RectTransform rect, bool allowFallback = true)
        {
            rect = null;
            switch (key)
            {
                case UIElementHighlightType.ResourceHUD:
                    return TryRect(Find(ref _resource), out rect);
                case UIElementHighlightType.PopulationHUD:
                    return TryRect(Find(ref _population), out rect);
                case UIElementHighlightType.BuildMenuButton:
                    return TrySlot(IsBuildMenu, out rect);
                case UIElementHighlightType.SquadProduceButton:
                    return TrySlot(IsSquadProduce, out rect);
                case UIElementHighlightType.PawnProduceButton:
                    return TrySlot(IsPawnProduce, out rect);
                case UIElementHighlightType.AppointButton:
                    return TrySlot(IsAppoint, out rect);
                case UIElementHighlightType.Minimap:
                    return TryRect(Find(ref _minimap), out rect);
                case UIElementHighlightType.ObjectiveHUD:
                    rect = Find(ref _objective) != null ? _objective.PanelRect : null;
                    return rect != null && rect.gameObject.activeInHierarchy;
                case UIElementHighlightType.CommanderProduceButton:
                    return TrySlot(IsCommanderProduce, out rect);
                case UIElementHighlightType.CommanderPortrait1:
                    return TryPortrait(1, out rect);
                case UIElementHighlightType.CommanderPortrait2:
                    return TryPortrait(2, out rect);

                // 건설 메뉴가 안 열려 있으면 Q 칸을 짚는다. 두 단계를 힌트 하나가 따라간다
                case UIElementHighlightType.FarmBuildButton:
                    return TrySlot(IsFarmBuild, out rect) || allowFallback && TrySlot(IsBuildMenu, out rect);
                case UIElementHighlightType.CottageBuildButton:
                    return TrySlot(IsCottageBuild, out rect) || allowFallback && TrySlot(IsBuildMenu, out rect);
                case UIElementHighlightType.BarracksBuildButton:
                    return TrySlot(IsBarracksBuild, out rect) || allowFallback && TrySlot(IsBuildMenu, out rect);

                default:
                    return false;
            }
        }

        /// <summary>
        /// 오른쪽 초상 줄에서 그 번호의 칸입니다. 줄이 없거나 아직 그 번호가 없으면 짚지 않습니다.
        /// </summary>
        private bool TryPortrait(int ordinal, out RectTransform rect)
        {
            rect = null;
            CommanderPortraitStrip strip = Find(ref _portraits);
            return strip != null && strip.TryGetPortrait(ordinal, out rect);
        }

        private bool TrySlot(Func<BaseCommandSO, bool> predicate, out RectTransform rect)
        {
            rect = null;
            CommandPanel panel = Find(ref _panel);
            return panel != null && panel.TryFindSlot(predicate, out rect);
        }

        private static bool TryRect(Component component, out RectTransform rect)
        {
            rect = component != null ? component.transform as RectTransform : null;
            return rect != null;
        }

        /// <summary>
        /// HUD는 _Core에 상주하므로 한 번 찾으면 그대로 씁니다. 파괴되면 다음에 다시 찾습니다.
        /// </summary>
        private static T Find<T>(ref T cached) where T : Component
        {
            if (cached == null)
                cached = UnityEngine.Object.FindAnyObjectByType<T>();

            return cached;
        }

        private static bool BuildOf<TBuilding>(BaseCommandSO command) where TBuilding : BuildingSO
            => command is ConstructionBuildingCommandSO build && build.BuildingSO is TBuilding;
    }
}
