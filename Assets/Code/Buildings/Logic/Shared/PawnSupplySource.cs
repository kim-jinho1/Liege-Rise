using Code.Buildings.Base;
using Code.Buildings.Interfaces;
using Code.Commandable;
using Code.Core;
using UnityEngine;

namespace Code.Buildings.Logic.Shared
{
    /// <summary>
    /// 판이 시작될 때 본부 앞에 폰을 세워 둡니다. 그게 전부입니다 —
    /// 자동 재생성은 2026-09-07에 뺐고, 이후의 폰은 본부·집의 생산 버튼으로 뽑습니다.
    /// </summary>
    public class PawnSupplySource : MonoBehaviour
    {
        private bool _initialized;

        public static PawnSupplySource GetOrAdd(AbstractBuilding building)
        {
            PawnSupplySource source = building.GetComponent<PawnSupplySource>();
            return source != null ? source : building.gameObject.AddComponent<PawnSupplySource>();
        }

        public void Initialize(AbstractBuilding building, IPawnSupplier supplier)
        {
            if (_initialized)
                return;

            // supplier는 인터페이스라 != null이 Unity의 == 오버로드를 타지 않는다
            if (building == null || !UnityRef.IsAlive(supplier)
                                 || supplier.PawnSO == null || supplier.PawnSO.Prefab == null)
                return;

            _initialized = true;

            Transform spawn = building.SpawnPoint != null ? building.SpawnPoint : building.transform;
            for (int i = 0; i < supplier.InitialPawns; i++)
            {
                // 순번으로 자리를 흩는다. 한 점에 뱉으면 한 루프에 만든 폰이 전부 포개진다
                Vector3 position = SpawnScatter.Around(spawn.position, i);
                GameObject pawn = Instantiate(supplier.PawnSO.Prefab, position, spawn.rotation);
                if (pawn.TryGetComponent(out AbstractCommandable commandable))
                    commandable.Owner = building.Owner;
            }
        }
    }
}
