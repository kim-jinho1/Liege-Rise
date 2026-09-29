using Code.Buildings.Logic.CoreBuilding;
using Code.Commandable;
using Code.Core.Bus;
using Code.Players;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Code.Sequences.Effects
{
    /// <summary>
    /// 플레이어에게 유닛을 지급하는 효과입니다. 지정 프리팹을 본성(또는 지정 앵커) 부근에 생성합니다.
    /// 생산 비용·큐를 우회하는 튜토리얼 전용 지급이며, 생성된 유닛은 자체 등록 로직을 따릅니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Eff_GiveUnits", menuName = "Sequence/Effects/GiveUnits")]
    public class GiveUnitsEffectSO : SequenceStepEffectSO
    {
        [BoxGroup("유닛 지급"), LabelText("유닛 프리팹"), Required, AssetsOnly]
        [field: SerializeField] public GameObject UnitPrefab { get; private set; }

        [BoxGroup("유닛 지급"), LabelText("개수"), MinValue(1)]
        [field: SerializeField] public int Count { get; private set; } = 1;

        [BoxGroup("스폰 위치"), LabelText("스폰 기준"), EnumToggleButtons]
        [field: SerializeField] public GameObjectHighlightType SpawnAnchor { get; private set; } = GameObjectHighlightType.MainCastle;

        [BoxGroup("스폰 위치"), LabelText("스폰 오프셋")]
        [field: SerializeField] public Vector2 SpawnOffset { get; private set; } = new(2f, 0f);

        [BoxGroup("스폰 위치"), LabelText("유닛 간격"), MinValue(0f)]
        [field: SerializeField] public float Spacing { get; private set; } = 1f;

        [BoxGroup("스폰 위치"), LabelText("나온 자리를 비춘다")]
        [field: SerializeField] public bool FocusCamera { get; private set; } = true;

        public override void Execute()
        {
            if (Count <= 0)
                return;

            if (UnitPrefab == null)
            {
                Debug.LogWarning($"[{name}] 지급할 유닛 프리팹이 비어 있어 아무것도 주지 못했습니다.", this);
                return;
            }

            Vector3 anchor = ResolveAnchor();
            for (int i = 0; i < Count; i++)
            {
                Vector3 position = anchor + (Vector3)SpawnOffset + new Vector3(i * Spacing, 0f, 0f);
                GameObject spawned = Instantiate(UnitPrefab, position, Quaternion.identity);

                // 프리팹 칸이 루트가 아니라 자식(미니맵 아이콘 등)을 가리키면 여기서 드러난다.
                // 조용히 두면 아이콘 한 장만 놓이고 지원군은 영영 오지 않는다
                if (!spawned.TryGetComponent(out AbstractCommandable commandable))
                {
                    Debug.LogWarning($"[{name}] '{UnitPrefab.name}'에 AbstractCommandable이 없습니다. " +
                                     "프리팹 루트를 가리키는지 확인하세요.", this);
                    continue;
                }

                // 지급은 플레이어 몫이다. 프리팹 값에 맡기면 한 칸만 틀려도 적에게 준다
                commandable.Owner = Owner.Player;
            }

            if (FocusCamera)
                LookAt(anchor + (Vector3)SpawnOffset);
        }

        /// <summary>
        /// 지급한 자리를 비춥니다. 화면 밖에서 조용히 나오면 플레이어는 지원군이 온 줄도 모릅니다.
        /// </summary>
        private static void LookAt(Vector3 position)
        {
            CameraController camera = FindAnyObjectByType<CameraController>();
            if (camera != null)
                camera.JumpTo(position);
        }

        /// <summary>
        /// 생성 기준 좌표를 구합니다. 하이라이트 앵커 → 플레이어 본부 순으로 탐색합니다.
        /// </summary>
        private Vector3 ResolveAnchor()
        {
            if (SequenceHighlighter.Instance != null
                && SequenceHighlighter.Instance.TryGetPosition(SpawnAnchor, out Vector3 position))
                return position;

            // 폴백은 반드시 소유자를 가린다 — 안 가리면 GoblinCore·FishHut도 본부라
            // 지원군이 맵 반대편 적진 한가운데에 떨어진다
            foreach (HeadquartersBuilding headquarters in FindObjectsByType<HeadquartersBuilding>())
                if (headquarters != null && headquarters.Owner == Owner.Player)
                    return headquarters.transform.position;

            Debug.LogWarning($"[{name}] 스폰 기준을 찾지 못해 원점에 지급합니다.", this);
            return Vector3.zero;
        }
    }
}
