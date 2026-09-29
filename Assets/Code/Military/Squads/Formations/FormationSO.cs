using Code.Military.Stats;
using UnityEngine;

namespace Code.Military.Squads.Formations
{
    /// <summary>
    /// 분대원 배치에 대한 정보를 담고 있는 ScriptableObject입니다.
    /// 슬롯 모양뿐 아니라 그 대형으로 싸울 때의 전투 보정도 함께 쥡니다.
    /// </summary>
    public abstract class FormationSO : ScriptableObject
    {
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public float Spacing { get; private set; }
        [field: SerializeField] public float SeparationRadius { get; private set; } = 0.4f;
        [field: SerializeField] public float RotationSpeed { get; private set; } = 90f;

        [field: Header("Combat")] // 새 자리로 걸어가는 이 시간이 대형을 바꾸는 대가다 — 그동안 보정이 없다
        [field: SerializeField] public float SetupTime { get; private set; } = 1.5f;
        [field: SerializeField] public StatEffect[] Effects { get; private set; }

        /// <summary>
        /// 분대원 배치 슬롯의 오프셋을 반환합니다. 이 대형의 기본 간격을 씁니다.
        /// </summary>
        public Vector2 GetOffset(int slotIndex, int memberCount)
            => GetOffset(slotIndex, memberCount, Spacing);

        /// <summary>
        /// 간격을 지정해 슬롯 오프셋을 구합니다. 교전 중에는 사거리에 맞춰 좁힌 값이 들어옵니다.
        /// </summary>
        public abstract Vector2 GetOffset(int slotIndex, int memberCount, float spacing);

        /// <summary>
        /// 지정 슬롯이 설 월드 좌표입니다. 배치와 교전 유지가 같은 자리를 보게 하는 통로입니다.
        /// </summary>
        public Vector3 SlotAt(int slotIndex, int memberCount, Vector3 origin, float facingAngle, float spacing)
            => origin + Quaternion.Euler(0f, 0f, facingAngle)
                * GetOffset(slotIndex, memberCount, spacing);

        /// <summary>
        /// 모든 슬롯이 지정한 반경 안에 들어오는 간격입니다.
        /// 간격 1로 슬롯을 훑어 가장 먼 자리를 재므로 대형별 분기가 필요 없습니다.
        /// </summary>
        public float SpacingForRadius(float maxRadius, int memberCount)
        {
            float widest = 0f;
            for (int slot = 0; slot < memberCount; slot++)
                widest = Mathf.Max(widest, GetOffset(slot, memberCount, 1f).magnitude);

            return widest < 0.0001f ? Spacing : maxRadius / widest;
        }
    }
}
