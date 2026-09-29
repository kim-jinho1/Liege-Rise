using UnityEngine;

namespace Code.MapGeneration
{
    /// <summary>
    /// 기지 슬롯이 어느 진영으로 쓰일지입니다.
    /// </summary>
    public enum SlotSide
    {
        Random,
        Player,
        Enemy
    }

    /// <summary>
    /// 씬에 배치된 기지 슬롯의 앵커입니다. MapGenerator가 이 앵커를 찾아 기지를 배치합니다.
    /// </summary>
    public class BaseSlotAnchor : MapAnchor
    {
        /// <summary>
        /// 이 슬롯이 속한 쌍의 키입니다. 같은 키를 가진 슬롯 두 개가 한 쌍이 됩니다.
        /// </summary>
        [field: SerializeField] public string PairKey { get; private set; } = "NS";

        // 매 판 같은 자리에서 시작해야 하는 맵(튜토리얼)에서 진영을 못 박는다.
        // Random이면 짝의 어느 쪽이 플레이어가 될지 매 판 뒤집힌다
        [field: SerializeField] public SlotSide FixedSide { get; private set; } = SlotSide.Random;

        protected override Color GizmoColor => new(1f, 0.45f, 0.1f);

        protected override string GizmoLabel
            => FixedSide == SlotSide.Random
                ? $"Base Slot [{PairKey}]"
                : $"Base Slot [{PairKey} · {SideLabel}]";

        private string SideLabel
            => FixedSide == SlotSide.Player ? "플레이어" : "적";
    }
}
