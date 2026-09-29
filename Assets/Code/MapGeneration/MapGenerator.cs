using System;
using Code.Bootstraps;
using Code.Navigation.Baking;
using Code.Players;
using UnityEngine;
using Random = System.Random;

namespace Code.MapGeneration
{
    /// <summary>
    /// 손으로 찍은 고정 지형 위에 기지와 자원을 절차적으로 배치합니다.
    /// 위치 후보는 씬의 MapAnchor가, 무엇을 놓을지는 MapLayoutSO가 정하고, 실제 배치는 AnchorPopulator가 수행합니다.
    /// Bootstrap이 다른 매니저보다 먼저 호출하므로, 이후 초기화는 모두 기지가 존재하는 상태에서 돌아갑니다.
    /// </summary>
    public class MapGenerator : MonoBehaviour, IGameStartListener
    {
        [Header("Layout")]
        [SerializeField] private MapLayoutSO layout;
        [SerializeField] private Transform container; // 생성물을 담을 부모. 비워 두면 이 오브젝트 아래에 생성한다.
                                                      // 이 아래 자식은 재생성 시 전부 삭제된다

        [Header("Seed")]
        [SerializeField] private int seed; // 0이면 매 판 무작위. 로그에 찍힌 값을 넣으면 그 배치가 그대로 재현된다

        private readonly PlacementNotes _notes = new();

        /// <summary>
        /// 모든 매니저보다 먼저 실행해, 기지·자원이 존재하는 상태로 초기화를 넘깁니다.
        /// </summary>
        public int InitOrder => -100;

        /// <summary>
        /// Bootstrap이 게임 시작 시 1회 호출합니다.
        /// </summary>
        public void OnGameStart()
            => Generate();

        /// <summary>
        /// 배치를 실행합니다. 이미 생성된 오브젝트가 있으면 먼저 정리합니다.
        /// </summary>
        public void Generate()
        {
            if (layout == null)
            {
                Debug.LogWarning($"[{name}] MapLayoutSO가 지정되지 않아 배치를 건너뜁니다.", this);
                return;
            }

            PathBaker baker = PathBaker.Instance;
            if (baker == null || baker.BakedData == null)
            {
                Debug.LogError($"[{name}] PathBaker 또는 베이크 데이터가 없습니다. 지형을 먼저 베이크하세요.", this);
                return;
            }

            int effectiveSeed = seed != 0 ? seed : Environment.TickCount;
            Random random = new(effectiveSeed);

            if (!BaseSlotPairing.TrySelect(FindObjectsByType<BaseSlotAnchor>(), random,
                    out BaseSlotAnchor playerSlot, out BaseSlotAnchor enemySlot))
            {
                Debug.LogError(
                    $"[{name}] 쓸 수 있는 기지 슬롯 짝이 없습니다. 같은 PairKey를 가진 BaseSlotAnchor를 2개 배치하세요.", this);
                return;
            }

            Transform parent = container != null ? container : transform;
            ClearGenerated(parent);

            PlacementSampler sampler = new(random, layout.GlobalMinSpacing);
            AnchorPopulator populator = new(baker, sampler, layout.MaxAttemptsPerItem);

            int total = SpawnBase(playerSlot, layout.PlayerBase, populator, baker, random, parent)
                        + SpawnBase(enemySlot, layout.EnemyBase, populator, baker, random, parent);

            SpawnZoneAnchor[] zones = CollectZonesInStableOrder();
            ZoneGroupSelector selector = new();
            selector.Resolve(zones, layout.Groups, random);

            foreach (SpawnZoneAnchor zone in zones)
                total += PopulateZone(zone, selector, populator, baker, random, parent);

            FocusCameraOn(playerSlot.Center);
            Debug.Log($"<color=green>[MapGenerator] {total}개 배치 완료 — 짝 '{playerSlot.PairKey}', " +
                      $"플레이어 {playerSlot.name} / 적 {enemySlot.name}, " +
                      $"활성 구역 {selector.ActiveZoneCount}곳 (seed: {effectiveSeed})</color>");
        }

        /// <summary>
        /// 이전에 생성한 오브젝트를 모두 제거합니다. Generate를 다시 호출할 때 중복 배치를 막습니다.
        /// </summary>
        private static void ClearGenerated(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        /// <summary>
        /// 구역을 이름순으로 모읍니다. FindObjectsByType의 순서는 보장되지 않으므로
        /// 정렬해야 같은 시드가 같은 배치를 재현합니다.
        /// </summary>
        private static SpawnZoneAnchor[] CollectZonesInStableOrder()
        {
            SpawnZoneAnchor[] zones = FindObjectsByType<SpawnZoneAnchor>();
            Array.Sort(zones, (left, right) => string.CompareOrdinal(left.name, right.name));
            return zones;
        }

        /// <summary>
        /// 기지 하나를 배치합니다. 본부 건물은 앵커 중앙에, 위성은 앵커 반경 안에 흩뿌립니다.
        /// </summary>
        private int SpawnBase(BaseSlotAnchor slot, BaseCompositionSO composition, AnchorPopulator populator,
            PathBaker baker, Random random, Transform parent)
        {
            if (composition == null || composition.CorePrefab == null)
            {
                Debug.LogError($"[{name}] '{slot.name}' 슬롯에 쓸 기지 구성이 비어 있습니다.", this);
                return 0;
            }

            populator.Collect(slot, baker.BakedData);

            int placed = 0;
            if (populator.TryPlaceAt(slot.Center, composition.SafeCoreFootprint, composition.CorePrefab, parent))
                placed++;
            else
                Debug.LogError($"[{name}] '{slot.name}'에 {composition.CorePrefab.name}을(를) 놓을 자리가 없습니다. " +
                               "슬롯을 건설 가능한 지형으로 옮기거나 반경을 늘리세요.", this);

            foreach (SpawnEntry satellite in composition.Satellites)
            {
                if (!satellite.IsValid)
                    continue;

                placed += PlaceCounted(populator, satellite, satellite.ResolveCount(random), parent,
                    ClusterBounds.None);
            }

            _notes.Flush($"'{slot.name}' 기지", this);
            return placed;
        }

        /// <summary>
        /// 구역 하나의 구성을 굴려 채우고 실제 배치 수를 반환합니다.
        /// 이번 판에 뽑히지 않은 후보 앵커는 조용히 비워 둡니다 — 그게 정상 동작입니다.
        /// </summary>
        private int PopulateZone(SpawnZoneAnchor zone, ZoneGroupSelector selector, AnchorPopulator populator,
            PathBaker baker, Random random, Transform parent)
        {
            if (!selector.TryGetComposition(zone, out ZoneCompositionSO composition))
            {
                if (string.IsNullOrWhiteSpace(zone.GroupKey))
                    Debug.LogWarning($"[{name}] '{zone.name}' 구역에 구성이 지정되지 않아 건너뜁니다.", this);

                return 0;
            }

            populator.Collect(zone, baker.BakedData);
            if (populator.CandidateCount == 0)
            {
                Debug.LogWarning($"[{name}] '{zone.name}' 구역에 건설 가능한 지형이 없습니다. " +
                                 "반경을 늘리거나 위치를 옮기세요.", this);
                return 0;
            }

            ZoneOption[] options = composition.Options;
            int[] counts = composition.Roll(random);
            ClusterBounds cluster = new(zone.Center, composition.ClusterRadius);
            int placed = 0;

            foreach (int index in OrderForPlacement(options))
            {
                if (counts[index] <= 0 || !options[index].entry.IsValid)
                    continue;

                placed += PlaceCounted(populator, options[index].entry, counts[index], parent,
                    options[index].clustered ? cluster : ClusterBounds.None);
            }

            _notes.Relaxed(populator.RelaxedCount);
            _notes.Flush($"'{zone.name}' 구역", this);
            return placed;
        }

        /// <summary>
        /// 목표 개수만큼 배치를 맡기고 미달분을 적어 둡니다. 알리는 것은 앵커를 다 채운 뒤 한 번입니다.
        /// </summary>
        private int PlaceCounted(AnchorPopulator populator, SpawnEntry entry, int target, Transform parent,
            ClusterBounds bounds)
        {
            int placed = populator.PlaceEntry(entry, target, parent, bounds);
            _notes.Shortfall(entry.prefab.name, target - placed);
            return placed;
        }

        /// <summary>
        /// 배치 순서를 정합니다. 뭉치는 항목이 먼저 자리를 잡아야 자원이 진지 자리를 선점하지 못하고,
        /// 그다음은 간격 요구가 큰 순입니다 — 좁은 항목이 먼저 깔리면 큰 항목이 굶습니다.
        /// </summary>
        private static int[] OrderForPlacement(ZoneOption[] options)
        {
            int[] order = new int[options.Length];
            for (int i = 0; i < order.Length; i++)
                order[i] = i;

            Array.Sort(order, (left, right) =>
            {
                int byCluster = options[right].clustered.CompareTo(options[left].clustered);
                return byCluster != 0
                    ? byCluster
                    : options[right].entry.minSpacing.CompareTo(options[left].entry.minSpacing);
            });
            return order;
        }

        /// <summary>
        /// 시작 시점의 카메라를 이번 판의 플레이어 기지로 옮깁니다.
        /// </summary>
        private static void FocusCameraOn(Vector3 position)
        {
            CameraController cameraController = FindAnyObjectByType<CameraController>();
            if (cameraController == null)
                return;

            cameraController.JumpTo(position);
        }
    }
}
