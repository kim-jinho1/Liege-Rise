using Code.Buildings.Base;
using Code.Buildings.Data;
using Code.Core.Bus;
using Code.GameEvents.Building;
using Code.Sequences.Guide;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Code.Sequences.Effects
{
    /// <summary>
    /// 플레이어에게 완성된 건물을 지급하는 효과입니다. 건설 과정을 건너뛰고 본성 곁 빈 땅에 세웁니다.
    /// 부스용 짧은 판처럼 병영을 갖춘 채 시작할 때 씁니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Eff_GiveBuilding", menuName = "Sequence/Effects/GiveBuilding")]
    public class GiveBuildingEffectSO : SequenceStepEffectSO
    {
        [BoxGroup("건물 지급"), LabelText("건물 데이터"), Required]
        [field: SerializeField] public BuildingSO Building { get; private set; }

        [BoxGroup("자리"), LabelText("본성 기준 오프셋")]
        [field: SerializeField] public Vector2 SpawnOffset { get; private set; } = new(-9f, -4f);

        [BoxGroup("자리"), LabelText("비어 있어야 하는 반경"), MinValue(0f)]
        [field: SerializeField] public float ClearRadius { get; private set; } = 5f;

        public override void Execute()
        {
            if (Building == null || Building.Prefab == null)
            {
                Debug.LogWarning($"[{name}] 지급할 건물 데이터가 비어 있습니다.", this);
                return;
            }

            if (!GuidePointFinder.TryGetPlayerCastle(out Vector3 castle))
            {
                Debug.LogWarning($"[{name}] 플레이어 본성이 없어 건물을 놓을 자리를 못 잡았습니다.", this);
                return;
            }

            Vector3 position = ClearSpotFinder.Find(castle + (Vector3)SpawnOffset, ClearRadius);
            GameObject spawned = Instantiate(Building.Prefab, position, Quaternion.identity);
            if (!spawned.TryGetComponent(out AbstractBuilding building))
            {
                Debug.LogWarning($"[{name}] '{Building.Prefab.name}'에 AbstractBuilding이 없습니다.", this);
                return;
            }

            // 건설 완료와 같은 경로를 밟아야 등록·UI가 같은 상태가 된다 (ConstructionDummy.CompleteAndSpawn과 같다)
            building.Owner = Owner.Player;
            Bus<BuildingSetupCompleteEvent>.Raise(Owner.Player, new BuildingSetupCompleteEvent(Owner.Player, building));
        }
    }
}
